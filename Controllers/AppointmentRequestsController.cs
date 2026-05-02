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

namespace Okafor_.NET.Controllers;

public class AppointmentRequestsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAvailabilityService _availability;
    private readonly INotificationService _notifications;
    private readonly IHubContext<BookingHub> _bookingHub;

    public AppointmentRequestsController(
        ApplicationDbContext context,
        IAvailabilityService availability,
        INotificationService notifications,
        IHubContext<BookingHub> bookingHub)
    {
        _context = context;
        _availability = availability;
        _notifications = notifications;
        _bookingHub = bookingHub;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Create(int? departmentId = null, int? doctorId = null)
    {
        await PopulateLookupDataAsync(departmentId, doctorId);
        return View(new AppointmentRequest
        {
            PreferredDate = DateTime.Today,
            DepartmentId = departmentId ?? 0,
            DoctorId = doctorId
        });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("PatientName,Email,Phone,DepartmentId,DoctorId,PreferredDate,PreferredTime,Message")] AppointmentRequest appointmentRequest)
    {
        appointmentRequest.Email = appointmentRequest.Email?.Trim() ?? string.Empty;

        if (appointmentRequest.PreferredDate.Date < DateTime.Today)
        {
            ModelState.AddModelError(nameof(AppointmentRequest.PreferredDate), "Preferred date cannot be in the past.");
        }

        if (!appointmentRequest.DoctorId.HasValue)
        {
            ModelState.AddModelError(nameof(AppointmentRequest.DoctorId), "Please choose a doctor.");
        }
        else
        {
            var doctorExists = await _context.Doctors.AnyAsync(d => d.Id == appointmentRequest.DoctorId.Value && d.DepartmentId == appointmentRequest.DepartmentId);
            if (!doctorExists)
            {
                ModelState.AddModelError(nameof(AppointmentRequest.DoctorId), "Selected doctor is invalid for the chosen department.");
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateLookupDataAsync(appointmentRequest.DepartmentId, appointmentRequest.DoctorId);
            return View(appointmentRequest);
        }

        appointmentRequest.Status = AppointmentStatus.Pending;
        appointmentRequest.CreatedAt = DateTime.UtcNow;

        _context.Add(appointmentRequest);
        await _context.SaveChangesAsync();

        var dept = await _context.Departments.FindAsync(appointmentRequest.DepartmentId);

        TempData["Appt_Id"]   = appointmentRequest.Id.ToString();
        TempData["Appt_Name"] = appointmentRequest.PatientName;
        TempData["Appt_Email"]= appointmentRequest.Email;
        TempData["Appt_Date"] = appointmentRequest.PreferredDate.ToString("MMMM d, yyyy");
        TempData["Appt_Time"] = appointmentRequest.PreferredTime;
        TempData["Appt_Dept"] = dept?.Name ?? string.Empty;

        await _bookingHub.Clients.Group(BookingHubGroups.AdminQueue).SendAsync("appointmentSubmitted", new
        {
            id = appointmentRequest.Id,
            patientName = appointmentRequest.PatientName,
            department = dept?.Name ?? "Unassigned",
            preferredDate = appointmentRequest.PreferredDate.ToString("MMM d, yyyy"),
            preferredTime = appointmentRequest.PreferredTime,
            status = appointmentRequest.Status.ToString()
        });

        return RedirectToAction(nameof(Submitted));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Submitted()
    {
        return View();
    }

    // ──────────────────────────────────────────────────────────
    // Scheduling API endpoints
    // ──────────────────────────────────────────────────────────

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAvailableSlots(int doctorId, string date)
    {
        try
        {
            if (!DateTime.TryParse(date, out var parsedDate))
                return Json(new { slots = Array.Empty<string>() });

            var slots = await _availability.GetAvailableSlotsAsync(doctorId, parsedDate);
            var slotStrings = slots.Select(s => s.ToString("HH:mm")).ToList();
            return Json(new { slots = slotStrings });
        }
        catch
        {
            return Json(new { slots = Array.Empty<string>() });
        }
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> BookSlot([FromBody] BookSlotViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Invalid request.";
            return Json(new { success = false, message = errors });
        }

        try
        {
            if (!DateTime.TryParse($"{model.SlotDate} {model.SlotTime}", out var slotDateTime))
                return Json(new { success = false, message = "Invalid date or time." });

            if (slotDateTime < DateTime.Now)
                return Json(new { success = false, message = "This slot is in the past." });

            var doctor = await _context.Doctors
                .AsNoTracking()
                .Include(d => d.Department)
                .FirstOrDefaultAsync(d => d.Id == model.DoctorId);

            if (doctor is null)
                return Json(new { success = false, message = "Doctor not found." });

            var availableSlots = await _availability.GetAvailableSlotsAsync(model.DoctorId, slotDateTime.Date);
            if (!availableSlots.Contains(slotDateTime))
                return Json(new { success = false, message = "This slot is no longer available. Please choose another time." });

            // Create the appointment request
            var appointmentRequest = new AppointmentRequest
            {
                PatientName = model.PatientName,
                Email = model.PatientEmail,
                Phone = model.PatientPhone,
                DepartmentId = doctor.DepartmentId,
                DoctorId = model.DoctorId,
                PreferredDate = slotDateTime.Date,
                PreferredTime = slotDateTime.ToString("HH:mm"),
                Message = model.ReasonForVisit,
                Status = AppointmentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.AppointmentRequests.Add(appointmentRequest);
            await _context.SaveChangesAsync();

            var confirmationRef = appointmentRequest.Id.ToString("D8");

            // Reserve the slot
            var (reserved, reserveError) = await _availability.ReserveSlotAsync(
                model.DoctorId, slotDateTime, appointmentRequest.Id);

            if (!reserved)
            {
                // Remove the request we just created since slot is gone
                _context.AppointmentRequests.Remove(appointmentRequest);
                await _context.SaveChangesAsync();
                return Json(new { success = false, message = reserveError ?? "Slot no longer available." });
            }

            // Build notification request
            var notifRequest = new NotificationRequest
            {
                PatientName = model.PatientName,
                PatientEmail = model.PatientEmail,
                PatientPhone = model.PatientPhone,
                DoctorName = doctor.FullName,
                Department = doctor.Department?.Name ?? string.Empty,
                AppointmentDateTime = slotDateTime,
                ConfirmationRef = confirmationRef,
                AppointmentRequestId = appointmentRequest.Id
            };

            // Fire notifications (don't await in parallel — log failures, never crash)
            await _notifications.SendConfirmationAsync(notifRequest);
            await _notifications.SendAdminAlertAsync(notifRequest);

            await _bookingHub.Clients.Group(BookingHubGroups.AdminQueue).SendAsync("appointmentSubmitted", new
            {
                id = appointmentRequest.Id,
                patientName = appointmentRequest.PatientName,
                department = doctor.Department?.Name ?? string.Empty,
                doctor = doctor.FullName,
                preferredDate = slotDateTime.ToString("MMM d, yyyy"),
                preferredTime = slotDateTime.ToString("h:mm tt"),
                status = appointmentRequest.Status.ToString()
            });

            await _bookingHub.Clients
                .Group(BookingHubGroups.DoctorDay(model.DoctorId, model.SlotDate))
                .SendAsync("slotBooked", new
                {
                    doctorId = model.DoctorId,
                    date = model.SlotDate,
                    slot = model.SlotTime,
                    message = "This time was just booked by another patient."
                });

            // Build WhatsApp click-to-chat URL (always, regardless of provider)
            var waNumber = HttpContext.RequestServices
                .GetService<IConfiguration>()
                ?["Notifications:WhatsAppNumber"]?.Replace("+", "") ?? "2348012345678";
            var waMessage = Uri.EscapeDataString(
                $"Hello, I have booked an appointment at BP Okafor Memorial Hospital.\n" +
                $"Name: {model.PatientName}\nDate: {slotDateTime:MMMM d, yyyy}\n" +
                $"Time: {slotDateTime:h:mm tt}\nDoctor: {doctor.FullName}\nRef: {confirmationRef}");
            var whatsAppUrl = $"https://wa.me/{waNumber}?text={waMessage}";

            return Json(new
            {
                success = true,
                confirmationRef,
                whatsAppUrl,
                doctorName = doctor.FullName,
                department = doctor.Department?.Name,
                appointmentDate = slotDateTime.ToString("dddd, MMMM d, yyyy"),
                appointmentTime = slotDateTime.ToString("h:mm tt"),
                message = "Your appointment has been confirmed."
            });
        }
        catch (Exception)
        {
            return Json(new { success = false, message = "An unexpected error occurred. Please try again." });
        }
    }

    private static string GenerateRef()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        return new string(Enumerable.Range(0, 8).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }

    private async Task PopulateLookupDataAsync(int? selectedDepartmentId = null, int? selectedDoctorId = null)
    {
        var departments = await _context.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .ToListAsync();

        var doctorOptions = await _context.Doctors
            .AsNoTracking()
            .Include(d => d.Department)
            .OrderBy(d => d.FullName)
            .Select(d => new
            {
                d.Id,
                d.DepartmentId,
                Name = d.FullName
            })
            .ToListAsync();

        var doctors = selectedDepartmentId.HasValue
            ? doctorOptions.Where(d => d.DepartmentId == selectedDepartmentId.Value).ToList()
            : doctorOptions;

        ViewData["DepartmentId"] = new SelectList(departments, "Id", "Name", selectedDepartmentId);
        ViewData["DoctorId"] = new SelectList(doctors, "Id", "Name", selectedDoctorId);
        ViewBag.DoctorOptions = doctorOptions;
        ViewBag.Departments = departments;
    }
}
