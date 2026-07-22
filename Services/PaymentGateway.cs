using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Okafor_.NET.Services;

public sealed record PaymentInitializeRequest(
    string Email,
    decimal Amount,
    string Currency,
    string Reference,
    string CallbackUrl,
    string Purpose,
    string CustomerName,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record PaymentInitializeResult(
    bool Success,
    string Provider,
    string ProviderReference,
    string Channel,
    string Message,
    bool IsSandbox,
    bool RequiresRedirect = false,
    string? AuthorizationUrl = null,
    string? AccessCode = null);

public sealed record PaymentVerificationResult(
    bool Success,
    string ProviderReference,
    string Channel,
    string Message,
    bool IsSandbox,
    DateTime? PaidAt = null,
    decimal? Amount = null,
    string? Currency = null);

public interface IPaymentGateway
{
    string ProviderName { get; }
    bool IsSandbox { get; }
    Task<PaymentInitializeResult> InitializeAsync(PaymentInitializeRequest request, CancellationToken cancellationToken = default);
    Task<PaymentVerificationResult> VerifyAsync(string reference, CancellationToken cancellationToken = default);
}

public sealed class MockPaymentGateway : IPaymentGateway
{
    private readonly IConfiguration _configuration;

    public MockPaymentGateway(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string ProviderName => "Mock";
    public bool IsSandbox => true;

    public Task<PaymentInitializeResult> InitializeAsync(PaymentInitializeRequest request, CancellationToken cancellationToken = default)
    {
        var prefix = _configuration["Payments:Mock:ReferencePrefix"] ?? "SANDBOX";
        var reference = $"{prefix}-{request.Reference}";

        return Task.FromResult(new PaymentInitializeResult(
            Success: true,
            Provider: ProviderName,
            ProviderReference: reference,
            Channel: "Sandbox",
            Message: "Sandbox payment approved. No real money was collected.",
            IsSandbox: true));
    }

    public Task<PaymentVerificationResult> VerifyAsync(string reference, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new PaymentVerificationResult(
            Success: true,
            ProviderReference: reference,
            Channel: "Sandbox",
            Message: "Sandbox payment verified. No real money was collected.",
            IsSandbox: true,
            PaidAt: DateTime.UtcNow));
    }
}

public sealed class DisabledPaymentGateway : IPaymentGateway
{
    private const string UnavailableMessage =
        "Online payments are not available. Please contact the hospital to arrange payment.";

    public string ProviderName => "Disabled";
    public bool IsSandbox => false;

    public Task<PaymentInitializeResult> InitializeAsync(
        PaymentInitializeRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new PaymentInitializeResult(
            Success: false,
            Provider: ProviderName,
            ProviderReference: request.Reference,
            Channel: "Unavailable",
            Message: UnavailableMessage,
            IsSandbox: false));
    }

    public Task<PaymentVerificationResult> VerifyAsync(
        string reference,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new PaymentVerificationResult(
            Success: false,
            ProviderReference: reference,
            Channel: "Unavailable",
            Message: UnavailableMessage,
            IsSandbox: false));
    }
}

public sealed class PaystackPaymentGateway : IPaymentGateway
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public PaystackPaymentGateway(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _httpClient.BaseAddress = ResolveBaseAddress(_configuration);
    }

    public string ProviderName => "Paystack";

    public bool IsSandbox
    {
        get => !IntegrationConfiguration.HasPaystackLiveSecretKey(_configuration);
    }

    public async Task<PaymentInitializeResult> InitializeAsync(PaymentInitializeRequest request, CancellationToken cancellationToken = default)
    {
        var secretKey = GetSecretKey();
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            return new PaymentInitializeResult(
                Success: false,
                Provider: ProviderName,
                ProviderReference: request.Reference,
                Channel: "HostedCheckout",
                Message: "Paystack secret key is not configured.",
                IsSandbox: IsSandbox);
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/transaction/initialize");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);
        httpRequest.Content = JsonContent.Create(new PaystackInitializeRequest
        {
            Email = request.Email,
            Amount = ToSubunitAmount(request.Amount),
            Currency = request.Currency,
            Reference = request.Reference,
            CallbackUrl = request.CallbackUrl,
            Channels = new[] { "card", "bank", "ussd", "bank_transfer", "mobile_money" },
            Metadata = new Dictionary<string, object?>
            {
                ["purpose"] = request.Purpose,
                ["customer_name"] = request.CustomerName,
                ["custom_fields"] = BuildCustomFields(request)
            }
        }, options: JsonOptions);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<PaystackInitializeResponse>(JsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode || payload?.Status != true || payload.Data is null)
        {
            return new PaymentInitializeResult(
                Success: false,
                Provider: ProviderName,
                ProviderReference: request.Reference,
                Channel: "HostedCheckout",
                Message: payload?.Message ?? "Unable to initialize Paystack checkout.",
                IsSandbox: IsSandbox);
        }

        return new PaymentInitializeResult(
            Success: true,
            Provider: ProviderName,
            ProviderReference: payload.Data.Reference ?? request.Reference,
            Channel: "HostedCheckout",
            Message: payload.Message ?? "Paystack checkout initialized.",
            IsSandbox: IsSandbox,
            RequiresRedirect: true,
            AuthorizationUrl: payload.Data.AuthorizationUrl,
            AccessCode: payload.Data.AccessCode);
    }

    public async Task<PaymentVerificationResult> VerifyAsync(string reference, CancellationToken cancellationToken = default)
    {
        var secretKey = GetSecretKey();
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            return new PaymentVerificationResult(
                Success: false,
                ProviderReference: reference,
                Channel: "Paystack",
                Message: "Paystack secret key is not configured.",
                IsSandbox: IsSandbox);
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"/transaction/verify/{Uri.EscapeDataString(reference)}");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<PaystackVerifyResponse>(JsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode || payload?.Status != true || payload.Data is null)
        {
            return new PaymentVerificationResult(
                Success: false,
                ProviderReference: reference,
                Channel: "Paystack",
                Message: payload?.Message ?? "Unable to verify Paystack transaction.",
                IsSandbox: IsSandbox);
        }

        var paid = string.Equals(payload.Data.Status, "success", StringComparison.OrdinalIgnoreCase);
        return new PaymentVerificationResult(
            Success: paid,
            ProviderReference: payload.Data.Reference ?? reference,
            Channel: payload.Data.Channel ?? "Paystack",
            Message: payload.Data.GatewayResponse ?? payload.Message ?? payload.Data.Status ?? "Paystack transaction verified.",
            IsSandbox: string.Equals(payload.Data.Domain, "test", StringComparison.OrdinalIgnoreCase),
            PaidAt: payload.Data.PaidAt ?? payload.Data.TransactionDate,
            Amount: payload.Data.Amount.HasValue ? payload.Data.Amount.Value / 100m : null,
            Currency: payload.Data.Currency);
    }

    public bool IsValidWebhookSignature(string body, string? signature)
    {
        var secretKey = GetSecretKey();
        if (string.IsNullOrWhiteSpace(secretKey) || string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secretKey));
        var hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(hash),
            Encoding.UTF8.GetBytes(signature.ToLowerInvariant()));
    }

    private string? GetSecretKey()
    {
        var key = _configuration["Payments:Paystack:SecretKey"];
        return IntegrationConfiguration.HasPaystackSecretKey(_configuration)
            ? key
            : null;
    }

    private static Uri ResolveBaseAddress(IConfiguration configuration)
    {
        var configuredUrl = configuration["Payments:Paystack:BaseUrl"] ?? "https://api.paystack.co";
        if (!Uri.TryCreate(configuredUrl, UriKind.Absolute, out var baseAddress) ||
            !string.Equals(baseAddress.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal) ||
            !string.Equals(baseAddress.Host, "api.paystack.co", StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrEmpty(baseAddress.UserInfo) ||
            baseAddress.Port != 443 ||
            baseAddress.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(baseAddress.Query) ||
            !string.IsNullOrEmpty(baseAddress.Fragment))
        {
            throw new InvalidOperationException(
                "Payments:Paystack:BaseUrl must be the official HTTPS Paystack API endpoint.");
        }

        return baseAddress;
    }

    private static long ToSubunitAmount(decimal amount)
    {
        return checked((long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero));
    }

    private static object[] BuildCustomFields(PaymentInitializeRequest request)
    {
        var fields = new List<object>
        {
            new { display_name = "Purpose", variable_name = "purpose", value = request.Purpose },
            new { display_name = "Customer name", variable_name = "customer_name", value = request.CustomerName }
        };

        if (request.Metadata is not null)
        {
            fields.AddRange(request.Metadata.Select(item => new
            {
                display_name = item.Key,
                variable_name = item.Key.ToLowerInvariant().Replace(" ", "_"),
                value = item.Value
            }));
        }

        return fields.ToArray();
    }

    private sealed class PaystackInitializeRequest
    {
        public string Email { get; set; } = string.Empty;
        public long Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string CallbackUrl { get; set; } = string.Empty;
        public string[] Channels { get; set; } = Array.Empty<string>();
        public object? Metadata { get; set; }
    }

    private sealed class PaystackInitializeResponse
    {
        public bool Status { get; set; }
        public string? Message { get; set; }
        public PaystackInitializeData? Data { get; set; }
    }

    private sealed class PaystackInitializeData
    {
        [JsonPropertyName("authorization_url")]
        public string? AuthorizationUrl { get; set; }

        [JsonPropertyName("access_code")]
        public string? AccessCode { get; set; }

        public string? Reference { get; set; }
    }

    private sealed class PaystackVerifyResponse
    {
        public bool Status { get; set; }
        public string? Message { get; set; }
        public PaystackVerifyData? Data { get; set; }
    }

    private sealed class PaystackVerifyData
    {
        public string? Status { get; set; }
        public string? Reference { get; set; }
        public string? Domain { get; set; }
        public string? Channel { get; set; }
        public string? Currency { get; set; }
        public long? Amount { get; set; }

        [JsonPropertyName("gateway_response")]
        public string? GatewayResponse { get; set; }

        [JsonPropertyName("paidAt")]
        public DateTime? PaidAt { get; set; }

        [JsonPropertyName("transaction_date")]
        public DateTime? TransactionDate { get; set; }
    }
}
