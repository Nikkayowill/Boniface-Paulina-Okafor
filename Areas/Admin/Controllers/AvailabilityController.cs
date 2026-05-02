using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;

namespace Okafor_.NET.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AvailabilityController : Controller
{
    private readonly ApplicationDbContext _context;

    public AvailabilityController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var doctors = await _context.Doctors
            .AsNoTracking()
            .OrderBy(d => d.FullName)
            .ToListAsync();

        ViewBag.Doctors = doctors;

        var recentLogs = await _context.NotificationLogs
            .AsNoTracking()
            .OrderByDescending(l => l.SentAt)
            .Take(10)
            .ToListAsync();

        ViewBag.RecentLogs = recentLogs;

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetDoctorAvailability(int doctorId)
    {
        var availability = await _context.DoctorAvailabilities
            .AsNoTracking()
            .Where(a => a.DoctorId == doctorId)
            .OrderBy(a => a.DayOfWeek)
            .Select(a => new
            {
                a.Id,
                a.DayOfWeek,
                startTime = a.StartTime.ToString(@"hh\:mm"),
                endTime = a.EndTime.ToString(@"hh\:mm"),
                a.SlotDurationMinutes,
                a.IsActive
            })
            .ToListAsync();

        return Json(availability);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAvailability([FromBody] SaveAvailabilityRequest request)
    {
        if (request is null || request.DoctorId <= 0)
            return Json(new { success = false, message = "Invalid doctor." });

        // Remove existing availability for this doctor and replace
        var existing = await _context.DoctorAvailabilities
            .Where(a => a.DoctorId == request.DoctorId)
            .ToListAsync();

        _context.DoctorAvailabilities.RemoveRange(existing);

        foreach (var day in request.Days)
        {
            if (!TimeSpan.TryParse(day.StartTime, out var start) ||
                !TimeSpan.TryParse(day.EndTime, out var end))
                continue;

            _context.DoctorAvailabilities.Add(new DoctorAvailability
            {
                DoctorId = request.DoctorId,
                DayOfWeek = (DayOfWeek)day.DayOfWeek,
                StartTime = start,
                EndTime = end,
                SlotDurationMinutes = day.SlotDurationMinutes > 0 ? day.SlotDurationMinutes : 30,
                IsActive = day.IsActive
            });
        }

        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "Availability saved." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateSlots([FromBody] GenerateSlotsRequest request)
    {
        if (request is null || request.DoctorId <= 0)
            return Json(new { success = false, message = "Invalid doctor." });

        if (request.FromDate >= request.ToDate)
            return Json(new { success = false, message = "From date must be before To date." });

        var availabilities = await _context.DoctorAvailabilities
            .AsNoTracking()
            .Where(a => a.DoctorId == request.DoctorId && a.IsActive)
            .ToListAsync();

        if (availabilities.Count == 0)
            return Json(new { success = false, message = "No active availability config found for this doctor." });

        var generated = 0;
        var current = request.FromDate.Date;

        while (current <= request.ToDate.Date)
        {
            var avail = availabilities.FirstOrDefault(a => a.DayOfWeek == current.DayOfWeek);
            if (avail is not null)
            {
                var slotTime = current.Add(avail.StartTime);
                var endTime = current.Add(avail.EndTime);

                while (slotTime.AddMinutes(avail.SlotDurationMinutes) <= endTime)
                {
                    var exists = await _context.AppointmentSlots
                        .AnyAsync(s => s.DoctorId == request.DoctorId && s.SlotDateTime == slotTime);

                    if (!exists)
                    {
                        _context.AppointmentSlots.Add(new AppointmentSlot
                        {
                            DoctorId = request.DoctorId,
                            SlotDateTime = slotTime,
                            IsBooked = false
                        });
                        generated++;
                    }

                    slotTime = slotTime.AddMinutes(avail.SlotDurationMinutes);
                }
            }

            current = current.AddDays(1);
        }

        await _context.SaveChangesAsync();
        return Json(new { success = true, message = $"{generated} slots generated." });
    }
}

public class SaveAvailabilityRequest
{
    public int DoctorId { get; set; }
    public List<DayAvailability> Days { get; set; } = [];
}

public class DayAvailability
{
    public int DayOfWeek { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int SlotDurationMinutes { get; set; } = 30;
    public bool IsActive { get; set; }
}

public class GenerateSlotsRequest
{
    public int DoctorId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
}
