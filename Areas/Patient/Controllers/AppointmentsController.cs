using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Hubs;
using Okafor_.NET.Models;
using Okafor_.NET.ViewModels;
using System.Text;

namespace Okafor_.NET.Areas.Patient.Controllers;

public class AppointmentsController : PatientBaseController
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHubContext<BookingHub> _bookingHub;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IHubContext<BookingHub> bookingHub,
        ILogger<AppointmentsController> logger)
    {
        _context = context;
        _userManager = userManager;
        _bookingHub = bookingHub;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;
        var profile = await _context.PatientProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ApplicationUserId == userId);

        if (profile is null)
            return RedirectToAction("Create", "Profile");

        // Admin-created portal appointments
        var portalAppointments = await _context.PatientAppointments
            .AsNoTracking()
            .Where(a => a.PatientProfileId == profile.Id)
            .Include(a => a.Department)
            .Include(a => a.Doctor)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync();

        // Public booking requests by this user's email
        var user = await _userManager.FindByIdAsync(userId);
        var bookingRequests = user?.Email is not null
            ? await _context.AppointmentRequests
                .AsNoTracking()
                .Where(r => r.Email == user.Email &&
                            !_context.PatientAppointments.Any(pa => pa.AppointmentRequestId == r.Id))
                .Include(r => r.Department)
                .Include(r => r.Doctor)
                .OrderByDescending(r => r.PreferredDate)
                .ToListAsync()
            : new();

        // Merge into unified view model, most recent first
        var merged = portalAppointments
            .Select(a => new PortalAppointmentViewModel
            {
                SourceId = a.Id,
                BookingStatusId = a.AppointmentRequestId ?? a.Id,
                Date       = a.AppointmentDate,
                Department = a.Department?.Name ?? "—",
                Doctor     = a.Doctor?.FullName,
                Status     = a.Status.ToString(),
                Notes      = a.Notes,
                Source     = "Scheduled Appointment",
                SourceType = "scheduled",
                Subject = $"Hospital Appointment - {a.Department?.Name ?? "General"}"
            })
            .Concat(bookingRequests.Select(r => new PortalAppointmentViewModel
            {
                SourceId = r.Id,
                BookingStatusId = r.Id,
                Date       = CombineDateAndTime(r.PreferredDate, r.PreferredTime),
                Department = r.Department?.Name ?? "—",
                Doctor     = r.Doctor?.FullName,
                Status     = r.Status.ToString(),
                Notes      = r.Message,
                Source     = "Booking Request",
                SourceType = "request",
                Subject = $"Hospital Appointment Request - {r.Department?.Name ?? "General"}"
            }))
            .OrderByDescending(a => a.Date)
            .ToList();

        return View(merged);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadCalendar(string sourceType, int sourceId)
    {
        var userId = _userManager.GetUserId(User)!;
        var user = await _userManager.FindByIdAsync(userId);
        if (user?.Email is null)
            return NotFound();

        DateTime start;
        string title;
        string description;

        if (sourceType == "scheduled")
        {
            var scheduled = await _context.PatientAppointments
                .AsNoTracking()
                .Include(a => a.Department)
                .Include(a => a.PatientProfile)
                .FirstOrDefaultAsync(a => a.Id == sourceId && a.PatientProfile != null && a.PatientProfile.ApplicationUserId == userId);

            if (scheduled is null)
                return NotFound();

            start = scheduled.AppointmentDate;
            title = $"Hospital Appointment - {scheduled.Department?.Name ?? "General"}";
            description = scheduled.Notes ?? "Confirmed hospital appointment.";
        }
        else
        {
            var request = await _context.AppointmentRequests
                .AsNoTracking()
                .Include(r => r.Department)
                .FirstOrDefaultAsync(r => r.Id == sourceId && r.Email == user.Email);

            if (request is null)
                return NotFound();

            start = CombineDateAndTime(request.PreferredDate, request.PreferredTime);
            title = $"Hospital Appointment Request - {request.Department?.Name ?? "General"}";
            description = request.Message ?? "Pending/approved hospital booking request.";
        }

        var end = start.AddMinutes(45);
        var now = DateTime.UtcNow;

        string ToUtcIcs(DateTime dt) => dt.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'");
        string Escape(string input) => input.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\n", "\\n").Replace("\r", string.Empty);

        var ics = new StringBuilder();
        ics.AppendLine("BEGIN:VCALENDAR");
        ics.AppendLine("VERSION:2.0");
        ics.AppendLine("PRODID:-//Okafor Hospital//Patient Portal//EN");
        ics.AppendLine("CALSCALE:GREGORIAN");
        ics.AppendLine("BEGIN:VEVENT");
        ics.AppendLine($"UID:{Guid.NewGuid():N}@okaforhospital.local");
        ics.AppendLine($"DTSTAMP:{ToUtcIcs(now)}");
        ics.AppendLine($"DTSTART:{ToUtcIcs(start)}");
        ics.AppendLine($"DTEND:{ToUtcIcs(end)}");
        ics.AppendLine($"SUMMARY:{Escape(title)}");
        ics.AppendLine($"DESCRIPTION:{Escape(description)}");
        ics.AppendLine("END:VEVENT");
        ics.AppendLine("END:VCALENDAR");

        var fileName = $"appointment-{start:yyyyMMdd-HHmm}.ics";
        return File(Encoding.UTF8.GetBytes(ics.ToString()), "text/calendar", fileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(string sourceType, int sourceId)
    {
        var userId = _userManager.GetUserId(User)!;
        var profile = await _context.PatientProfiles
            .FirstOrDefaultAsync(p => p.ApplicationUserId == userId);

        if (sourceType == "scheduled")
        {
            if (profile is null)
            {
                return RedirectToAction("Create", "Profile");
            }

            var appointment = await _context.PatientAppointments
                .FirstOrDefaultAsync(a => a.Id == sourceId && a.PatientProfileId == profile.Id);

            if (appointment is null)
                return NotFound();

            // Only allow cancellation if appointment is not yet completed
            if (appointment.Status is not PatientAppointmentStatus.Cancelled and not PatientAppointmentStatus.Completed &&
                appointment.AppointmentDate > DateTime.Now)
            {
                appointment.Status = PatientAppointmentStatus.Cancelled;
                appointment.Notes = string.IsNullOrWhiteSpace(appointment.Notes)
                    ? "Cancelled by patient."
                    : $"{appointment.Notes} [CANCELLED BY PATIENT]";

                if (appointment.AppointmentRequestId.HasValue)
                {
                    var linkedRequestId = appointment.AppointmentRequestId.Value;
                    var linkedRequest = await _context.AppointmentRequests
                        .FirstOrDefaultAsync(request => request.Id == linkedRequestId);

                    if (linkedRequest is not null)
                    {
                        linkedRequest.Status = AppointmentStatus.Cancelled;
                        linkedRequest.ContactNotes = AppendCancellationNote(linkedRequest.ContactNotes);
                    }

                    await ReleaseReservedSlotsAsync(linkedRequestId);
                }

                await _context.SaveChangesAsync();
                if (appointment.AppointmentRequestId.HasValue)
                {
                    await PublishCancellationSafelyAsync("appointment", appointment.AppointmentRequestId.Value);
                }
                TempData["Success"] = "Appointment cancelled.";
            }
            else
            {
                TempData["Error"] = "Cannot cancel this appointment.";
            }
        }
        else if (sourceType == "request")
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user?.Email is null)
            {
                return NotFound();
            }

            var request = await _context.AppointmentRequests
                .FirstOrDefaultAsync(r => r.Id == sourceId && r.Email == user.Email);

            if (request is null)
                return NotFound();

            // Only allow cancellation if status is still Pending
            if (request.Status == AppointmentStatus.Pending)
            {
                request.Status = AppointmentStatus.Cancelled;
                request.ContactNotes = AppendCancellationNote(request.ContactNotes);
                await ReleaseReservedSlotsAsync(request.Id);
                await _context.SaveChangesAsync();
                await PublishCancellationSafelyAsync("appointment", request.Id);
                TempData["Success"] = "Booking request cancelled.";
            }
            else
            {
                TempData["Error"] = "Only pending booking requests can be cancelled.";
            }
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task ReleaseReservedSlotsAsync(int appointmentRequestId)
    {
        var linkedSlots = await _context.AppointmentSlots
            .Where(slot => slot.AppointmentRequestId == appointmentRequestId)
            .ToListAsync();

        foreach (var slot in linkedSlots)
        {
            slot.IsBooked = false;
            slot.AppointmentRequestId = null;
            slot.ReminderSent = false;
        }
    }

    private async Task PublishCancellationSafelyAsync(string type, int id)
    {
        try
        {
            await _bookingHub.Clients.Group(BookingHubGroups.AdminQueue).SendAsync("bookingActioned", new
            {
                type,
                id,
                status = "Cancelled"
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Patient cancellation for {BookingType} {BookingId} succeeded, but the admin realtime update failed.", type, id);
        }
    }

    private static string AppendCancellationNote(string? notes) =>
        string.IsNullOrWhiteSpace(notes)
            ? "Cancelled by patient."
            : $"{notes} [CANCELLED BY PATIENT]";

    private static DateTime CombineDateAndTime(DateTime date, string? time)
    {
        if (!string.IsNullOrWhiteSpace(time) && DateTime.TryParse(time, out var parsedTime))
        {
            return date.Date.Add(parsedTime.TimeOfDay);
        }

        return date.Date;
    }
}
