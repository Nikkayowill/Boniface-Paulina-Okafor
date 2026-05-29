using Okafor_.NET.Data;
using Okafor_.NET.Models;
using System.Text.Json;

namespace Okafor_.NET.Services;

/// <summary>
/// Africa's Talking SMS implementation.
/// Activated by setting "Notifications:Provider": "AfricasTalking" in appsettings.json.
/// Uses the Africa's Talking REST API directly via HttpClient.
/// </summary>
public class AfricasTalkingNotificationService : INotificationService
{
    private readonly IConfiguration _config;
    private readonly ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AfricasTalkingNotificationService> _logger;

    public AfricasTalkingNotificationService(
        IConfiguration config,
        ApplicationDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<AfricasTalkingNotificationService> logger)
    {
        _config = config;
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public Task<bool> SendSmsAsync(
        string patientPhone,
        string message,
        CancellationToken cancellationToken = default)
    {
        var phone = NormalizeNigerianPhone(patientPhone);
        return SendSmsAndLog(phone, message, null, cancellationToken);
    }

    public async Task<bool> SendConfirmationAsync(NotificationRequest request)
    {
        var phone = NormalizeNigerianPhone(request.PatientPhone);
        var message = $"Hello {request.PatientName}, appt confirmed at BP Okafor Hospital: " +
                      $"{request.AppointmentDateTime:MMM d} {request.AppointmentDateTime:h:mm tt} " +
                      $"with {request.DoctorName}. Ref: {request.ConfirmationRef}. " +
                      $"Arrive 15min early. Call: {_config["Notifications:HospitalPhone"]}";

        return await SendSmsAndLog(phone, message, request);
    }

    public async Task<bool> SendAdminAlertAsync(NotificationRequest request)
    {
        var adminPhone = NormalizeNigerianPhone(_config["Notifications:AdminPhone"] ?? "");
        var message = $"[New Booking] {request.PatientName} booked " +
                      $"{request.AppointmentDateTime:MMM d h:mm tt} with {request.DoctorName} " +
                      $"({request.Department}). Ref: {request.ConfirmationRef}.";

        return await SendSmsAndLog(adminPhone, message, request);
    }

    public async Task<bool> SendReminderAsync(NotificationRequest request)
    {
        var phone = NormalizeNigerianPhone(request.PatientPhone);
        var message = $"Reminder: Appt tomorrow {request.AppointmentDateTime:MMM d h:mm tt} " +
                      $"with {request.DoctorName} at BP Okafor Hospital. " +
                      $"Reschedule? Call: {_config["Notifications:HospitalPhone"]}. Ref: {request.ConfirmationRef}";

        return await SendSmsAndLog(phone, message, request);
    }

    // ──────────────────────────────────────────────────────────
    public async Task<bool> SendTeleconsultationReceivedAsync(NotificationRequest request)
    {
        var phone = NormalizeNigerianPhone(request.PatientPhone);
        var message = $"Hello {request.PatientName}, your teleconsultation request was received by BP Okafor Hospital. " +
                      $"Preferred: {request.AppointmentDateTime:MMM d h:mm tt}. Ref: {request.ConfirmationRef}. " +
                      "We will contact you after clinical review.";

        return await SendSmsAndLog(phone, message, request);
    }

    public async Task<bool> SendAppointmentStatusAsync(NotificationRequest request, string status, string nextStep)
    {
        var phone = NormalizeNigerianPhone(request.PatientPhone);
        var message = $"Appointment {status}: {request.AppointmentDateTime:MMM d h:mm tt}, " +
                      $"{request.Department}. Ref: {request.ConfirmationRef}. Next: {nextStep}";

        return await SendSmsAndLog(phone, message, request);
    }

    public async Task<bool> SendTeleconsultationStatusAsync(NotificationRequest request, string status, string nextStep)
    {
        var phone = NormalizeNigerianPhone(request.PatientPhone);
        var message = $"Teleconsultation {status}: {request.AppointmentDateTime:MMM d h:mm tt}, " +
                      $"{request.Department}. Ref: {request.ConfirmationRef}. Next: {nextStep}";

        return await SendSmsAndLog(phone, message, request);
    }

    // Private helpers
    // ──────────────────────────────────────────────────────────

    private async Task<bool> SendSmsAndLog(
        string phone,
        string message,
        NotificationRequest? request,
        CancellationToken cancellationToken = default)
    {
        var log = new NotificationLog
        {
            Channel = "SMS",
            Recipient = phone,
            MessageBody = message,
            AppointmentRequestId = request?.AppointmentRequestId,
            TeleconsultationRequestId = request?.TeleconsultationRequestId,
            DeliveryStatus = "submitted",
            SentAt = DateTime.UtcNow
        };

        try
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                log.Success = false;
                log.ErrorMessage = "Recipient phone is empty.";
            }
            else
            {
                var apiKey = _config["Notifications:AfricasTalking:ApiKey"];
                var username = _config["Notifications:AfricasTalking:Username"];
                var senderId = _config["Notifications:AfricasTalking:SenderId"];

                if (string.IsNullOrWhiteSpace(apiKey) ||
                    string.IsNullOrWhiteSpace(username) ||
                    apiKey.StartsWith("REPLACE_", StringComparison.OrdinalIgnoreCase) ||
                    username.StartsWith("REPLACE_", StringComparison.OrdinalIgnoreCase))
                {
                    log.Success = false;
                    log.ErrorMessage = "AfricasTalking credentials are missing or placeholder values.";
                }
                else
                {
                    var configuredBaseUrl = _config["Notifications:AfricasTalking:BaseUrl"];
                    var isSandbox = string.Equals(username, "sandbox", StringComparison.OrdinalIgnoreCase);
                    var baseUrl = !string.IsNullOrWhiteSpace(configuredBaseUrl)
                        ? configuredBaseUrl.TrimEnd('/')
                        : (isSandbox ? "https://api.sandbox.africastalking.com" : "https://api.africastalking.com");
                    var endpoint = $"{baseUrl}/version1/messaging";

                    var form = new Dictionary<string, string>
                    {
                        ["username"] = username,
                        ["to"] = phone,
                        ["message"] = message
                    };

                    if (!string.IsNullOrWhiteSpace(senderId))
                        form["from"] = senderId;

                    var client = _httpClientFactory.CreateClient();
                    using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
                    {
                        Content = new FormUrlEncodedContent(form)
                    };
                    httpRequest.Headers.Add("apiKey", apiKey);

                    using var response = await client.SendAsync(httpRequest, cancellationToken);
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);

                    log.Success = IsSuccessfulSmsResponse(response.IsSuccessStatusCode, body);
                    if (!log.Success)
                    {
                        log.ErrorMessage = $"AfricasTalking API failed: HTTP {(int)response.StatusCode}; Body: {Truncate(body, 1000)}";
                    }
                    else
                    {
                        _logger.LogInformation("AT SMS sent to {Phone}.", phone);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Africa's Talking SMS failed for {Phone}", phone);
            log.Success = false;
            log.ErrorMessage = ex.Message;
        }

        _context.NotificationLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);

        return log.Success;
    }

    private static bool IsSuccessfulSmsResponse(bool httpSuccess, string body)
    {
        if (!httpSuccess)
            return false;

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("SMSMessageData", out var smsData))
                return true;

            if (!smsData.TryGetProperty("Recipients", out var recipients) || recipients.ValueKind != JsonValueKind.Array)
                return true;

            var anyRecipient = false;
            foreach (var recipient in recipients.EnumerateArray())
            {
                anyRecipient = true;
                if (!recipient.TryGetProperty("status", out var statusElement))
                    return false;

                var status = statusElement.GetString() ?? string.Empty;
                if (!status.Contains("success", StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return anyRecipient;
        }
        catch
        {
            // If parsing fails but HTTP succeeded, treat as success and rely on logs.
            return true;
        }
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;

        return text[..maxLength] + "...";
    }

    /// <summary>
    /// Normalises Nigerian phone numbers to +234XXXXXXXXXX format.
    /// Accepts: 08012345678 | 2348012345678 | +2348012345678
    /// </summary>
    private static string NormalizeNigerianPhone(string phone)
    {
        return NigerianPhoneNumber.NormalizeToE164(phone);
    }
}
