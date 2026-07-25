using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Okafor_.NET.Areas.Admin.Controllers;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.Services;

namespace Okafor_.NET.Tests;

/// <summary>
/// Covers the /Admin/Availability endpoints (SaveAvailability, GenerateSlots) that the browser-side
/// availabilityManager() Alpine component calls. These endpoints previously had no automated coverage,
/// and the admin page that calls them was unreachable in a real browser because Alpine.js was never
/// loaded in the admin layout (fixed by vendoring Alpine locally and loading it on this page only).
/// This test proves the server-side contract those calls depend on is correct, and that saving a
/// weekly template actually unlocks the public-facing slot picker (AvailabilityService.GetAvailableSlotsAsync),
/// which is the real patient-facing feature that was starved while the page was dead.
/// </summary>
public sealed class AvailabilityControllerTests : IAsyncLifetime
{
    private ApplicationDbContext _context = null!;
    private AvailabilityController _controller = null!;
    private int _doctorId;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"AvailabilityControllerTests_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        var department = new Department { Name = "General Medicine" };
        var doctor = new Doctor
        {
            FullName = "Dr. Amaka Nwosu",
            Department = department,
            Specialty = "General Medicine",
            Bio = "Test",
            Qualifications = "Test"
        };
        _context.Departments.Add(department);
        _context.Doctors.Add(doctor);
        await _context.SaveChangesAsync();
        _doctorId = doctor.Id;

        _controller = new AvailabilityController(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        _context.Dispose();
    }

    [Fact]
    public async Task SaveAvailability_PersistsWeeklyTemplate_AndUnlocksPublicSlotPicker()
    {
        var monday = new DayAvailability
        {
            DayOfWeek = (int)DayOfWeek.Monday,
            StartTime = "09:00",
            EndTime = "17:00",
            SlotDurationMinutes = 30,
            IsActive = true
        };

        var result = await _controller.SaveAvailability(new SaveAvailabilityRequest
        {
            DoctorId = _doctorId,
            Days = [monday]
        });

        var json = Assert.IsType<Microsoft.AspNetCore.Mvc.JsonResult>(result);
        var successProperty = json.Value!.GetType().GetProperty("success");
        Assert.True((bool)successProperty!.GetValue(json.Value)!);

        var saved = await _context.DoctorAvailabilities.SingleAsync(a => a.DoctorId == _doctorId);
        Assert.Equal(DayOfWeek.Monday, saved.DayOfWeek);
        Assert.Equal(new TimeSpan(9, 0, 0), saved.StartTime);
        Assert.Equal(new TimeSpan(17, 0, 0), saved.EndTime);
        Assert.True(saved.IsActive);

        // This is the actual patient-facing payoff: once a template exists, the public booking
        // widget's slot picker (AvailabilityService) has real slots to offer for that doctor.
        var availabilityService = new AvailabilityService(_context, NullLogger<AvailabilityService>.Instance);
        var nextMonday = NextWeekday(DayOfWeek.Monday);
        var slots = await availabilityService.GetAvailableSlotsAsync(_doctorId, nextMonday);

        Assert.NotEmpty(slots);
        Assert.Contains(slots, s => s.TimeOfDay == new TimeSpan(9, 0, 0));
    }

    [Fact]
    public async Task SaveAvailability_ReplacesExistingTemplateForDoctor()
    {
        _context.DoctorAvailabilities.Add(new DoctorAvailability
        {
            DoctorId = _doctorId,
            DayOfWeek = DayOfWeek.Tuesday,
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(12, 0, 0),
            SlotDurationMinutes = 20,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        await _controller.SaveAvailability(new SaveAvailabilityRequest
        {
            DoctorId = _doctorId,
            Days =
            [
                new DayAvailability
                {
                    DayOfWeek = (int)DayOfWeek.Wednesday,
                    StartTime = "10:00",
                    EndTime = "14:00",
                    SlotDurationMinutes = 15,
                    IsActive = true
                }
            ]
        });

        var remaining = await _context.DoctorAvailabilities.Where(a => a.DoctorId == _doctorId).ToListAsync();
        var day = Assert.Single(remaining);
        Assert.Equal(DayOfWeek.Wednesday, day.DayOfWeek);
    }

    [Fact]
    public async Task GenerateSlots_CreatesAppointmentSlotsFromActiveAvailability()
    {
        var nextMonday = NextWeekday(DayOfWeek.Monday);
        _context.DoctorAvailabilities.Add(new DoctorAvailability
        {
            DoctorId = _doctorId,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(10, 0, 0),
            SlotDurationMinutes = 30,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        var result = await _controller.GenerateSlots(new GenerateSlotsRequest
        {
            DoctorId = _doctorId,
            FromDate = nextMonday,
            ToDate = nextMonday
        });

        var json = Assert.IsType<Microsoft.AspNetCore.Mvc.JsonResult>(result);
        var successProperty = json.Value!.GetType().GetProperty("success");
        Assert.True((bool)successProperty!.GetValue(json.Value)!);

        var generatedSlots = await _context.AppointmentSlots
            .Where(s => s.DoctorId == _doctorId)
            .OrderBy(s => s.SlotDateTime)
            .ToListAsync();

        Assert.Equal(2, generatedSlots.Count);
        Assert.All(generatedSlots, s => Assert.False(s.IsBooked));
        Assert.Equal(nextMonday.Date.Add(new TimeSpan(9, 0, 0)), generatedSlots[0].SlotDateTime);
        Assert.Equal(nextMonday.Date.Add(new TimeSpan(9, 30, 0)), generatedSlots[1].SlotDateTime);
    }

    [Fact]
    public async Task GenerateSlots_IsIdempotent_DoesNotDuplicateExistingSlots()
    {
        var nextMonday = NextWeekday(DayOfWeek.Monday);
        _context.DoctorAvailabilities.Add(new DoctorAvailability
        {
            DoctorId = _doctorId,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(9, 30, 0),
            SlotDurationMinutes = 30,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        var request = new GenerateSlotsRequest { DoctorId = _doctorId, FromDate = nextMonday, ToDate = nextMonday };
        await _controller.GenerateSlots(request);
        await _controller.GenerateSlots(request);

        var count = await _context.AppointmentSlots.CountAsync(s => s.DoctorId == _doctorId);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GenerateSlots_WithoutActiveAvailability_ReturnsFailureMessage()
    {
        var result = await _controller.GenerateSlots(new GenerateSlotsRequest
        {
            DoctorId = _doctorId,
            FromDate = DateTime.Today,
            ToDate = DateTime.Today.AddDays(7)
        });

        var json = Assert.IsType<Microsoft.AspNetCore.Mvc.JsonResult>(result);
        var successProperty = json.Value!.GetType().GetProperty("success");
        Assert.False((bool)successProperty!.GetValue(json.Value)!);
    }

    private static DateTime NextWeekday(DayOfWeek targetDay)
    {
        var today = DateTime.Today;
        var daysAhead = (int)targetDay - (int)today.DayOfWeek;
        if (daysAhead <= 0)
            daysAhead += 7;
        return today.AddDays(daysAhead);
    }
}
