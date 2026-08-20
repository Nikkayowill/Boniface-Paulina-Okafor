using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.ViewModels;

namespace Okafor_.NET.Areas.Admin.Controllers;

[Authorize(Roles = "Admin")]
public class DashboardController : AdminBaseController
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var recentCutoff = DateTime.UtcNow.AddDays(-7);

        var recentAppointments = await _context.AppointmentRequests
            .AsNoTracking()
            .Include(a => a.Department)
            .Where(a => a.CreatedAt >= recentCutoff)
            .OrderByDescending(a => a.CreatedAt)
            .Take(5)
            .Select(a => new AdminDashboardActivityViewModel
            {
                Title = $"Appointment request from {a.PatientName}",
                Details = $"{(a.Department != null ? a.Department.Name : "General")} - {a.Status}",
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        var recentTeleconsultations = await _context.TeleconsultationRequests
            .AsNoTracking()
            .Include(t => t.Department)
            .Where(t => t.CreatedAt >= recentCutoff)
            .OrderByDescending(t => t.CreatedAt)
            .Take(5)
            .Select(t => new AdminDashboardActivityViewModel
            {
                Title = $"Teleconsultation request from {t.PatientName}",
                Details = $"{(t.Department != null ? t.Department.Name : "General")} - {t.Status}",
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        var recentPayments = await _context.BillPayments
            .AsNoTracking()
            .Where(p => p.CreatedAt >= recentCutoff)
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .Select(p => new AdminDashboardActivityViewModel
            {
                Title = $"Bill payment {p.InvoiceNumber}",
                Details = $"{p.Currency} {p.Amount:N2} - {p.Status}",
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        var recentContacts = await _context.ContactSubmissions
            .AsNoTracking()
            .Where(c => c.SubmittedAt >= recentCutoff)
            .OrderByDescending(c => c.SubmittedAt)
            .Take(5)
            .Select(c => new AdminDashboardActivityViewModel
            {
                Title = $"Contact submission from {c.Name}",
                Details = c.Subject,
                CreatedAt = c.SubmittedAt
            })
            .ToListAsync();

        var recentPatientMessages = await _context.PatientMessages
            .AsNoTracking()
            .Include(message => message.PatientProfile)
            .Where(message => message.SentAt >= recentCutoff)
            .OrderByDescending(message => message.SentAt)
            .Take(5)
            .Select(message => new AdminDashboardActivityViewModel
            {
                Title = $"Patient message from {(message.PatientProfile != null ? message.PatientProfile.FullName : "Unknown patient")}",
                Details = message.Subject,
                CreatedAt = message.SentAt
            })
            .ToListAsync();

        // Only what the page prints. Counting doctors, departments, posts and
        // settled revenue fed a reference table that has been removed, so those
        // queries went with it.
        var model = new AdminDashboardViewModel
        {
            ContactSubmissionsCount = await _context.ContactSubmissions.CountAsync(),
            UnreadPatientMessagesCount = await _context.PatientMessages.CountAsync(message => !message.IsRead),
            PendingAppointmentsCount = await _context.AppointmentRequests.CountAsync(a => a.Status == AppointmentStatus.Pending),
            PendingTeleconsultationsCount = await _context.TeleconsultationRequests.CountAsync(t => t.Status == TeleconsultationStatus.Pending),
            PendingBillPaymentsCount = await _context.BillPayments.CountAsync(p => p.Status == BillPaymentStatus.Pending)
        };

        model.LongestWaiting = await FindLongestWaitingAsync();

        model.RecentActivity = recentAppointments
            .Concat(recentTeleconsultations)
            .Concat(recentPayments)
            .Concat(recentContacts)
            .Concat(recentPatientMessages)
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .ToList();

        return View(model);
    }

    /// <summary>
    /// The single request that has been waiting longest for staff attention.
    ///
    /// The dashboard used to open with six counts, which tell a member of staff
    /// how much is outstanding but never which item to pick up — and never how
    /// long someone has been waiting on it. Only the queues where a person is
    /// waiting for a reply are considered: contact submissions are a running
    /// inbox rather than a queue, and money queues are chased on their own
    /// schedule.
    /// </summary>
    private async Task<AdminOutstandingItemViewModel?> FindLongestWaitingAsync()
    {
        var candidates = new List<AdminOutstandingItemViewModel?>
        {
            await _context.AppointmentRequests
                .AsNoTracking()
                .Include(request => request.Department)
                .Where(request => request.Status == AppointmentStatus.Pending)
                .OrderBy(request => request.CreatedAt)
                .Select(request => new AdminOutstandingItemViewModel
                {
                    Queue = "Appointment request",
                    Who = request.PatientName,
                    What = request.Department != null ? request.Department.Name : "No department given",
                    WaitingSince = request.CreatedAt,
                    Controller = "AppointmentRequests",
                    Action = "Edit",
                    RecordId = request.Id
                })
                .FirstOrDefaultAsync(),

            await _context.TeleconsultationRequests
                .AsNoTracking()
                .Include(request => request.Department)
                .Where(request => request.Status == TeleconsultationStatus.Pending)
                .OrderBy(request => request.CreatedAt)
                .Select(request => new AdminOutstandingItemViewModel
                {
                    Queue = "Teleconsultation",
                    Who = request.PatientName,
                    What = request.Department != null ? request.Department.Name : "No department given",
                    WaitingSince = request.CreatedAt,
                    Controller = "Teleconsultations",
                    Action = "Edit",
                    RecordId = request.Id
                })
                .FirstOrDefaultAsync(),

            await _context.PatientMessages
                .AsNoTracking()
                .Include(message => message.PatientProfile)
                .Where(message => !message.IsRead)
                .OrderBy(message => message.SentAt)
                .Select(message => new AdminOutstandingItemViewModel
                {
                    Queue = "Patient message",
                    Who = message.PatientProfile != null ? message.PatientProfile.FullName : "Unknown patient",
                    What = message.Subject,
                    WaitingSince = message.SentAt,
                    Controller = "PatientMessages",
                    Action = "Details",
                    RecordId = message.Id
                })
                .FirstOrDefaultAsync()
        };

        return candidates
            .Where(candidate => candidate is not null)
            .OrderBy(candidate => candidate!.WaitingSince)
            .FirstOrDefault();
    }
}
