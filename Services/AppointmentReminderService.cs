using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Okafor_.NET.Data;

namespace Okafor_.NET.Services;

public class AppointmentReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AppointmentReminderService> _logger;
    private readonly BackgroundTaskOptions _options;

    public AppointmentReminderService(
        IServiceScopeFactory scopeFactory,
        ILogger<AppointmentReminderService> logger,
        IOptions<BackgroundTaskOptions>? options = null)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options?.Value ?? new BackgroundTaskOptions();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.AppointmentRemindersEnabled)
        {
            _logger.LogWarning("Appointment reminders are disabled by configuration.");
            return;
        }

        var intervalMinutes = Math.Clamp(_options.AppointmentReminderIntervalMinutes, 5, 1440);
        var interval = TimeSpan.FromMinutes(intervalMinutes);
        _logger.LogInformation("AppointmentReminderService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRemindersCoreAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in AppointmentReminderService loop.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private Task ProcessRemindersAsync()
    {
        return ProcessRemindersCoreAsync(CancellationToken.None);
    }

    private async Task ProcessRemindersCoreAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var windowStart = DateTime.Now.AddHours(23);
        var windowEnd = DateTime.Now.AddHours(25);

        // Project only the scalar fields the reminder content actually needs (patient contact info,
        // doctor/department display names) instead of materializing full Doctor/Department entity
        // graphs via Include/ThenInclude. The AppointmentSlot itself (`Slot`) stays a tracked entity
        // in this projection so ReminderSent can still be updated and saved below.
        var upcomingSlots = await context.AppointmentSlots
            .Where(s =>
                s.IsBooked &&
                !s.ReminderSent &&
                s.SlotDateTime >= windowStart &&
                s.SlotDateTime <= windowEnd &&
                s.AppointmentRequest != null)
            .Select(s => new
            {
                Slot = s,
                PatientName = s.AppointmentRequest!.PatientName,
                PatientEmail = s.AppointmentRequest.Email,
                PatientPhone = s.AppointmentRequest.Phone,
                AppointmentRequestRecordId = s.AppointmentRequest.Id,
                DoctorName = s.Doctor.FullName,
                DepartmentName = s.Doctor.Department != null ? s.Doctor.Department.Name : null
            })
            .ToListAsync(cancellationToken);

        if (upcomingSlots.Count == 0)
        {
            _logger.LogInformation("No reminders to send.");
            return;
        }

        _logger.LogInformation("Sending {Count} appointment reminders.", upcomingSlots.Count);

        // SaveChangesAsync is intentionally called per-reminder rather than once after the loop:
        // notifications.SendReminderAsync has a real external side effect (it dispatches the
        // reminder to the patient). Committing ReminderSent=true immediately after each successful
        // send means that if this run is cancelled or crashes partway through the batch, reminders
        // already sent are not resent on the next run. Deferring all commits to the end of the loop
        // would risk duplicate reminder notifications to patients on partial failure.
        foreach (var item in upcomingSlots)
        {
            var slot = item.Slot;
            try
            {
                var notifRequest = new NotificationRequest
                {
                    PatientName = item.PatientName,
                    PatientEmail = item.PatientEmail,
                    PatientPhone = item.PatientPhone,
                    DoctorName = item.DoctorName,
                    Department = item.DepartmentName ?? string.Empty,
                    AppointmentDateTime = slot.SlotDateTime,
                    ConfirmationRef = item.AppointmentRequestRecordId.ToString("D8"),
                    AppointmentRequestId = slot.AppointmentRequestId
                };

                var sent = await notifications.SendReminderAsync(notifRequest);
                if (sent)
                {
                    slot.ReminderSent = true;
                    await context.SaveChangesAsync(cancellationToken);
                }

                _logger.LogInformation(
                    "Reminder {Status} for slot {SlotId} → {Patient}",
                    sent ? "sent" : "failed", slot.Id, item.PatientName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send reminder for slot {SlotId}.", slot.Id);
            }
        }
    }
}
