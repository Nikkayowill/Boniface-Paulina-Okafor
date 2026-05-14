using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Services;

namespace Okafor_.NET.Services;

public class AppointmentReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AppointmentReminderService> _logger;

    public AppointmentReminderService(
        IServiceScopeFactory scopeFactory,
        ILogger<AppointmentReminderService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AppointmentReminderService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRemindersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in AppointmentReminderService loop.");
            }

            // Wait 60 minutes before next run
            await Task.Delay(TimeSpan.FromMinutes(60), stoppingToken);
        }
    }

    private async Task ProcessRemindersAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var windowStart = DateTime.Now.AddHours(23);
        var windowEnd = DateTime.Now.AddHours(25);

        var upcomingSlots = await context.AppointmentSlots
            .Include(s => s.AppointmentRequest)
            .Include(s => s.Doctor)
                .ThenInclude(d => d.Department)
            .Where(s =>
                s.IsBooked &&
                !s.ReminderSent &&
                s.SlotDateTime >= windowStart &&
                s.SlotDateTime <= windowEnd &&
                s.AppointmentRequest != null)
            .ToListAsync();

        if (upcomingSlots.Count == 0)
        {
            _logger.LogInformation("No reminders to send.");
            return;
        }

        _logger.LogInformation("Sending {Count} appointment reminders.", upcomingSlots.Count);

        foreach (var slot in upcomingSlots)
        {
            try
            {
                var notifRequest = new NotificationRequest
                {
                    PatientName = slot.AppointmentRequest!.PatientName,
                    PatientEmail = slot.AppointmentRequest.Email,
                    PatientPhone = slot.AppointmentRequest.Phone,
                    DoctorName = slot.Doctor.FullName,
                    Department = slot.Doctor.Department?.Name ?? string.Empty,
                    AppointmentDateTime = slot.SlotDateTime,
                    ConfirmationRef = slot.AppointmentRequest.Id.ToString("D8"),
                    AppointmentRequestId = slot.AppointmentRequestId
                };

                var sent = await notifications.SendReminderAsync(notifRequest);
                if (sent)
                {
                    slot.ReminderSent = true;
                    await context.SaveChangesAsync();
                }

                _logger.LogInformation(
                    "Reminder {Status} for slot {SlotId} → {Patient}",
                    sent ? "sent" : "failed", slot.Id, slot.AppointmentRequest.PatientName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send reminder for slot {SlotId}.", slot.Id);
            }
        }
    }
}
