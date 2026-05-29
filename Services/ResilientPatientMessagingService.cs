namespace Okafor_.NET.Services;

public sealed class ResilientPatientMessagingService : IResilientPatientMessagingService
{
    private readonly IWhatsAppNotificationService _whatsAppNotifications;
    private readonly AfricasTalkingNotificationService _smsNotifications;
    private readonly ILogger<ResilientPatientMessagingService> _logger;

    public ResilientPatientMessagingService(
        IWhatsAppNotificationService whatsAppNotifications,
        AfricasTalkingNotificationService smsNotifications,
        ILogger<ResilientPatientMessagingService> logger)
    {
        _whatsAppNotifications = whatsAppNotifications;
        _smsNotifications = smsNotifications;
        _logger = logger;
    }

    public async Task<bool> SendCriticalAlertAsync(
        string patientPhone,
        string message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var whatsAppDelivered = await _whatsAppNotifications.SendTextMessageAsync(
                patientPhone,
                message,
                cancellationToken);

            if (whatsAppDelivered)
            {
                _logger.LogInformation("Critical patient alert sent through WhatsApp to {Phone}.", patientPhone);
                return true;
            }

            _logger.LogWarning("WhatsApp critical alert did not deliver to {Phone}; falling back to SMS.", patientPhone);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            _logger.LogWarning(ex, "WhatsApp critical alert failed for {Phone}; falling back to SMS.", patientPhone);
        }

        var smsDelivered = await _smsNotifications.SendSmsAsync(patientPhone, message, cancellationToken);
        if (smsDelivered)
        {
            _logger.LogInformation("Critical patient alert sent through SMS fallback to {Phone}.", patientPhone);
        }
        else
        {
            _logger.LogError("Critical patient alert failed on both WhatsApp and SMS for {Phone}.", patientPhone);
        }

        return smsDelivered;
    }
}
