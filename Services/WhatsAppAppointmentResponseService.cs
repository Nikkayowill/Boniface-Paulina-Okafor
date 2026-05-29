using System.Text;

namespace Okafor_.NET.Services;

public sealed class WhatsAppAppointmentResponseService : IWhatsAppAppointmentResponseService
{
    private readonly IWhatsAppNotificationService _whatsAppNotifications;
    private readonly ILogger<WhatsAppAppointmentResponseService> _logger;

    public WhatsAppAppointmentResponseService(
        IWhatsAppNotificationService whatsAppNotifications,
        ILogger<WhatsAppAppointmentResponseService> logger)
    {
        _whatsAppNotifications = whatsAppNotifications;
        _logger = logger;
    }

    public async Task SendSlotOptionsAsync(
        string patientPhoneNumber,
        List<AppointmentSlotDto> slots,
        CancellationToken cancellationToken = default)
    {
        var recipient = NigerianPhoneNumber.NormalizeForWhatsApp(patientPhoneNumber);
        if (string.IsNullOrWhiteSpace(recipient))
        {
            _logger.LogWarning("Cannot send WhatsApp slot options because {Phone} is invalid.", patientPhoneNumber);
            return;
        }

        var message = BuildSlotOptionsMessage(slots);
        await _whatsAppNotifications.SendTextMessageAsync(recipient, message, cancellationToken);
    }

    public async Task SendFallbackPromptAsync(
        string patientPhoneNumber,
        IReadOnlyCollection<string> missingFields,
        CancellationToken cancellationToken = default)
    {
        var recipient = NigerianPhoneNumber.NormalizeForWhatsApp(patientPhoneNumber);
        if (string.IsNullOrWhiteSpace(recipient))
            return;

        var missing = missingFields.Count == 0
            ? "the clinic type, preferred date, and time of day"
            : string.Join(", ", missingFields.Select(ToFriendlyFieldName));

        var message = $"""
            Boniface and Paulina Okafor Memorial Hospital

            I can help you request an appointment, but I need: {missing}.

            Please reply like this:
            "I want to see a pediatrician tomorrow morning"
            or
            "Maternity appointment on 2026-05-25 afternoon"
            """;

        await _whatsAppNotifications.SendTextMessageAsync(recipient, message, cancellationToken);
    }

    public Task SendInvalidSelectionAsync(
        string patientPhoneNumber,
        int optionCount,
        CancellationToken cancellationToken = default)
    {
        var message = optionCount > 0
            ? $"Please reply with a number between 1 and {optionCount} to choose one of the appointment times I just sent."
            : "Please request appointment times again before choosing a number.";

        return SendPlainTextAsync(patientPhoneNumber, message, cancellationToken);
    }

    public Task SendNoActiveSessionAsync(
        string patientPhoneNumber,
        CancellationToken cancellationToken = default)
    {
        const string message = """
            Boniface and Paulina Okafor Memorial Hospital

            I could not find recent appointment options for that reply.
            Please send a new request, for example:
            "I want to see a pediatrician tomorrow morning"
            """;

        return SendPlainTextAsync(patientPhoneNumber, message, cancellationToken);
    }

    public Task SendBookingConfirmedAsync(
        string patientPhoneNumber,
        AppointmentSlotDto slot,
        int appointmentRequestId,
        CancellationToken cancellationToken = default)
    {
        var message = $"""
            Boniface and Paulina Okafor Memorial Hospital

            Your appointment is confirmed.
            Ref: APPT-{appointmentRequestId:D6}
            Doctor: {slot.DoctorName}
            Time: {slot.FormattedDateTime}

            Please arrive 15 minutes early. Do not send private medical records on WhatsApp.
            """;

        return SendPlainTextAsync(patientPhoneNumber, message, cancellationToken);
    }

    public Task SendBookingFailedAsync(
        string patientPhoneNumber,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var message = $"""
            Boniface and Paulina Okafor Memorial Hospital

            I could not confirm that appointment time.
            Reason: {reason}

            Please send your appointment request again and I will look for fresh times.
            """;

        return SendPlainTextAsync(patientPhoneNumber, message, cancellationToken);
    }

    private static string BuildSlotOptionsMessage(IReadOnlyList<AppointmentSlotDto> slots)
    {
        if (slots.Count == 0)
        {
            return """
                Boniface and Paulina Okafor Memorial Hospital

                We could not find an available appointment time for that request.
                Please reply with another specialty, date, or time of day.
                Example: "General doctor tomorrow morning"
                """;
        }

        var builder = new StringBuilder();
        builder.AppendLine("Boniface and Paulina Okafor Memorial Hospital");
        builder.AppendLine();
        builder.AppendLine("We found the following available times:");

        for (var index = 0; index < slots.Count; index++)
        {
            var slot = slots[index];
            builder.AppendLine($"{index + 1}. {slot.DoctorName} - {slot.FormattedDateTime}");
        }

        builder.AppendLine();
        builder.AppendLine(slots.Count == 1
            ? "Please reply with 1 to continue."
            : $"Please reply with the number (1 to {slots.Count}) to continue.");
        builder.AppendLine("Do not send private medical records on WhatsApp.");

        return builder.ToString();
    }

    private static string ToFriendlyFieldName(string field)
    {
        return field switch
        {
            "AppointmentType" => "the clinic or specialty",
            "PreferredDate" => "the preferred date",
            "PreferredTimeWindow" => "morning, afternoon, evening, or any time",
            _ => field
        };
    }

    private async Task SendPlainTextAsync(
        string patientPhoneNumber,
        string message,
        CancellationToken cancellationToken)
    {
        var recipient = NigerianPhoneNumber.NormalizeForWhatsApp(patientPhoneNumber);
        if (string.IsNullOrWhiteSpace(recipient))
            return;

        await _whatsAppNotifications.SendTextMessageAsync(recipient, message, cancellationToken);
    }
}
