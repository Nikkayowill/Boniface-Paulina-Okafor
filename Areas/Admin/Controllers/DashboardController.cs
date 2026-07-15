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
                Category = "Appointments",
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
                Category = "Teleconsultations",
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
                Category = "Billing",
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        var recentDonations = await _context.Donations
            .AsNoTracking()
            .Where(donation => donation.CreatedAt >= recentCutoff)
            .OrderByDescending(donation => donation.CreatedAt)
            .Take(5)
            .Select(donation => new AdminDashboardActivityViewModel
            {
                Title = $"Donation from {(donation.DonorName ?? "Anonymous donor")}",
                Details = donation.PurposeCode == DonationPurposeCodes.FatherToochukwuSpiritualCare
                    ? $"{donation.Currency} {donation.Amount:N2} - Father Toochukwu program - {donation.Status}"
                    : $"{donation.Currency} {donation.Amount:N2} - General hospital support - {donation.Status}",
                Category = "Donations",
                CreatedAt = donation.CreatedAt
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
                Category = "Messages",
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
                Category = "Patient Messages",
                CreatedAt = message.SentAt
            })
            .ToListAsync();

        var model = new AdminDashboardViewModel
        {
            DoctorsCount = await _context.Doctors.CountAsync(),
            DepartmentsCount = await _context.Departments.CountAsync(),
            AppointmentsCount = await _context.AppointmentRequests.CountAsync(),
            PostsCount = await _context.Posts.CountAsync(),
            ContactSubmissionsCount = await _context.ContactSubmissions.CountAsync(),
            UnreadPatientMessagesCount = await _context.PatientMessages.CountAsync(message => !message.IsRead)
        };

        model.PendingAppointmentsCount = await _context.AppointmentRequests.CountAsync(a => a.Status == AppointmentStatus.Pending);
        model.ApprovedAppointmentsCount = await _context.AppointmentRequests.CountAsync(a => a.Status == AppointmentStatus.Approved);
        model.RejectedAppointmentsCount = await _context.AppointmentRequests.CountAsync(a => a.Status == AppointmentStatus.Rejected);

        model.PendingTeleconsultationsCount = await _context.TeleconsultationRequests.CountAsync(t => t.Status == TeleconsultationStatus.Pending);
        model.ConfirmedTeleconsultationsCount = await _context.TeleconsultationRequests.CountAsync(t => t.Status == TeleconsultationStatus.Confirmed);
        model.RescheduledTeleconsultationsCount = await _context.TeleconsultationRequests.CountAsync(t => t.Status == TeleconsultationStatus.Rescheduled);

        model.PendingBillPaymentsCount = await _context.BillPayments.CountAsync(p => p.Status == BillPaymentStatus.Pending);
        model.PaidBillPaymentsCount = await _context.BillPayments.CountAsync(p => p.Status == BillPaymentStatus.Paid);
        model.TotalPaidRevenue = await _context.BillPayments
            .Where(p => p.Status == BillPaymentStatus.Paid && p.Currency == "NGN")
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;
        model.PendingDonationsCount = await _context.Donations.CountAsync(donation => donation.Status == DonationStatus.Pending);
        model.ConfirmedDonationsCount = await _context.Donations.CountAsync(donation => donation.Status == DonationStatus.Paid);

        model.RecentActivity = recentAppointments
            .Concat(recentTeleconsultations)
            .Concat(recentPayments)
            .Concat(recentDonations)
            .Concat(recentContacts)
            .Concat(recentPatientMessages)
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .ToList();

        return View(model);
    }
}
