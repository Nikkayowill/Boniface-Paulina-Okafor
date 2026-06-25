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
public class AppointmentRequestsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<BookingHub> _bookingHub;
    private readonly INotificationService _notifications;
    private readonly ILogger<AppointmentRequestsController> _logger;

    public AppointmentRequestsController(
        ApplicationDbContext context,
        IHubContext<BookingHub> bookingHub,
        INotificationService notifications,
        ILogger<AppointmentRequestsController> logger)
    {
        _context = context;
        _bookingHub = bookingHub;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var appointmentRequests = await _context.AppointmentRequests
            .AsNoTracking()
            .Include(a => a.Department)
            .Include(a => a.Doctor)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return View(appointmentRequests);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var appointmentRequest = await _context.AppointmentRequests
            .AsNoTracking()
            .Include(a => a.Department)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointmentRequest is null)
        {
            return NotFound();
        }

        return View(appointmentRequest);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var appointmentRequest = await _context.AppointmentRequests
            .AsNoTracking()
            .Include(a => a.Department)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointmentRequest is null)
        {
            return NotFound();
        }

        var model = new AdminAppointmentRequestUpdateViewModel
        {
            Id = appointmentRequest.Id,
            Status = appointmentRequest.Status,
            ContactConfirmed = appointmentRequest.ContactConfirmed,
            ContactMethod = appointmentRequest.ContactMethod,
            ContactNotes = appointmentRequest.ContactNotes,
            DoctorId = appointmentRequest.DoctorId,
            PatientName = appointmentRequest.PatientName,
            Email = appointmentRequest.Email,
            Phone = appointmentRequest.Phone,
            DepartmentName = appointmentRequest.Department?.Name ?? "N/A",
            PreferredDate = appointmentRequest.PreferredDate,
            PreferredTime = appointmentRequest.PreferredTime,
            Message = appointmentRequest.Message,
            CreatedAt = appointmentRequest.CreatedAt
        };

        await PopulateDoctorDropdownAsync(model.DoctorId);
        PopulateStatusDropdown(model.Status);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AdminAppointmentRequestUpdateViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        var appointmentRequest = await _context.AppointmentRequests
            .Include(a => a.Department)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointmentRequest is null)
        {
            return NotFound();
        }

        if (model.DoctorId.HasValue)
        {
            var doctorExists = await _context.Doctors.AnyAsync(d => d.Id == model.DoctorId.Value);
            if (!doctorExists)
            {
                ModelState.AddModelError(nameof(model.DoctorId), "Selected doctor is invalid.");
            }
        }

        if (!ModelState.IsValid)
        {
            model.PatientName = appointmentRequest.PatientName;
            model.Email = appointmentRequest.Email;
            model.Phone = appointmentRequest.Phone;
            model.DepartmentName = appointmentRequest.Department?.Name ?? "N/A";
            model.PreferredDate = appointmentRequest.PreferredDate;
            model.PreferredTime = appointmentRequest.PreferredTime;
            model.Message = appointmentRequest.Message;
            model.CreatedAt = appointmentRequest.CreatedAt;

            await PopulateDoctorDropdownAsync(model.DoctorId);
            PopulateStatusDropdown(model.Status);
            return View(model);
        }

        if (model.Status == AppointmentStatus.Approved)
        {
            if (!model.ContactConfirmed)
            {
                ModelState.AddModelError(nameof(model.ContactConfirmed), "Please confirm call/email verification before approving.");
            }

            if (string.IsNullOrWhiteSpace(model.ContactMethod) ||
                (model.ContactMethod != "Call" && model.ContactMethod != "Email"))
            {
                ModelState.AddModelError(nameof(model.ContactMethod), "Choose a valid confirmation method (Call or Email). ");
            }

            var (slotOk, slotError) = await TryReserveApprovedSlotAsync(appointmentRequest, model.DoctorId);
            if (!slotOk)
            {
                ModelState.AddModelError(nameof(model.Status), slotError ?? "This appointment conflicts with another booking.");
            }
        }

        if (!ModelState.IsValid)
        {
            model.PatientName = appointmentRequest.PatientName;
            model.Email = appointmentRequest.Email;
            model.Phone = appointmentRequest.Phone;
            model.DepartmentName = appointmentRequest.Department?.Name ?? "N/A";
            model.PreferredDate = appointmentRequest.PreferredDate;
            model.PreferredTime = appointmentRequest.PreferredTime;
            model.Message = appointmentRequest.Message;
            model.CreatedAt = appointmentRequest.CreatedAt;

            await PopulateDoctorDropdownAsync(model.DoctorId);
            PopulateStatusDropdown(model.Status);
            return View(model);
        }

        var oldStatus = appointmentRequest.Status;
        appointmentRequest.Status = model.Status;
        appointmentRequest.DoctorId = model.DoctorId;
        appointmentRequest.ContactConfirmed = model.ContactConfirmed;
        appointmentRequest.ContactMethod = model.ContactMethod;
        appointmentRequest.ContactNotes = model.ContactNotes;

        if (model.ContactConfirmed)
        {
            appointmentRequest.ContactConfirmedAt ??= DateTime.UtcNow;
        }

        if (model.Status == AppointmentStatus.Approved)
        {
            appointmentRequest.ApprovedAt = DateTime.UtcNow;
            appointmentRequest.ApprovedByUserId = User?.Identity?.Name;

            await EnsurePatientAppointmentFromApprovedRequestAsync(appointmentRequest);
        }

        await _context.SaveChangesAsync();

        if (oldStatus != appointmentRequest.Status)
        {
            await SendStatusNotificationSafelyAsync(appointmentRequest);
        }

        if (!string.IsNullOrWhiteSpace(appointmentRequest.Email))
        {
            await PublishPatientStatusChangeSafelyAsync(appointmentRequest);
        }

        await PublishAdminActionSafelyAsync(appointmentRequest);

        TempData["Success"] = model.Status switch
        {
            AppointmentStatus.Approved => $"Appointment for {appointmentRequest.PatientName} approved.",
            AppointmentStatus.Rejected => $"Appointment for {appointmentRequest.PatientName} rejected.",
            _ => "Appointment updated."
        };

        return RedirectToAction(nameof(Index));
    }

    private async Task SendStatusNotificationSafelyAsync(AppointmentRequest request)
    {
        try
        {
            await SendStatusNotificationAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Appointment request {AppointmentRequestId} was updated, but status notification delivery failed.", request.Id);
        }
    }

    private async Task PublishPatientStatusChangeSafelyAsync(AppointmentRequest request)
    {
        try
        {
            await _bookingHub.Clients.Group(BookingHubGroups.Patient(request.Email)).SendAsync("bookingStatusChanged", new
            {
                type = "appointment",
                id = request.Id,
                status = request.Status.ToString(),
                message = request.Status switch
                {
                    AppointmentStatus.Approved => "Your appointment has been approved.",
                    AppointmentStatus.Rejected => "Your appointment request was not approved. Please contact the hospital for next steps.",
                    _ => "Your appointment request was updated."
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Appointment request {AppointmentRequestId} was updated, but patient realtime status update failed.", request.Id);
        }
    }

    private async Task PublishAdminActionSafelyAsync(AppointmentRequest request)
    {
        try
        {
            await _bookingHub.Clients.Group(BookingHubGroups.AdminQueue).SendAsync("bookingActioned", new
            {
                type = "appointment",
                id = request.Id,
                status = request.Status.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Appointment request {AppointmentRequestId} was updated, but admin realtime action update failed.", request.Id);
        }
    }

    private async Task SendStatusNotificationAsync(AppointmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.Phone))
        {
            return;
        }

        var doctorName = request.DoctorId.HasValue
            ? await _context.Doctors
                .AsNoTracking()
                .Where(d => d.Id == request.DoctorId.Value)
                .Select(d => d.FullName)
                .FirstOrDefaultAsync()
            : null;

        var statusLabel = request.Status switch
        {
            AppointmentStatus.Approved => "Approved",
            AppointmentStatus.Rejected => "Not Approved",
            _ => "Updated"
        };

        var notification = new NotificationRequest
        {
            PatientName = request.PatientName,
            PatientEmail = request.Email ?? string.Empty,
            PatientPhone = request.Phone ?? string.Empty,
            DoctorName = doctorName ?? "To be assigned",
            Department = request.Department?.Name ?? "General",
            AppointmentDateTime = CombineDateAndTime(request.PreferredDate, request.PreferredTime),
            ConfirmationRef = request.Id.ToString("D8"),
            AppointmentRequestId = request.Id
        };

        await _notifications.SendAppointmentStatusAsync(notification, statusLabel, BuildNextStep(request));
    }

    private static string BuildNextStep(AppointmentRequest request)
    {
        return request.Status switch
        {
            AppointmentStatus.Approved => "Please arrive 15 minutes early with ID, medications, and relevant medical documents.",
            AppointmentStatus.Rejected => "Please contact the hospital if you still need care or want to request another appointment.",
            _ => "Hospital staff will contact you with the latest appointment details."
        };
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var appointmentRequest = await _context.AppointmentRequests
            .AsNoTracking()
            .Include(a => a.Department)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointmentRequest is null)
        {
            return NotFound();
        }

        return View(appointmentRequest);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var appointmentRequest = await _context.AppointmentRequests.FindAsync(id);
        if (appointmentRequest is null)
        {
            return NotFound();
        }

        _context.AppointmentRequests.Remove(appointmentRequest);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDoctorDropdownAsync(int? selectedDoctorId = null)
    {
        var doctors = await _context.Doctors
            .AsNoTracking()
            .Include(d => d.Department)
            .OrderBy(d => d.FullName)
            .Select(d => new
            {
                d.Id,
                Name = d.Department != null ? $"{d.FullName} ({d.Department.Name})" : d.FullName
            })
            .ToListAsync();

        ViewData["DoctorId"] = new SelectList(doctors, "Id", "Name", selectedDoctorId);
    }

    private void PopulateStatusDropdown(AppointmentStatus selectedStatus)
    {
        var statuses = Enum.GetValues<AppointmentStatus>()
            .Select(status => new { Id = status, Name = status.ToString() })
            .ToList();

        ViewData["Status"] = new SelectList(statuses, "Id", "Name", selectedStatus);
    }

    private async Task EnsurePatientAppointmentFromApprovedRequestAsync(AppointmentRequest request)
    {
        var linkedAppointmentExists = await _context.PatientAppointments
            .AnyAsync(a => a.AppointmentRequestId == request.Id);

        if (linkedAppointmentExists)
            return;

        var profile = await _context.PatientProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ApplicationUser != null && p.ApplicationUser.Email == request.Email);

        if (profile is null)
            return;

        _context.PatientAppointments.Add(new PatientAppointment
        {
            AppointmentRequestId = request.Id,
            PatientProfileId = profile.Id,
            DepartmentId = request.DepartmentId,
            DoctorId = request.DoctorId,
            AppointmentDate = CombineDateAndTime(request.PreferredDate, request.PreferredTime),
            Status = PatientAppointmentStatus.Confirmed,
            Notes = string.IsNullOrWhiteSpace(request.ContactNotes)
                ? "Approved from patient booking request after contact confirmation."
                : $"Approved after {request.ContactMethod?.ToLowerInvariant()} confirmation. {request.ContactNotes}"
        });
    }

    private async Task<(bool Success, string? Error)> TryReserveApprovedSlotAsync(AppointmentRequest request, int? doctorId)
    {
        if (!doctorId.HasValue)
        {
            return (false, "Assign a doctor before approving.");
        }

        var slotDateTime = CombineDateAndTime(request.PreferredDate, request.PreferredTime);
        if (slotDateTime <= DateTime.Now)
        {
            return (false, "This appointment time is in the past. Reschedule before approving.");
        }

        var conflictingSlot = await _context.AppointmentSlots
            .FirstOrDefaultAsync(s =>
                s.DoctorId == doctorId.Value &&
                s.SlotDateTime == slotDateTime &&
                s.AppointmentRequestId != request.Id);

        if (conflictingSlot?.IsBooked == true)
        {
            return (false, "That doctor already has a booked appointment at this time.");
        }

        var conflictingPortalAppointment = await _context.PatientAppointments
            .AsNoTracking()
            .AnyAsync(a =>
                a.DoctorId == doctorId.Value &&
                a.AppointmentDate == slotDateTime &&
                a.AppointmentRequestId != request.Id &&
                a.Status != PatientAppointmentStatus.Cancelled);

        if (conflictingPortalAppointment)
        {
            return (false, "That doctor already has a confirmed portal appointment at this time.");
        }

        var existingSlot = await _context.AppointmentSlots
            .FirstOrDefaultAsync(s =>
                s.DoctorId == doctorId.Value &&
                s.SlotDateTime == slotDateTime &&
                s.AppointmentRequestId == request.Id);

        if (existingSlot is not null)
        {
            existingSlot.IsBooked = true;
            return (true, null);
        }

        _context.AppointmentSlots.Add(new AppointmentSlot
        {
            DoctorId = doctorId.Value,
            SlotDateTime = slotDateTime,
            IsBooked = true,
            AppointmentRequestId = request.Id
        });

        return (true, null);
    }

    private static DateTime CombineDateAndTime(DateTime date, string? time)
    {
        if (!string.IsNullOrWhiteSpace(time) && DateTime.TryParse(time, out var parsedTime))
        {
            return date.Date.Add(parsedTime.TimeOfDay);
        }

        return date;
    }
}
