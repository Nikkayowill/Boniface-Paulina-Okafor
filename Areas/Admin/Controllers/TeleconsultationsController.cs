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
    private readonly IWhatsAppNotificationService _whatsAppNotifications;
    private readonly IHubContext<BookingHub> _bookingHub;
    private readonly ITeleconsultationLifecycleService _lifecycle;
    private readonly ILogger<TeleconsultationsController> _logger;

    public TeleconsultationsController(
        ApplicationDbContext context,
        INotificationService notifications,
        IWhatsAppNotificationService whatsAppNotifications,
        IHubContext<BookingHub> bookingHub,
        ITeleconsultationLifecycleService lifecycle,
        ILogger<TeleconsultationsController> logger)
    {
        _context = context;
        _notifications = notifications;
        _whatsAppNotifications = whatsAppNotifications;
        _bookingHub = bookingHub;
        _lifecycle = lifecycle;
        _logger = logger;
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

        var notifications = await _context.NotificationLogs
            .AsNoTracking()
            .Where(n => n.TeleconsultationRequestId == id)
            .OrderByDescending(n => n.SentAt)
            .Select(n => new NotificationTimelineItemViewModel
            {
                Channel = n.Channel,
                Recipient = n.Recipient,
                Message = n.MessageBody,
                Success = n.Success,
                ErrorMessage = n.ErrorMessage,
                DeliveryStatus = n.DeliveryStatus,
                SentAt = n.SentAt,
                DeliveredAt = n.DeliveredAt,
                ReadAt = n.ReadAt
            })
            .ToListAsync();

        return View(new AdminTeleconsultationDetailsViewModel
        {
            Request = request,
            Notifications = notifications
        });
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

        var update = new TeleconsultationUpdateInput(
            model.Status,
            model.PreferredDate,
            model.PreferredTime,
            model.MeetingLink,
            model.AdminNotes);

        foreach (var error in _lifecycle.ValidateAdminUpdate(request, update))
        {
            ModelState.AddModelError(error.FieldName, error.Message);
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
        _lifecycle.ApplyAdminUpdate(request, update);

        await _context.SaveChangesAsync();

        await SendStatusNotificationSafelyAsync(request, oldStatus);

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            await PublishPatientStatusChangeSafelyAsync(request);
        }

        await PublishAdminActionSafelyAsync(request);

        TempData["Success"] = "Teleconsultation request updated.";
        return RedirectToAction(nameof(Index));
    }

    private async Task SendStatusNotificationSafelyAsync(TeleconsultationRequest request, TeleconsultationStatus oldStatus)
    {
        try
        {
            await SendStatusNotificationAsync(request, oldStatus);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Teleconsultation request {TeleconsultationRequestId} was updated, but status notification delivery failed.", request.Id);
        }
    }

    private async Task PublishPatientStatusChangeSafelyAsync(TeleconsultationRequest request)
    {
        try
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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Teleconsultation request {TeleconsultationRequestId} was updated, but patient realtime status update failed.", request.Id);
        }
    }

    private async Task PublishAdminActionSafelyAsync(TeleconsultationRequest request)
    {
        try
        {
            await _bookingHub.Clients.Group(BookingHubGroups.AdminQueue).SendAsync("bookingActioned", new
            {
                type = "teleconsultation",
                id = request.Id,
                status = request.Status.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Teleconsultation request {TeleconsultationRequestId} was updated, but admin realtime action update failed.", request.Id);
        }
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
            AppointmentRequestId = null,
            TeleconsultationRequestId = request.Id
        };

        var statusLabel = request.Status switch
        {
            TeleconsultationStatus.Confirmed => "Confirmed",
            TeleconsultationStatus.Rescheduled => "Rescheduled",
            TeleconsultationStatus.Completed => "Completed",
            TeleconsultationStatus.Rejected => "Not Approved",
            _ => "Updated"
        };

        var nextStep = BuildNextStep(request);
        try
        {
            await _notifications.SendTeleconsultationStatusAsync(notifRequest, statusLabel, nextStep);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Teleconsultation request {TeleconsultationRequestId} status email notification failed.", request.Id);
        }

        try
        {
            await _whatsAppNotifications.SendTeleconsultationStatusAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Teleconsultation request {TeleconsultationRequestId} status WhatsApp notification failed.", request.Id);
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

    private static string BuildNextStep(TeleconsultationRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.MeetingLink))
            return request.MeetingLink;

        if (!string.IsNullOrWhiteSpace(request.AdminNotes))
            return request.AdminNotes;

        return request.Status switch
        {
            TeleconsultationStatus.Confirmed => "Your meeting link will be shared by the care team.",
            TeleconsultationStatus.Rescheduled => "Please review the new date and time.",
            TeleconsultationStatus.Rejected => "Please contact the hospital for safer next steps.",
            TeleconsultationStatus.Completed => "Thank you for using BP Okafor virtual care.",
            _ => "Please wait for clinical review."
        };
    }
}
