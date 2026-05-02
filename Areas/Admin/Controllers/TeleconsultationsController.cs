using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Hubs;
using Okafor_.NET.Models;
using Okafor_.NET.Services;
using Okafor_.NET.ViewModels;

namespace Okafor_.NET.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Staff")]
public class TeleconsultationsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notifications;
    private readonly IHubContext<BookingHub> _bookingHub;

    public TeleconsultationsController(
        ApplicationDbContext context,
        INotificationService notifications,
        IHubContext<BookingHub> bookingHub)
    {
        _context = context;
        _notifications = notifications;
        _bookingHub = bookingHub;
    }

    public async Task<IActionResult> Index(TeleconsultationStatus? status = null)
    {
        var query = _context.TeleconsultationRequests
            .AsNoTracking()
            .Include(t => t.Department)
            .Include(t => t.Doctor)
            .OrderByDescending(t => t.CreatedAt)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        ViewData["Status"] = new SelectList(Enum.GetValues<TeleconsultationStatus>());
        return View(await query.ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var request = await _context.TeleconsultationRequests
            .AsNoTracking()
            .Include(t => t.Department)
            .Include(t => t.Doctor)
            .Include(t => t.ApplicationUser)
            .Include(t => t.PatientProfile)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (request is null)
        {
            return NotFound();
        }

        return View(request);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var request = await _context.TeleconsultationRequests
            .AsNoTracking()
            .Include(t => t.Department)
            .Include(t => t.Doctor)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (request is null)
        {
            return NotFound();
        }

        var model = new AdminTeleconsultationUpdateViewModel
        {
            Id = request.Id,
            PatientName = request.PatientName,
            Email = request.Email,
            Phone = request.Phone,
            DepartmentName = request.Department?.Name ?? "N/A",
            DoctorName = request.Doctor?.FullName,
            ConsultationType = request.ConsultationType,
            Status = request.Status,
            PreferredDate = request.PreferredDate,
            PreferredTime = request.PreferredTime,
            MeetingLink = request.MeetingLink,
            AdminNotes = request.AdminNotes,
            Reason = request.Reason,
            CreatedAt = request.CreatedAt
        };

        PopulateStatusDropdown(model.Status);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AdminTeleconsultationUpdateViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        var request = await _context.TeleconsultationRequests
            .Include(t => t.Department)
            .Include(t => t.Doctor)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (request is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.PatientName = request.PatientName;
            model.Email = request.Email;
            model.Phone = request.Phone;
            model.DepartmentName = request.Department?.Name ?? "N/A";
            model.DoctorName = request.Doctor?.FullName;
            model.ConsultationType = request.ConsultationType;
            model.Reason = request.Reason;
            model.CreatedAt = request.CreatedAt;
            PopulateStatusDropdown(model.Status);
            return View(model);
        }

        var oldStatus = request.Status;
        request.Status = model.Status;
        request.PreferredDate = model.PreferredDate.Date;
        request.PreferredTime = model.PreferredTime;
        request.MeetingLink = model.MeetingLink;
        request.AdminNotes = model.AdminNotes;
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Send status notifications
        await SendStatusNotificationAsync(request, oldStatus);

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            await _bookingHub.Clients.Group(BookingHubGroups.Patient(request.Email)).SendAsync("bookingStatusChanged", new
            {
                type = "teleconsultation",
                id = request.Id,
                status = request.Status.ToString(),
                meetingLink = request.MeetingLink,
                message = request.Status switch
                {
                    TeleconsultationStatus.Confirmed => "Your teleconsultation has been confirmed.",
                    TeleconsultationStatus.Rescheduled => "Your teleconsultation has been rescheduled.",
                    TeleconsultationStatus.Rejected => "Your teleconsultation request was not approved.",
                    TeleconsultationStatus.Completed => "Your teleconsultation has been marked completed.",
                    _ => "Your teleconsultation request was updated."
                }
            });
        }

        await _bookingHub.Clients.Group(BookingHubGroups.AdminQueue).SendAsync("bookingActioned", new
        {
            type = "teleconsultation",
            id = request.Id,
            status = request.Status.ToString()
        });

        TempData["Success"] = "Teleconsultation request updated.";
        return RedirectToAction(nameof(Index));
    }

    private async Task SendStatusNotificationAsync(TeleconsultationRequest request, TeleconsultationStatus oldStatus)
    {
        // Only send if status changed
        if (request.Status == oldStatus)
            return;

        var doctorName = request.Doctor?.FullName ?? request.AdminNotes ?? "To be assigned";
        var appointmentDateTime = CombineDateAndTime(request.PreferredDate, request.PreferredTime);

        var notifRequest = new NotificationRequest
        {
            PatientName = request.PatientName,
            PatientEmail = request.Email,
            PatientPhone = request.Phone,
            DoctorName = doctorName,
            Department = request.Department?.Name ?? "—",
            AppointmentDateTime = appointmentDateTime,
            ConfirmationRef = $"TC-{request.Id:D6}",
            AppointmentRequestId = null
        };

        if (request.Status == TeleconsultationStatus.Confirmed)
        {
            // Send confirmation with meeting link and time
            await _notifications.SendConfirmationAsync(notifRequest);
        }
        else if (request.Status == TeleconsultationStatus.Rejected)
        {
            // Notify rejection (implementation depends on INotificationService interface)
            // For now, we just update the DB and admin can see it in the list
        }
        else if (request.Status == TeleconsultationStatus.Rescheduled)
        {
            // Send rescheduled notification with new time
            await _notifications.SendConfirmationAsync(notifRequest);
        }
    }

    private static DateTime CombineDateAndTime(DateTime date, string time)
    {
        if (string.IsNullOrWhiteSpace(time) || !time.Contains(':'))
            return date;

        var parts = time.Split(':');
        if (parts.Length == 2 && int.TryParse(parts[0], out var hours) && int.TryParse(parts[1], out var minutes))
        {
            return date.AddHours(hours).AddMinutes(minutes);
        }

        return date;
    }

    private void PopulateStatusDropdown(TeleconsultationStatus selected)
    {
        ViewData["Status"] = new SelectList(Enum.GetValues<TeleconsultationStatus>(), selected);
    }
}
