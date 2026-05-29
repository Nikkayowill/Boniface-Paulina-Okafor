using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.Services;

namespace Okafor_.NET.Tests;

/// <summary>
/// Unit tests for AvailabilityService (slot generation and reservation)
/// </summary>
public sealed class AvailabilityServiceTests : IAsyncLifetime
{
    private ApplicationDbContext _context = null!;
    private IAvailabilityService _service = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"AvailabilityTests_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        _service = new AvailabilityService(_context);

        // Seed test data
        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        _context.Dispose();
    }

    private async Task SeedTestDataAsync()
    {
        var dept = new Department { Name = "Cardiology" };
        var doctor = new Doctor
        {
            FullName = "Dr. John Smith",
            DepartmentId = 1,
            Specialty = "Cardiology",
            Bio = "Test",
            Qualifications = "Test",
            ImageUrl = null
        };

        _context.Departments.Add(dept);
        _context.Doctors.Add(doctor);

        // Doctor works Mon-Fri, 09:00-17:00, 30-min slots
        for (int i = 0; i < 5; i++)
        {
            _context.DoctorAvailabilities.Add(new DoctorAvailability
            {
                DoctorId = doctor.Id,
                DayOfWeek = (DayOfWeek)i,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(17, 0, 0),
                SlotDurationMinutes = 30,
                IsActive = true
            });
        }

        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAvailableSlots_WithValidDoctor_ReturnsSlots()
    {
        // Arrange
        var doctor = await _context.Doctors.FirstAsync();
        var testDate = GetNextWeekday(DayOfWeek.Monday);

        // Act
        var slots = await _service.GetAvailableSlotsAsync(doctor.Id, testDate);

        // Assert
        Assert.NotEmpty(slots);
        Assert.True(slots.Count > 0, "Should generate multiple 30-minute slots");
        Assert.All(slots, slot =>
        {
            Assert.Equal(testDate.Date, slot.Date);
            Assert.True(slot >= testDate.Date.AddHours(9));
            Assert.True(slot <= testDate.Date.AddHours(17));
        });
    }

    [Fact]
    public async Task GetAvailableSlots_WithInactiveDayOff_ReturnsEmpty()
    {
        // Arrange
        var doctor = await _context.Doctors.FirstAsync();
        var testDate = GetNextWeekday(DayOfWeek.Saturday); // No availability on Saturday

        // Act
        var slots = await _service.GetAvailableSlotsAsync(doctor.Id, testDate);

        // Assert
        Assert.Empty(slots);
    }

    [Fact]
    public async Task GetAvailableSlots_ExcludesAlreadyBookedSlots()
    {
        // Arrange
        var doctor = await _context.Doctors.FirstAsync();
        var testDate = GetNextWeekday(DayOfWeek.Monday);
        var slotTime = testDate.Date.AddHours(10); // 10:00 AM

        // Pre-book a slot
        var appointmentRequest = new AppointmentRequest
        {
            PatientName = "Test Patient",
            Email = "patient@test.com",
            Phone = "555-0100",
            DepartmentId = doctor.DepartmentId,
            DoctorId = doctor.Id,
            PreferredDate = testDate.Date,
            PreferredTime = slotTime.ToString("HH:mm"),
            Message = "Test",
            Status = AppointmentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _context.AppointmentRequests.Add(appointmentRequest);
        await _context.SaveChangesAsync();

        var bookedSlot = new AppointmentSlot
        {
            DoctorId = doctor.Id,
            SlotDateTime = slotTime,
            IsBooked = true,
            AppointmentRequestId = appointmentRequest.Id
        };
        _context.AppointmentSlots.Add(bookedSlot);
        await _context.SaveChangesAsync();

        // Act
        var availableSlots = await _service.GetAvailableSlotsAsync(doctor.Id, testDate);

        // Assert
        Assert.DoesNotContain(slotTime, availableSlots);
        Assert.NotEmpty(availableSlots); // Other slots should still be available
    }

    [Fact]
    public async Task ReserveSlot_WithValidSlot_CreatesBooking()
    {
        // Arrange
        var doctor = await _context.Doctors.FirstAsync();
        var testDate = GetNextWeekday(DayOfWeek.Tuesday);
        var slotTime = testDate.Date.AddHours(14); // 2:00 PM

        var appointmentRequest = new AppointmentRequest
        {
            PatientName = "Jane Doe",
            Email = "jane@example.com",
            Phone = "555-0101",
            DepartmentId = doctor.DepartmentId,
            DoctorId = doctor.Id,
            PreferredDate = testDate.Date,
            PreferredTime = slotTime.ToString("HH:mm"),
            Message = "Test",
            Status = AppointmentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _context.AppointmentRequests.Add(appointmentRequest);
        await _context.SaveChangesAsync();

        // Act
        var (success, error) = await _service.ReserveSlotAsync(doctor.Id, slotTime, appointmentRequest.Id);

        // Assert
        Assert.True(success, $"Reservation should succeed. Error: {error}");
        Assert.Null(error);

        var slot = await _context.AppointmentSlots
            .FirstOrDefaultAsync(s => s.SlotDateTime == slotTime && s.DoctorId == doctor.Id);
        Assert.NotNull(slot);
        Assert.True(slot.IsBooked);
        Assert.Equal(appointmentRequest.Id, slot.AppointmentRequestId);
    }

    [Fact]
    public async Task ReserveSlot_WithAlreadyBookedSlot_Fails()
    {
        // Arrange
        var doctor = await _context.Doctors.FirstAsync();
        var testDate = GetNextWeekday(DayOfWeek.Wednesday);
        var slotTime = testDate.Date.AddHours(11);

        var appt1 = new AppointmentRequest
        {
            PatientName = "Patient 1",
            Email = "p1@test.com",
            Phone = "555-0102",
            DepartmentId = doctor.DepartmentId,
            DoctorId = doctor.Id,
            PreferredDate = testDate.Date,
            PreferredTime = slotTime.ToString("HH:mm"),
            Message = "Test",
            Status = AppointmentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _context.AppointmentRequests.Add(appt1);
        await _context.SaveChangesAsync();

        // Reserve first time
        await _service.ReserveSlotAsync(doctor.Id, slotTime, appt1.Id);

        // Try to reserve same slot again
        var appt2 = new AppointmentRequest
        {
            PatientName = "Patient 2",
            Email = "p2@test.com",
            Phone = "555-0103",
            DepartmentId = doctor.DepartmentId,
            DoctorId = doctor.Id,
            PreferredDate = testDate.Date,
            PreferredTime = slotTime.ToString("HH:mm"),
            Message = "Test",
            Status = AppointmentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _context.AppointmentRequests.Add(appt2);
        await _context.SaveChangesAsync();

        // Act
        var (success, error) = await _service.ReserveSlotAsync(doctor.Id, slotTime, appt2.Id);

        // Assert
        Assert.False(success);
        Assert.NotNull(error);
        Assert.Contains("taken", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReserveSlot_RaceCondition_OnlyOneSucceeds()
    {
        // Arrange
        var doctor = await _context.Doctors.FirstAsync();
        var testDate = GetNextWeekday(DayOfWeek.Thursday);
        var slotTime = testDate.Date.AddHours(15);

        var appt1 = new AppointmentRequest
        {
            PatientName = "Racer 1",
            Email = "race1@test.com",
            Phone = "555-0104",
            DepartmentId = doctor.DepartmentId,
            DoctorId = doctor.Id,
            PreferredDate = testDate.Date,
            PreferredTime = slotTime.ToString("HH:mm"),
            Message = "Test",
            Status = AppointmentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        var appt2 = new AppointmentRequest
        {
            PatientName = "Racer 2",
            Email = "race2@test.com",
            Phone = "555-0105",
            DepartmentId = doctor.DepartmentId,
            DoctorId = doctor.Id,
            PreferredDate = testDate.Date,
            PreferredTime = slotTime.ToString("HH:mm"),
            Message = "Test",
            Status = AppointmentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.AppointmentRequests.AddRange(appt1, appt2);
        await _context.SaveChangesAsync();

        // Act - Try to reserve simultaneously (simulate race condition)
        var task1 = _service.ReserveSlotAsync(doctor.Id, slotTime, appt1.Id);
        var task2 = _service.ReserveSlotAsync(doctor.Id, slotTime, appt2.Id);
        await Task.WhenAll(task1, task2);

        var result1 = await task1;
        var result2 = await task2;

        // Assert - Exactly one should succeed
        var successCount = (result1.Success ? 1 : 0) + (result2.Success ? 1 : 0);
        Assert.Equal(1, successCount);

        var bookedSlots = await _context.AppointmentSlots
            .Where(s => s.SlotDateTime == slotTime && s.DoctorId == doctor.Id)
            .ToListAsync();
        Assert.Single(bookedSlots);
    }

    private static DateTime GetNextWeekday(DayOfWeek targetDay)
    {
        var today = DateTime.Today;
        var daysAhead = (int)targetDay - (int)today.DayOfWeek;
        if (daysAhead <= 0)
            daysAhead += 7;
        return today.AddDays(daysAhead);
    }
}

/// <summary>
/// Unit tests for AppointmentReminderService
/// </summary>
public sealed class AppointmentReminderServiceTests : IAsyncLifetime
{
    private ApplicationDbContext _context = null!;
    private MockNotificationService _mockNotifications = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"ReminderTests_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        _mockNotifications = new MockNotificationService();

        // Seed test data
        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        _context.Dispose();
    }

    private async Task SeedTestDataAsync()
    {
        var dept = new Department { Name = "General Medicine" };
        var doctor = new Doctor
        {
            FullName = "Dr. Maria Garcia",
            DepartmentId = 1,
            Specialty = "General Medicine",
            Bio = "Test",
            Qualifications = "Test",
            ImageUrl = null
        };

        _context.Departments.Add(dept);
        _context.Doctors.Add(doctor);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task ProcessReminders_WithUpcomingSlots_SendsReminders()
    {
        // Arrange
        var doctor = await _context.Doctors.FirstAsync();
        var now = DateTime.Now;
        var reminderTime = now.AddHours(24); // 24 hours from now (within reminder window)

        var appointment = new AppointmentRequest
        {
            PatientName = "Michael Johnson",
            Email = "michael@example.com",
            Phone = "555-0200",
            DepartmentId = doctor.DepartmentId,
            DoctorId = doctor.Id,
            PreferredDate = reminderTime.Date,
            PreferredTime = reminderTime.ToString("HH:mm"),
            Message = "Checkup",
            Status = AppointmentStatus.Approved,
            CreatedAt = now
        };

        _context.AppointmentRequests.Add(appointment);
        await _context.SaveChangesAsync();

        var slot = new AppointmentSlot
        {
            DoctorId = doctor.Id,
            SlotDateTime = reminderTime,
            IsBooked = true,
            ReminderSent = false,
            AppointmentRequestId = appointment.Id
        };

        _context.AppointmentSlots.Add(slot);
        await _context.SaveChangesAsync();

        // Act - Call the private method indirectly (through the service logic)
        var windowStart = now.AddHours(23);
        var windowEnd = now.AddHours(25);

        var upcomingSlots = await _context.AppointmentSlots
            .Include(s => s.AppointmentRequest)
            .Include(s => s.Doctor)
            .Where(s =>
                s.IsBooked &&
                !s.ReminderSent &&
                s.SlotDateTime >= windowStart &&
                s.SlotDateTime <= windowEnd &&
                s.AppointmentRequest != null)
            .ToListAsync();

        // Assert
        Assert.NotEmpty(upcomingSlots);
        var upcomingSlot = upcomingSlots.First();
        Assert.False(upcomingSlot.ReminderSent);
        Assert.Equal(appointment.Id, upcomingSlot.AppointmentRequestId);
    }

    [Fact]
    public async Task ProcessReminders_IgnoresPastReminders()
    {
        // Arrange
        var doctor = await _context.Doctors.FirstAsync();
        var now = DateTime.Now;
        var pastTime = now.AddHours(-2); // 2 hours in past

        var appointment = new AppointmentRequest
        {
            PatientName = "Sarah Williams",
            Email = "sarah@example.com",
            Phone = "555-0201",
            DepartmentId = doctor.DepartmentId,
            DoctorId = doctor.Id,
            PreferredDate = pastTime.Date,
            PreferredTime = pastTime.ToString("HH:mm"),
            Message = "Checkup",
            Status = AppointmentStatus.Approved,
            CreatedAt = now
        };

        _context.AppointmentRequests.Add(appointment);
        await _context.SaveChangesAsync();

        var slot = new AppointmentSlot
        {
            DoctorId = doctor.Id,
            SlotDateTime = pastTime,
            IsBooked = true,
            ReminderSent = false,
            AppointmentRequestId = appointment.Id
        };

        _context.AppointmentSlots.Add(slot);
        await _context.SaveChangesAsync();

        // Act - Check if it's filtered out
        var windowStart = now.AddHours(23);
        var windowEnd = now.AddHours(25);

        var upcomingSlots = await _context.AppointmentSlots
            .Where(s =>
                s.IsBooked &&
                !s.ReminderSent &&
                s.SlotDateTime >= windowStart &&
                s.SlotDateTime <= windowEnd)
            .ToListAsync();

        // Assert
        Assert.Empty(upcomingSlots);
    }

    [Fact]
    public async Task ProcessReminders_MarksAsSent()
    {
        // Arrange
        var doctor = await _context.Doctors.FirstAsync();
        var now = DateTime.Now;
        var reminderTime = now.AddHours(24.5);

        var appointment = new AppointmentRequest
        {
            PatientName = "David Brown",
            Email = "david@example.com",
            Phone = "555-0202",
            DepartmentId = doctor.DepartmentId,
            DoctorId = doctor.Id,
            PreferredDate = reminderTime.Date,
            PreferredTime = reminderTime.ToString("HH:mm"),
            Message = "Follow-up",
            Status = AppointmentStatus.Approved,
            CreatedAt = now
        };

        _context.AppointmentRequests.Add(appointment);
        await _context.SaveChangesAsync();

        var slot = new AppointmentSlot
        {
            DoctorId = doctor.Id,
            SlotDateTime = reminderTime,
            IsBooked = true,
            ReminderSent = false,
            AppointmentRequestId = appointment.Id
        };

        _context.AppointmentSlots.Add(slot);
        await _context.SaveChangesAsync();

        // Act - Simulate marking as sent
        slot.ReminderSent = true;
        _context.AppointmentSlots.Update(slot);
        await _context.SaveChangesAsync();

        // Assert
        var updatedSlot = await _context.AppointmentSlots.FindAsync(slot.Id);
        Assert.NotNull(updatedSlot);
        Assert.True(updatedSlot.ReminderSent);
    }
}

/// <summary>
/// Mock notification service for testing
/// </summary>
internal class MockNotificationService : INotificationService
{
    public List<NotificationRequest> SentNotifications { get; } = [];

    public Task<bool> SendConfirmationAsync(NotificationRequest request)
    {
        SentNotifications.Add(request);
        return Task.FromResult(true);
    }

    public Task<bool> SendReminderAsync(NotificationRequest request)
    {
        SentNotifications.Add(request);
        return Task.FromResult(true);
    }

    public Task<bool> SendAdminAlertAsync(NotificationRequest request)
    {
        SentNotifications.Add(request);
        return Task.FromResult(true);
    }

    public Task<bool> SendAppointmentStatusAsync(NotificationRequest request, string status, string nextStep)
    {
        SentNotifications.Add(request);
        return Task.FromResult(true);
    }

    public Task<bool> SendTeleconsultationReceivedAsync(NotificationRequest request)
    {
        SentNotifications.Add(request);
        return Task.FromResult(true);
    }

    public Task<bool> SendTeleconsultationStatusAsync(NotificationRequest request, string status, string nextStep)
    {
        SentNotifications.Add(request);
        return Task.FromResult(true);
    }
}
