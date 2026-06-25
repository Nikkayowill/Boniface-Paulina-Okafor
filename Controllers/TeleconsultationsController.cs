using Microsoft.AspNetCore.Identity;
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

public class TeleconsultationsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notifications;
    private readonly IWhatsAppNotificationService _whatsAppNotifications;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHubContext<BookingHub> _bookingHub;
    private readonly ILogger<TeleconsultationsController> _logger;

    public TeleconsultationsController(
        ApplicationDbContext context,
        INotificationService notifications,
        IWhatsAppNotificationService whatsAppNotifications,
        UserManager<ApplicationUser> userManager,
        IHubContext<BookingHub> bookingHub,
        ILogger<TeleconsultationsController> logger)
    {
        _context = context;
        _notifications = notifications;
        _whatsAppNotifications = whatsAppNotifications;
        _userManager = userManager;
        _bookingHub = bookingHub;
        _logger = logger;
    }

    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Create()
    {
        await PopulateLookupsAsync();
        return View(new TeleconsultationRequestViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TeleconsultationRequestViewModel model)
    {
        model.PatientName = model.PatientName.Trim();
        model.Email = model.Email.Trim();
        model.Phone = model.Phone.Trim();
        model.PhoneCountryCode = model.PhoneCountryCode.Trim();
        model.PreferredTime = model.PreferredTime.Trim();
        model.Reason = model.Reason.Trim();
        var normalizedPhone = BuildPhoneNumber(model.PhoneCountryCode, model.Phone);

        if (model.PreferredDate.Date < DateTime.Today)
        {
            ModelState.AddModelError(nameof(model.PreferredDate), "Preferred date cannot be in the past.");
        }

        if (model.ConsultationType == TeleconsultationType.Phone)
        {
            ModelState.AddModelError(nameof(model.ConsultationType), "Phone-call appointments are not booked online. Please call the hospital number for voice support.");
        }

        if (model.WhatsAppOptIn && string.IsNullOrWhiteSpace(NigerianPhoneNumber.NormalizeForWhatsApp(normalizedPhone)))
        {
            ModelState.AddModelError(nameof(model.Phone), "Enter a valid WhatsApp phone number, including country code if outside Nigeria.");
        }

        var departmentExists = await _context.Departments
            .AsNoTracking()
            .AnyAsync(d => d.Id == model.DepartmentId);

        if (!departmentExists)
        {
            ModelState.AddModelError(nameof(model.DepartmentId), "Please choose a valid department.");
        }

        if (model.DoctorId.HasValue)
        {
            var doctorValid = await _context.Doctors
                .AsNoTracking()
                .AnyAsync(d => d.Id == model.DoctorId.Value && d.DepartmentId == model.DepartmentId);

            if (!doctorValid)
            {
                ModelState.AddModelError(nameof(model.DoctorId), "Selected doctor is invalid for the chosen department.");
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(model.DepartmentId, model.DoctorId);
            return View(model);
        }

        var currentUser = User.Identity?.IsAuthenticated == true
            ? await _userManager.GetUserAsync(User)
            : null;
        var patientProfile = currentUser is null
            ? null
            : await _context.PatientProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ApplicationUserId == currentUser.Id);

        var request = new TeleconsultationRequest
        {
            PatientName = model.PatientName,
            Email = model.Email,
            Phone = normalizedPhone,
            DepartmentId = model.DepartmentId,
            DoctorId = model.DoctorId,
            ConsultationType = model.ConsultationType,
            PreferredDate = model.PreferredDate.Date,
            PreferredTime = model.PreferredTime,
            Reason = model.Reason,
            ConsentAccepted = model.ConsentAccepted,
            WhatsAppOptIn = model.WhatsAppOptIn,
            Status = TeleconsultationStatus.Pending,
            ApplicationUserId = currentUser?.Id,
            PatientProfileId = patientProfile?.Id,
            CreatedAt = DateTime.UtcNow
        };

        _context.TeleconsultationRequests.Add(request);
        await _context.SaveChangesAsync();

        var departmentName = await _context.Departments
            .AsNoTracking()
            .Where(d => d.Id == request.DepartmentId)
            .Select(d => d.Name)
            .FirstOrDefaultAsync() ?? string.Empty;

        var doctorName = request.DoctorId.HasValue
            ? await _context.Doctors.AsNoTracking().Where(d => d.Id == request.DoctorId.Value).Select(d => d.FullName).FirstOrDefaultAsync() ?? "To be assigned"
            : "To be assigned";

        var notification = new NotificationRequest
        {
            PatientName = request.PatientName,
            PatientEmail = request.Email,
            PatientPhone = request.Phone,
            DoctorName = doctorName,
            Department = departmentName,
            AppointmentDateTime = CombineDateAndTime(request.PreferredDate, request.PreferredTime),
            ConfirmationRef = $"TC-{request.Id:D6}",
            AppointmentRequestId = null,
            TeleconsultationRequestId = request.Id
        };

        await _context.Entry(request).Reference(r => r.Department).LoadAsync();
        if (request.DoctorId.HasValue)
        {
            await _context.Entry(request).Reference(r => r.Doctor).LoadAsync();
        }

        await SendRequestReceivedNotificationsAsync(request, notification);
        await PublishTeleconsultationSubmittedAsync(request, departmentName);

        return RedirectToAction(nameof(Submitted), new { id = request.Id });
    }

    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Submitted(int id)
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

        return View(request);
    }

    private async Task PopulateLookupsAsync(int? selectedDepartmentId = null, int? selectedDoctorId = null)
    {
        var departments = await _context.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .ToListAsync();

        var doctors = await _context.Doctors
            .AsNoTracking()
            .OrderBy(d => d.FullName)
            .Select(d => new { d.Id, Name = d.FullName, d.DepartmentId })
            .ToListAsync();

        var filteredDoctors = selectedDepartmentId.HasValue
            ? doctors.Where(d => d.DepartmentId == selectedDepartmentId.Value).ToList()
            : doctors;

        ViewData["DepartmentId"] = new SelectList(departments, "Id", "Name", selectedDepartmentId);
        ViewData["DoctorId"] = new SelectList(filteredDoctors, "Id", "Name", selectedDoctorId);
        ViewBag.DoctorOptions = doctors;
    }

    private async Task SendRequestReceivedNotificationsAsync(
        TeleconsultationRequest request,
        NotificationRequest notification)
    {
        try
        {
            await _notifications.SendTeleconsultationReceivedAsync(notification);
            await _notifications.SendAdminAlertAsync(notification);
            await _whatsAppNotifications.SendTeleconsultationReceivedAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Teleconsultation request {TeleconsultationRequestId} was saved, but notification delivery failed.", request.Id);
        }
    }

    private async Task PublishTeleconsultationSubmittedAsync(TeleconsultationRequest request, string departmentName)
    {
        try
        {
            await _bookingHub.Clients.Group(BookingHubGroups.AdminQueue).SendAsync("teleconsultationSubmitted", new
            {
                id = request.Id,
                patientName = request.PatientName,
                department = departmentName,
                preferredDate = request.PreferredDate.ToString("MMM d, yyyy"),
                preferredTime = request.PreferredTime,
                status = request.Status.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Teleconsultation request {TeleconsultationRequestId} was saved, but realtime admin updates failed.", request.Id);
        }
    }

    private static DateTime CombineDateAndTime(DateTime date, string time)
    {
        return DateTime.TryParse(time, out var parsedTime)
            ? date.Date.Add(parsedTime.TimeOfDay)
            : date.Date;
    }

    private static string BuildPhoneNumber(string countryCode, string phone)
    {
        var trimmedPhone = phone.Trim();
        if (string.IsNullOrWhiteSpace(trimmedPhone))
            return string.Empty;

        if (trimmedPhone.StartsWith("+", StringComparison.Ordinal) ||
            trimmedPhone.StartsWith("00", StringComparison.Ordinal) ||
            trimmedPhone.StartsWith("234", StringComparison.Ordinal) ||
            trimmedPhone.StartsWith("0", StringComparison.Ordinal))
        {
            return trimmedPhone;
        }

        var trimmedCountryCode = string.IsNullOrWhiteSpace(countryCode) ? "+234" : countryCode.Trim();
        return $"{trimmedCountryCode}{trimmedPhone}";
    }
}
