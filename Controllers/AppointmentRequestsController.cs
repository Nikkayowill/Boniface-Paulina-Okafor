using System.Globalization;
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
    private const int MaxAdvanceBookingDays = 60;
    private const string TeleconsultationOnlyDepartment = "Spiritual Care and Psychotherapy";

    private readonly ApplicationDbContext _context;
    private readonly IAvailabilityService _availability;
    private readonly INotificationService _notifications;
    private readonly IHubContext<BookingHub> _bookingHub;
    private readonly ILogger<AppointmentRequestsController> _logger;

    public AppointmentRequestsController(
        ApplicationDbContext context,
        IAvailabilityService availability,
        INotificationService notifications,
        IHubContext<BookingHub> bookingHub,
        ILogger<AppointmentRequestsController> logger)
    {
        _context = context;
        _availability = availability;
        _notifications = notifications;
        _bookingHub = bookingHub;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Create(int? departmentId = null, int? doctorId = null)
    {
        if (doctorId.HasValue && !departmentId.HasValue)
        {
            departmentId = await _context.Doctors
                .AsNoTracking()
                .Where(d => d.Id == doctorId.Value)
                .Select(d => (int?)d.DepartmentId)
                .FirstOrDefaultAsync();
        }

        if (departmentId.HasValue && await _context.Departments
                .AsNoTracking()
                .AnyAsync(department =>
                    department.Id == departmentId.Value &&
                    department.Name == TeleconsultationOnlyDepartment))
        {
            return RedirectToAction(
                "Create",
                "Teleconsultations",
                new { departmentId, doctorId });
        }

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
        appointmentRequest.PatientName = appointmentRequest.PatientName?.Trim() ?? string.Empty;
        appointmentRequest.Email = appointmentRequest.Email?.Trim() ?? string.Empty;
        appointmentRequest.Phone = appointmentRequest.Phone?.Trim() ?? string.Empty;
        appointmentRequest.PreferredTime = appointmentRequest.PreferredTime?.Trim() ?? string.Empty;
        appointmentRequest.Message = string.IsNullOrWhiteSpace(appointmentRequest.Message)
            ? null
            : appointmentRequest.Message.Trim();

        ModelState.Clear();
        TryValidateModel(appointmentRequest);

        if (appointmentRequest.PreferredDate.Date < DateTime.Today)
        {
            ModelState.AddModelError(nameof(AppointmentRequest.PreferredDate), "Preferred date cannot be in the past.");
        }

        Doctor? doctor = null;
        DateTime slotDateTime = default;

        if (!appointmentRequest.DoctorId.HasValue)
        {
            ModelState.AddModelError(nameof(AppointmentRequest.DoctorId), "Please choose a doctor.");
        }
        else
        {
            doctor = await _context.Doctors
                .AsNoTracking()
                .Include(d => d.Department)
                .FirstOrDefaultAsync(d => d.Id == appointmentRequest.DoctorId.Value && d.DepartmentId == appointmentRequest.DepartmentId);

            if (doctor is null)
            {
                ModelState.AddModelError(nameof(AppointmentRequest.DoctorId), "Selected doctor is invalid for the chosen department.");
            }
        }

        if (!TryCombineDateAndTime(appointmentRequest.PreferredDate, appointmentRequest.PreferredTime, out slotDateTime))
        {
            ModelState.AddModelError(nameof(AppointmentRequest.PreferredTime), "Please choose a valid appointment time.");
        }
        else if (slotDateTime < DateTime.Now)
        {
            ModelState.AddModelError(nameof(AppointmentRequest.PreferredTime), "This slot is in the past.");
        }

        if (ModelState.IsValid && doctor is not null)
        {
            var availableSlots = await _availability.GetAvailableSlotsAsync(doctor.Id, slotDateTime.Date);
            if (!availableSlots.Contains(slotDateTime))
            {
                ModelState.AddModelError(nameof(AppointmentRequest.PreferredTime), "This slot is no longer available. Please choose another time.");
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateLookupDataAsync(appointmentRequest.DepartmentId, appointmentRequest.DoctorId);
            return View(appointmentRequest);
        }

        appointmentRequest.Status = AppointmentStatus.Pending;
        appointmentRequest.PreferredDate = slotDateTime.Date;
        appointmentRequest.PreferredTime = slotDateTime.ToString("HH:mm");
        appointmentRequest.CreatedAt = DateTime.UtcNow;

        _context.Add(appointmentRequest);
        await _context.SaveChangesAsync();

        var (reserved, reserveError) = await _availability.ReserveSlotAsync(
            doctor!.Id,
            slotDateTime,
            appointmentRequest.Id);

        if (!reserved)
        {
            _context.AppointmentRequests.Remove(appointmentRequest);
            await _context.SaveChangesAsync();

            ModelState.AddModelError(nameof(AppointmentRequest.PreferredTime), reserveError ?? "Slot no longer available.");
            await PopulateLookupDataAsync(appointmentRequest.DepartmentId, appointmentRequest.DoctorId);
            return View(appointmentRequest);
        }

        var dept = doctor.Department;

        TempData["Appt_Id"]   = appointmentRequest.Id.ToString();
        TempData["Appt_Name"] = appointmentRequest.PatientName;
        TempData["Appt_Email"]= appointmentRequest.Email;
        TempData["Appt_Date"] = appointmentRequest.PreferredDate.ToString("MMMM d, yyyy");
        TempData["Appt_Time"] = slotDateTime.ToString("h:mm tt");
        TempData["Appt_Dept"] = dept?.Name ?? string.Empty;

        await PublishBookingUpdatesAsync(appointmentRequest, doctor, slotDateTime);

        return RedirectToAction(nameof(Submitted));
    }

    [HttpGet]
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Submitted()
    {
        return View();
    }

    // ──────────────────────────────────────────────────────────
    // Scheduling API endpoints
    // ──────────────────────────────────────────────────────────

    [HttpGet]
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> GetAvailableSlots(int doctorId, string date)
    {
        try
        {
            if (!DateTime.TryParseExact(
                    date,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedDate) ||
                parsedDate.Date < DateTime.Today ||
                parsedDate.Date > DateTime.Today.AddDays(MaxAdvanceBookingDays))
            {
                return Json(new { slots = Array.Empty<string>() });
            }

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
    public async Task<IActionResult> BookSlot([FromBody] BookSlotViewModel? model)
    {
        if (model is null)
        {
            return Json(new { success = false, message = "Invalid request." });
        }

        model.SlotDate = model.SlotDate?.Trim() ?? string.Empty;
        model.SlotTime = model.SlotTime?.Trim() ?? string.Empty;
        model.PatientName = model.PatientName?.Trim() ?? string.Empty;
        model.PatientPhone = model.PatientPhone?.Trim() ?? string.Empty;
        model.PatientEmail = model.PatientEmail?.Trim() ?? string.Empty;
        model.ReasonForVisit = string.IsNullOrWhiteSpace(model.ReasonForVisit)
            ? null
            : model.ReasonForVisit.Trim();

        ModelState.Clear();
        TryValidateModel(model);

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
            if (!DateTime.TryParseExact(
                    $"{model.SlotDate} {model.SlotTime}",
                    "yyyy-MM-dd HH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var slotDateTime))
            {
                return Json(new { success = false, message = "Invalid date or time." });
            }

            if (slotDateTime < DateTime.Now)
                return Json(new { success = false, message = "This slot is in the past." });

            if (slotDateTime.Date > DateTime.Today.AddDays(MaxAdvanceBookingDays))
                return Json(new { success = false, message = "Appointments can only be booked up to 60 days in advance." });

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

            await SendBookingNotificationsAsync(notifRequest, appointmentRequest.Id);
            await PublishBookingUpdatesAsync(appointmentRequest, doctor, slotDateTime);

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Appointment booking failed for doctor {DoctorId} on {SlotDate} at {SlotTime}.", model.DoctorId, model.SlotDate, model.SlotTime);
            return Json(new { success = false, message = "An unexpected error occurred. Please try again." });
        }
    }

    private async Task SendBookingNotificationsAsync(NotificationRequest notification, int appointmentRequestId)
    {
        try
        {
            await _notifications.SendConfirmationAsync(notification);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Appointment {AppointmentRequestId} confirmation notification failed.", appointmentRequestId);
        }

        try
        {
            await _notifications.SendAdminAlertAsync(notification);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Appointment {AppointmentRequestId} admin notification failed.", appointmentRequestId);
        }
    }

    private async Task PublishBookingUpdatesAsync(AppointmentRequest appointmentRequest, Doctor doctor, DateTime slotDateTime)
    {
        try
        {
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
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Appointment {AppointmentRequestId} admin realtime booking update failed.", appointmentRequest.Id);
        }

        try
        {
            await _bookingHub.Clients
                .Group(BookingHubGroups.DoctorDay(doctor.Id, slotDateTime.ToString("yyyy-MM-dd")))
                .SendAsync("slotBooked", new
                {
                    doctorId = doctor.Id,
                    date = slotDateTime.ToString("yyyy-MM-dd"),
                    slot = slotDateTime.ToString("HH:mm"),
                    message = "This time was just booked by another patient."
                });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Appointment {AppointmentRequestId} doctor realtime booking update failed.", appointmentRequest.Id);
        }
    }

    private async Task PopulateLookupDataAsync(int? selectedDepartmentId = null, int? selectedDoctorId = null)
    {
        var departments = await _context.Departments
            .AsNoTracking()
            .Where(d => d.Name != TeleconsultationOnlyDepartment)
            .OrderBy(d => d.Name)
            .ToListAsync();

        var doctorOptions = await _context.Doctors
            .AsNoTracking()
            .Include(d => d.Department)
            .Where(d => d.Department != null && d.Department.Name != TeleconsultationOnlyDepartment)
            .OrderBy(d => d.FullName)
            .Select(d => new
            {
                d.Id,
                d.DepartmentId,
                Name = d.FullName,
                d.Specialty,
                Department = d.Department != null ? d.Department.Name : string.Empty,
                DisplayName = d.Department != null
                    ? d.FullName + " — " + d.Department.Name
                    : d.FullName
            })
            .ToListAsync();

        var doctors = selectedDepartmentId.HasValue && selectedDepartmentId.Value > 0
            ? doctorOptions.Where(d => d.DepartmentId == selectedDepartmentId.Value).ToList()
            : [];

        ViewData["DepartmentId"] = new SelectList(departments, "Id", "Name", selectedDepartmentId);
        ViewData["DoctorId"] = new SelectList(doctors, "Id", "DisplayName", selectedDoctorId);
        ViewBag.DoctorOptions = doctorOptions;
        ViewBag.BookingDoctors = doctorOptions.Select(d => new
        {
            id = d.Id,
            departmentId = d.DepartmentId,
            fullName = d.Name,
            specialty = d.Specialty,
            department = d.Department,
            displayName = d.DisplayName
        });
        ViewBag.HasDoctors = doctorOptions.Any();
        ViewBag.Departments = departments;
    }

    private static bool TryCombineDateAndTime(DateTime date, string? time, out DateTime dateTime)
    {
        var value = $"{date:yyyy-MM-dd} {time?.Trim()}";
        var formats = new[] { "yyyy-MM-dd HH:mm", "yyyy-MM-dd H:mm", "yyyy-MM-dd h:mm tt", "yyyy-MM-dd hh:mm tt" };

        return DateTime.TryParseExact(
            value,
            formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out dateTime);
    }
}
