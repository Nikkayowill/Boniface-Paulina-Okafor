using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.Services;
using System.Reflection;

namespace Okafor.NET.Tests;

public sealed class NotificationDeliveryTests
{
    [Fact]
    public async Task LeanNotificationService_WhenSmtpIsMissing_LogsFailureWithoutCallingEmailSender()
    {
        await using var context = CreateContext();
        var emailSender = new RecordingEmailSender();
        var service = new LeanNotificationService(emailSender, context, CreateConfiguration(new Dictionary<string, string?>
        {
            ["Email:SmtpHost"] = "",
            ["Email:FromAddress"] = "info@example.com"
        }));

        var sent = await service.SendReminderAsync(CreateNotificationRequest());

        var log = await context.NotificationLogs.SingleAsync();
        Assert.False(sent);
        Assert.Empty(emailSender.Messages);
        Assert.False(log.Success);
        Assert.Equal("failed", log.DeliveryStatus);
        Assert.Contains("SMTP", log.ErrorMessage);
    }

    [Fact]
    public async Task AppointmentReminderService_WhenDeliveryFails_DoesNotMarkReminderSent()
    {
        await using var context = CreateContext();
        var slot = await SeedReminderSlotAsync(context);
        var notifications = new FailingNotificationService();
        using var provider = new ServiceCollection()
            .AddSingleton(context)
            .AddSingleton<INotificationService>(notifications)
            .BuildServiceProvider();
        var service = new AppointmentReminderService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<AppointmentReminderService>.Instance);

        await InvokeProcessRemindersAsync(service);

        var updatedSlot = await context.AppointmentSlots.FindAsync(slot.Id);
        Assert.NotNull(updatedSlot);
        Assert.False(updatedSlot.ReminderSent);
        Assert.Single(notifications.ReminderRequests);
    }

    private static async Task InvokeProcessRemindersAsync(AppointmentReminderService service)
    {
        var method = typeof(AppointmentReminderService)
            .GetMethod("ProcessRemindersAsync", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);
        var task = Assert.IsAssignableFrom<Task>(method.Invoke(service, null));
        await task;
    }

    private static async Task<AppointmentSlot> SeedReminderSlotAsync(ApplicationDbContext context)
    {
        var department = new Department { Name = "General Medicine" };
        var doctor = new Doctor
        {
            FullName = "Dr. Ada Okafor",
            Department = department,
            Specialty = "General Medicine",
            Bio = "Test",
            Qualifications = "Test"
        };
        var appointmentTime = DateTime.Now.AddHours(24);
        var appointment = new AppointmentRequest
        {
            PatientName = "Chika Nwosu",
            Email = "chika@example.com",
            Phone = "08012345678",
            Department = department,
            Doctor = doctor,
            PreferredDate = appointmentTime.Date,
            PreferredTime = appointmentTime.ToString("HH:mm"),
            Status = AppointmentStatus.Approved
        };
        var slot = new AppointmentSlot
        {
            Doctor = doctor,
            SlotDateTime = appointmentTime,
            IsBooked = true,
            ReminderSent = false,
            AppointmentRequest = appointment
        };

        context.AppointmentSlots.Add(slot);
        await context.SaveChangesAsync();
        return slot;
    }

    private static NotificationRequest CreateNotificationRequest()
    {
        return new NotificationRequest
        {
            PatientName = "Chika Nwosu",
            PatientEmail = "chika@example.com",
            PatientPhone = "08012345678",
            DoctorName = "Dr. Ada Okafor",
            Department = "General Medicine",
            AppointmentDateTime = DateTime.Now.AddDays(1),
            ConfirmationRef = "00000001"
        };
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public List<(string Email, string Subject, string Body)> Messages { get; } = [];

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            Messages.Add((email, subject, htmlMessage));
            return Task.CompletedTask;
        }
    }

    private sealed class FailingNotificationService : INotificationService
    {
        public List<NotificationRequest> ReminderRequests { get; } = [];

        public Task<bool> SendConfirmationAsync(NotificationRequest request) => Task.FromResult(false);

        public Task<bool> SendAdminAlertAsync(NotificationRequest request) => Task.FromResult(false);

        public Task<bool> SendReminderAsync(NotificationRequest request)
        {
            ReminderRequests.Add(request);
            return Task.FromResult(false);
        }

        public Task<bool> SendTeleconsultationReceivedAsync(NotificationRequest request) => Task.FromResult(false);

        public Task<bool> SendTeleconsultationStatusAsync(NotificationRequest request, string status, string nextStep) => Task.FromResult(false);
    }
}
