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

