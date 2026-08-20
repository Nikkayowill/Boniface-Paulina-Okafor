using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.ViewModels;

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

        // Every visit still ahead of the patient, from both places a visit can
        // live — exactly what the plate below draws on and what the Appointments
        // page lists. This used to count staff-scheduled appointments only, and
        // only ones falling inside the next seven days, which let the dashboard
        // print "Your next visit: Monday 24 August" and "Appointments — None
        // coming up" in the same screenful. A patient who reads both and
        // believes the second one does not come in.
        var now = DateTime.Now;

        if (profile is not null)
        {
            dashboardData.UpcomingAppointmentsCount = await _context.PatientAppointments
                .CountAsync(a => a.PatientProfileId == profile.Id &&
                                a.AppointmentDate > now &&
                                (a.Status == PatientAppointmentStatus.Scheduled ||
                                 a.Status == PatientAppointmentStatus.Confirmed));

            dashboardData.PendingDocumentsCount = await _context.PatientDocuments
                .CountAsync(d => d.PatientProfileId == profile.Id);

            dashboardData.MessagesAwaitingReviewCount = await _context.PatientMessages
                .CountAsync(m => m.PatientProfileId == profile.Id && !m.IsRead);

        }

        dashboardData.PendingTeleconsultationsCount = await _context.TeleconsultationRequests
            .CountAsync(t =>
                (t.ApplicationUserId == userId ||
                 (profile != null && t.PatientProfileId == profile.Id) ||
                 (user != null && t.Email == user.Email)) &&
                (t.Status == TeleconsultationStatus.Pending ||
                 t.Status == TeleconsultationStatus.Confirmed));

        // A booking request the patient sent that has not been turned into a
        // scheduled appointment yet is still a visit they are waiting on, so it
        // counts. The same exclusion the Appointments page merge uses keeps a
        // request that HAS become an appointment from being counted twice.
        if (user?.Email is not null)
        {
            dashboardData.UpcomingAppointmentsCount += await _context.AppointmentRequests
                .CountAsync(r => r.Email == user.Email &&
                                r.PreferredDate >= now.Date &&
                                (r.Status == AppointmentStatus.Pending ||
                                 r.Status == AppointmentStatus.Approved) &&
                                !_context.PatientAppointments.Any(pa => pa.AppointmentRequestId == r.Id));
        }

        dashboardData.PatientName = profile?.FullName ?? user?.UserName ?? "Patient";
        dashboardData.HasProfile = profile is not null;
        dashboardData.NextVisit = await FindNextVisitAsync(profile, user);

        return View(dashboardData);
    }

    /// <summary>
    /// The soonest visit still ahead of the patient, drawn from both places a
    /// visit can live: an appointment staff scheduled, or a booking request the
    /// patient sent that has not been turned into one yet. A request that is
    /// still Pending is included deliberately — "we have your request and no
    /// date is set" is the answer the patient came for just as much as a
    /// confirmed date is.
    /// </summary>
    private async Task<PortalAppointmentViewModel?> FindNextVisitAsync(PatientProfile? profile, ApplicationUser? user)
    {
        var now = DateTime.Now;
        PortalAppointmentViewModel? next = null;

        if (profile is not null)
        {
            var scheduled = await _context.PatientAppointments
                .AsNoTracking()
                .Include(a => a.Department)
                .Include(a => a.Doctor)
                .Where(a => a.PatientProfileId == profile.Id &&
                            a.AppointmentDate > now &&
                            (a.Status == PatientAppointmentStatus.Scheduled ||
                             a.Status == PatientAppointmentStatus.Confirmed))
                .OrderBy(a => a.AppointmentDate)
                .FirstOrDefaultAsync();

            if (scheduled is not null)
            {
                next = new PortalAppointmentViewModel
                {
                    SourceId = scheduled.Id,
                    BookingStatusId = scheduled.AppointmentRequestId ?? scheduled.Id,
                    Date = scheduled.AppointmentDate,
                    Department = scheduled.Department?.Name ?? "—",
                    Doctor = scheduled.Doctor?.FullName,
                    Status = scheduled.Status.ToString(),
                    Notes = scheduled.Notes,
                    Source = "Booked by the hospital",
                    SourceType = "scheduled",
                    Subject = $"Hospital Appointment - {scheduled.Department?.Name ?? "General"}"
                };
            }
        }

        if (user?.Email is not null)
        {
            var openRequests = await _context.AppointmentRequests
                .AsNoTracking()
                .Include(r => r.Department)
                .Include(r => r.Doctor)
                .Where(r => r.Email == user.Email &&
                            (r.Status == AppointmentStatus.Pending || r.Status == AppointmentStatus.Approved) &&
                            !_context.PatientAppointments.Any(pa => pa.AppointmentRequestId == r.Id))
                .ToListAsync();

            // The date and time are combined in memory, so the ordering has to
            // happen here rather than in the query.
            var nextRequest = openRequests
                .Select(r => new PortalAppointmentViewModel
                {
                    SourceId = r.Id,
                    BookingStatusId = r.Id,
                    Date = PortalAppointmentTime.Combine(r.PreferredDate, r.PreferredTime),
                    Department = r.Department?.Name ?? "—",
                    Doctor = r.Doctor?.FullName,
                    Status = r.Status.ToString(),
                    Notes = r.Message,
                    Source = "You asked for this",
                    SourceType = "request",
                    Subject = $"Hospital Appointment Request - {r.Department?.Name ?? "General"}"
                })
                .Where(r => r.Date > now)
                .OrderBy(r => r.Date)
                .FirstOrDefault();

            if (nextRequest is not null && (next is null || nextRequest.Date < next.Date))
            {
                next = nextRequest;
            }
        }

        return next;
    }
}

public class PatientDashboardViewModel
{
    public string PatientName { get; set; } = string.Empty;
    public bool HasProfile { get; set; }
    public int UpcomingAppointmentsCount { get; set; }
    public int PendingDocumentsCount { get; set; }
    public int MessagesAwaitingReviewCount { get; set; }
    public int PendingTeleconsultationsCount { get; set; }

    /// <summary>The soonest visit still ahead, or null when nothing is booked.</summary>
    public PortalAppointmentViewModel? NextVisit { get; set; }
}
