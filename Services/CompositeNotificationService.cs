namespace Okafor_.NET.Services;

public sealed class CompositeNotificationService : INotificationService
{
    private readonly LeanNotificationService _emailNotifications;
    private readonly AfricasTalkingNotificationService _smsNotifications;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CompositeNotificationService> _logger;

    public CompositeNotificationService(
        LeanNotificationService emailNotifications,
        AfricasTalkingNotificationService smsNotifications,
        IConfiguration configuration,
        ILogger<CompositeNotificationService> logger)
    {
        _emailNotifications = emailNotifications;
        _smsNotifications = smsNotifications;
        _configuration = configuration;
        _logger = logger;
    }

    public Task<bool> SendConfirmationAsync(NotificationRequest request)
    {
        return SendAsync(
            email => email.SendConfirmationAsync(request),
            sms => sms.SendConfirmationAsync(request));
    }

    public Task<bool> SendAdminAlertAsync(NotificationRequest request)
    {
        return SendAsync(
            email => email.SendAdminAlertAsync(request),
            sms => sms.SendAdminAlertAsync(request));
    }

    public Task<bool> SendReminderAsync(NotificationRequest request)
    {
        return SendAsync(
            email => email.SendReminderAsync(request),
            sms => sms.SendReminderAsync(request));
    }

    public Task<bool> SendTeleconsultationReceivedAsync(NotificationRequest request)
    {
        return SendAsync(
            email => email.SendTeleconsultationReceivedAsync(request),
            sms => sms.SendTeleconsultationReceivedAsync(request));
    }

    public Task<bool> SendTeleconsultationStatusAsync(NotificationRequest request, string status, string nextStep)
    {
        return SendAsync(
            email => email.SendTeleconsultationStatusAsync(request, status, nextStep),
            sms => sms.SendTeleconsultationStatusAsync(request, status, nextStep));
    }

    private async Task<bool> SendAsync(
        Func<LeanNotificationService, Task<bool>> sendEmail,
        Func<AfricasTalkingNotificationService, Task<bool>> sendSms)
    {
        var provider = _configuration["Notifications:Provider"];
        var isAuto = IntegrationConfiguration.IsAutoProvider(_configuration, "Notifications:Provider");
        var sendEmailChannel = ShouldUseEmail(provider, isAuto);
        var sendSmsChannel = ShouldUseSms(provider, isAuto);

        if (!sendEmailChannel && !sendSmsChannel)
        {
            _logger.LogWarning("No notification channel is configured. Falling back to email so the failure is logged.");
            return await sendEmail(_emailNotifications);
        }

        var delivered = false;

        if (sendEmailChannel)
        {
            delivered |= await sendEmail(_emailNotifications);
        }

        if (sendSmsChannel)
        {
            delivered |= await sendSms(_smsNotifications);
        }

        return delivered;
    }

    private bool ShouldUseEmail(string? provider, bool isAuto)
    {
        if (isAuto)
        {
            return IntegrationConfiguration.HasSmtpSettings(_configuration);
        }

        return string.Equals(provider, "Lean", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(provider, "Email", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(provider, "Composite", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(provider, "All", StringComparison.OrdinalIgnoreCase);
    }

    private bool ShouldUseSms(string? provider, bool isAuto)
    {
        if (isAuto)
        {
            return IntegrationConfiguration.HasAfricasTalkingCredentials(_configuration);
        }

        return string.Equals(provider, "AfricasTalking", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(provider, "Sms", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(provider, "Composite", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(provider, "All", StringComparison.OrdinalIgnoreCase);
    }
}
