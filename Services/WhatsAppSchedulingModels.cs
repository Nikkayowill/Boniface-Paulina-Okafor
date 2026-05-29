namespace Okafor_.NET.Services;

public sealed class SchedulingIntent
{
    public string AppointmentType { get; set; } = string.Empty;
    public DateTime? PreferredDate { get; set; }
    public string PreferredTimeWindow { get; set; } = string.Empty;
    public List<string> MissingFields { get; set; } = [];

    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(AppointmentType) &&
        PreferredDate.HasValue &&
        !string.IsNullOrWhiteSpace(PreferredTimeWindow) &&
        MissingFields.Count == 0;
}

public sealed class AppointmentSlotDto
{
    public int SlotId { get; set; }
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public DateTime SlotDateTime { get; set; }
    public string FormattedDateTime { get; set; } = string.Empty;
}

public interface IAiSchedulingService
{
    Task<SchedulingIntent> ParseAppointmentRequestAsync(string message, CancellationToken cancellationToken = default);
}

public interface IWhatsAppAppointmentSlotService
{
    Task<List<AppointmentSlotDto>> FindAvailableSlotsAsync(
        string specialty,
        DateTime preferredDate,
        string timeWindow,
        CancellationToken cancellationToken = default);
}

public interface IWhatsAppAppointmentResponseService
{
    Task SendSlotOptionsAsync(
        string patientPhoneNumber,
        List<AppointmentSlotDto> slots,
        CancellationToken cancellationToken = default);

    Task SendFallbackPromptAsync(
        string patientPhoneNumber,
        IReadOnlyCollection<string> missingFields,
        CancellationToken cancellationToken = default);

    Task SendInvalidSelectionAsync(
        string patientPhoneNumber,
        int optionCount,
        CancellationToken cancellationToken = default);

    Task SendNoActiveSessionAsync(
        string patientPhoneNumber,
        CancellationToken cancellationToken = default);

    Task SendBookingConfirmedAsync(
        string patientPhoneNumber,
        AppointmentSlotDto slot,
        int appointmentRequestId,
        CancellationToken cancellationToken = default);

    Task SendBookingFailedAsync(
        string patientPhoneNumber,
        string reason,
        CancellationToken cancellationToken = default);
}

public interface IWhatsAppSchedulingConversationService
{
    Task HandleInboundTextAsync(string patientPhoneNumber, string message, CancellationToken cancellationToken = default);
}

public sealed class WhatsAppBookingConfirmationResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int? AppointmentRequestId { get; set; }
    public AppointmentSlotDto? Slot { get; set; }
    public int OptionCount { get; set; }
}

public interface IWhatsAppSchedulingSessionService
{
    Task SaveSlotOptionsAsync(
        string patientPhoneNumber,
        List<AppointmentSlotDto> slots,
        CancellationToken cancellationToken = default);

    Task<WhatsAppBookingConfirmationResult> TryConfirmSelectionAsync(
        string patientPhoneNumber,
        int selectedOptionNumber,
        CancellationToken cancellationToken = default);
}

public interface IResilientPatientMessagingService
{
    Task<bool> SendCriticalAlertAsync(string patientPhone, string message, CancellationToken cancellationToken = default);
}
