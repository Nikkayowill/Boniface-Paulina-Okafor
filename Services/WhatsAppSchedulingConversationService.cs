namespace Okafor_.NET.Services;

public sealed class WhatsAppSchedulingConversationService : IWhatsAppSchedulingConversationService
{
    private readonly IAiSchedulingService _aiSchedulingService;
    private readonly IWhatsAppAppointmentSlotService _slotService;
    private readonly IWhatsAppSchedulingSessionService _sessionService;
    private readonly IWhatsAppAppointmentResponseService _responseService;
    private readonly ILogger<WhatsAppSchedulingConversationService> _logger;

    public WhatsAppSchedulingConversationService(
        IAiSchedulingService aiSchedulingService,
        IWhatsAppAppointmentSlotService slotService,
        IWhatsAppSchedulingSessionService sessionService,
        IWhatsAppAppointmentResponseService responseService,
        ILogger<WhatsAppSchedulingConversationService> logger)
    {
        _aiSchedulingService = aiSchedulingService;
        _slotService = slotService;
        _sessionService = sessionService;
        _responseService = responseService;
        _logger = logger;
    }

    public async Task HandleInboundTextAsync(
        string patientPhoneNumber,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (int.TryParse(message.Trim(), out var selectedOptionNumber))
        {
            var result = await _sessionService.TryConfirmSelectionAsync(
                patientPhoneNumber,
                selectedOptionNumber,
                cancellationToken);

            if (result.Success && result.Slot is not null && result.AppointmentRequestId.HasValue)
            {
                await _responseService.SendBookingConfirmedAsync(
                    patientPhoneNumber,
                    result.Slot,
                    result.AppointmentRequestId.Value,
                    cancellationToken);
                return;
            }

            if (result.Error == "Invalid selection.")
            {
                await _responseService.SendInvalidSelectionAsync(
                    patientPhoneNumber,
                    result.OptionCount,
                    cancellationToken);
                return;
            }

            if (result.Error == "No active appointment options found.")
            {
                await _responseService.SendNoActiveSessionAsync(patientPhoneNumber, cancellationToken);
                return;
            }

            await _responseService.SendBookingFailedAsync(
                patientPhoneNumber,
                result.Error ?? "That time could not be reserved.",
                cancellationToken);
            return;
        }

        var intent = await _aiSchedulingService.ParseAppointmentRequestAsync(message, cancellationToken);
        if (!intent.IsComplete)
        {
            await _responseService.SendFallbackPromptAsync(patientPhoneNumber, intent.MissingFields, cancellationToken);
            return;
        }

        var slots = await _slotService.FindAvailableSlotsAsync(
            intent.AppointmentType,
            intent.PreferredDate!.Value,
            intent.PreferredTimeWindow,
            cancellationToken);

        _logger.LogInformation(
            "WhatsApp appointment request parsed as {AppointmentType} on {PreferredDate} during {TimeWindow}; found {SlotCount} slots.",
            intent.AppointmentType,
            intent.PreferredDate.Value.ToString("yyyy-MM-dd"),
            intent.PreferredTimeWindow,
            slots.Count);

        await _sessionService.SaveSlotOptionsAsync(patientPhoneNumber, slots, cancellationToken);
        await _responseService.SendSlotOptionsAsync(patientPhoneNumber, slots, cancellationToken);
    }
}
