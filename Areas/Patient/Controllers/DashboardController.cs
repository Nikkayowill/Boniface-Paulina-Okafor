using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;

namespace Okafor_.NET.Areas.Patient.Controllers;

public class DashboardController : PatientBaseController
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;
        var user = await _userManager.FindByIdAsync(userId);
        var profile = await _context.PatientProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ApplicationUserId == userId);

        // Dashboard data
        var dashboardData = new PatientDashboardViewModel();

        if (profile is not null)
        {
            // Upcoming appointments (next 7 days)
            var sevenDaysFromNow = DateTime.Today.AddDays(7);
            dashboardData.UpcomingAppointmentsCount = await _context.PatientAppointments
                .CountAsync(a => a.PatientProfileId == profile.Id &&
                                a.AppointmentDate >= DateTime.Today &&
                                a.AppointmentDate <= sevenDaysFromNow);

            dashboardData.PendingDocumentsCount = await _context.PatientDocuments
                .CountAsync(d => d.PatientProfileId == profile.Id);

            dashboardData.UnreadMessagesCount = await _context.PatientMessages
                .CountAsync(m => m.PatientProfileId == profile.Id && !m.IsRead);

        }

        dashboardData.PendingTeleconsultationsCount = await _context.TeleconsultationRequests
            .CountAsync(t =>
                (t.ApplicationUserId == userId ||
                 (profile != null && t.PatientProfileId == profile.Id) ||
                 (user != null && t.Email == user.Email)) &&
                (t.Status == TeleconsultationStatus.Pending ||
                 t.Status == TeleconsultationStatus.Confirmed));

        // Public booking requests by email
        if (user?.Email is not null)
        {
            dashboardData.PendingBookingRequestsCount = await _context.AppointmentRequests
                .CountAsync(r => r.Email == user.Email &&
                                r.Status == AppointmentStatus.Pending &&
                                !_context.PatientAppointments.Any(pa => pa.AppointmentRequestId == r.Id));
        }

        dashboardData.PatientName = profile?.FullName ?? user?.UserName ?? "Patient";
        dashboardData.HasProfile = profile is not null;

        return View(dashboardData);
    }
}

public class PatientDashboardViewModel
{
    public string PatientName { get; set; } = string.Empty;
    public bool HasProfile { get; set; }
    public int UpcomingAppointmentsCount { get; set; }
    public int PendingDocumentsCount { get; set; }
    public int UnreadMessagesCount { get; set; }
    public int PendingTeleconsultationsCount { get; set; }
    public int PendingBookingRequestsCount { get; set; }
}
