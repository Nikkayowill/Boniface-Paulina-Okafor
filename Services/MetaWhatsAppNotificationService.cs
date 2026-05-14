using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Okafor_.NET.Data;
using Okafor_.NET.Models;

namespace Okafor_.NET.Services;

public sealed class MetaWhatsAppNotificationService : IWhatsAppNotificationService
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MetaWhatsAppNotificationService> _logger;

    public MetaWhatsAppNotificationService(
        IConfiguration configuration,
        ApplicationDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<MetaWhatsAppNotificationService> logger)
    {
        _configuration = configuration;
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public Task<bool> SendTeleconsultationReceivedAsync(TeleconsultationRequest request, CancellationToken cancellationToken = default)
    {
        var parameters = new[]
        {
            request.PatientName,
            BuildReference(request),
            request.PreferredDate.ToString("MMM d, yyyy"),
            request.PreferredTime,
            request.Department?.Name ?? "Teleconsultation",
            _configuration["Hospital:Name"] ?? "BP Okafor Memorial Hospital"
        };

        return SendTemplateAsync(
            request,
            _configuration["Notifications:WhatsApp:ReceivedTemplate"] ?? "teleconsultation_received",
            parameters,
            $"Teleconsultation received: {BuildReference(request)}",
            cancellationToken);
    }

    public Task<bool> SendTeleconsultationStatusAsync(TeleconsultationRequest request, CancellationToken cancellationToken = default)
    {
        var statusMessage = request.Status switch
        {
            TeleconsultationStatus.Confirmed => "confirmed",
            TeleconsultationStatus.Rescheduled => "rescheduled",
            TeleconsultationStatus.Completed => "completed",
            TeleconsultationStatus.Rejected => "not approved",
            _ => "updated"
        };

        var nextStep = BuildNextStep(request);
        var parameters = new[]
        {
            request.PatientName,
            BuildReference(request),
            statusMessage,
            request.PreferredDate.ToString("MMM d, yyyy"),
            request.PreferredTime,
            request.Department?.Name ?? "Teleconsultation",
            request.Doctor?.FullName ?? "To be assigned",
            nextStep
        };

        return SendTemplateAsync(
            request,
            _configuration["Notifications:WhatsApp:StatusTemplate"] ?? "teleconsultation_update",
            parameters,
            $"Teleconsultation {statusMessage}: {BuildReference(request)}",
            cancellationToken);
    }

    private async Task<bool> SendTemplateAsync(
        TeleconsultationRequest request,
        string templateName,
        IReadOnlyCollection<string> bodyParameters,
        string logMessage,
        CancellationToken cancellationToken)
    {
        if (!request.WhatsAppOptIn)
            return false;

        var recipient = NigerianPhoneNumber.NormalizeForWhatsApp(request.Phone);
        if (string.IsNullOrWhiteSpace(recipient))
            return await LogAsync(request, recipient, logMessage, false, "WhatsApp recipient phone is empty.", null, "failed", cancellationToken);

        if (!IsEnabled())
            return false;

        var phoneNumberId = _configuration["Notifications:WhatsApp:PhoneNumberId"];
        var accessToken = _configuration["Notifications:WhatsApp:AccessToken"];
        var apiVersion = _configuration["Notifications:WhatsApp:ApiVersion"] ?? "v23.0";
        var languageCode = _configuration["Notifications:WhatsApp:LanguageCode"] ?? "en";

        if (IsPlaceholder(phoneNumberId) || IsPlaceholder(accessToken))
        {
            return await LogAsync(request, recipient, logMessage, false, "WhatsApp Cloud API credentials are missing.", null, "failed", cancellationToken);
        }

        var payload = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to = recipient,
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = languageCode },
                components = new[]
                {
                    new
                    {
                        type = "body",
                        parameters = bodyParameters
                            .Select(value => new { type = "text", text = string.IsNullOrWhiteSpace(value) ? "N/A" : value })
                            .ToArray()
                    }
                }
            }
        };

        var endpoint = $"https://graph.facebook.com/{apiVersion}/{phoneNumberId}/messages";
        var client = _httpClientFactory.CreateClient();
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            using var response = await client.SendAsync(httpRequest, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var success = response.IsSuccessStatusCode;
            var error = success
                ? null
                : $"WhatsApp Cloud API failed: HTTP {(int)response.StatusCode}; Body: {Truncate(body, 1000)}";
            var externalMessageId = success ? TryReadMessageId(body) : null;

            if (success)
            {
                _logger.LogInformation("WhatsApp teleconsultation update sent to {Recipient}.", recipient);
            }

            return await LogAsync(request, recipient, logMessage, success, error, externalMessageId, success ? "sent" : "failed", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsApp teleconsultation update failed for {Recipient}.", recipient);
            return await LogAsync(request, recipient, logMessage, false, ex.Message, null, "failed", cancellationToken);
        }
    }

    private async Task<bool> LogAsync(
        TeleconsultationRequest request,
        string recipient,
        string message,
        bool success,
        string? error,
        string? externalMessageId,
        string deliveryStatus,
        CancellationToken cancellationToken)
    {
        _context.NotificationLogs.Add(new NotificationLog
        {
            Channel = "WhatsApp",
            Recipient = recipient,
            MessageBody = message,
            Success = success,
            ErrorMessage = error,
            TeleconsultationRequestId = request.Id,
            ExternalMessageId = externalMessageId,
            DeliveryStatus = deliveryStatus,
            SentAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
        return success;
    }

    private static string? TryReadMessageId(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (!document.RootElement.TryGetProperty("messages", out var messages) ||
                messages.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var first = messages.EnumerateArray().FirstOrDefault();
            return first.ValueKind == JsonValueKind.Object &&
                first.TryGetProperty("id", out var id)
                    ? id.GetString()
                    : null;
        }
        catch
        {
            return null;
        }
    }

    private bool IsEnabled()
    {
        return IntegrationConfiguration.IsEnabledOrAuto(
            _configuration,
            "Notifications:WhatsApp:Enabled",
            IntegrationConfiguration.HasWhatsAppCredentials(_configuration));
    }

    private static bool IsPlaceholder(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ||
            value.StartsWith("REPLACE_WITH_", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildReference(TeleconsultationRequest request)
    {
        return $"TC-{request.Id:D6}";
    }

    private static string BuildNextStep(TeleconsultationRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.MeetingLink))
            return request.MeetingLink;

        if (!string.IsNullOrWhiteSpace(request.AdminNotes))
            return request.AdminNotes;

        return request.Status switch
        {
            TeleconsultationStatus.Confirmed => "Your meeting link will be shared by the care team.",
            TeleconsultationStatus.Rescheduled => "Please review the new date and time.",
            TeleconsultationStatus.Rejected => "Please contact the hospital for safer next steps.",
            TeleconsultationStatus.Completed => "Thank you for using BP Okafor virtual care.",
            _ => "Please wait for clinical review."
        };
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;

        return text[..maxLength] + "...";
    }
}
