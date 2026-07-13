using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Hubs;
using Okafor_.NET.Models;
using Okafor_.NET.ViewModels;

namespace Okafor_.NET.Areas.Patient.Controllers;

public class TeleconsultationsController : PatientBaseController
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHubContext<BookingHub> _bookingHub;
    private readonly ILogger<TeleconsultationsController> _logger;

    public TeleconsultationsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IHubContext<BookingHub> bookingHub,
        ILogger<TeleconsultationsController> logger)
    {
        _context = context;
        _userManager = userManager;
        _bookingHub = bookingHub;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;
        var user = await _userManager.FindByIdAsync(userId);

        // Match by userId (authenticated submissions) or by email (guest submissions later linked)
        var requests = await _context.TeleconsultationRequests
            .AsNoTracking()
            .Where(r => r.ApplicationUserId == userId ||
                        (user != null && r.Email == user.Email))
            .Include(r => r.Department)
            .Include(r => r.Doctor)
            .OrderByDescending(r => r.PreferredDate)
            .Select(r => new PortalTeleconsultationViewModel
            {
                Id = r.Id,
                PreferredDate = r.PreferredDate,
                PreferredTime = r.PreferredTime,
                Department = r.Department != null ? r.Department.Name : "—",
                Doctor = r.Doctor != null ? r.Doctor.FullName : null,
                ConsultationType = r.ConsultationType.ToString(),
                Status = r.Status.ToString(),
                MeetingLink = r.MeetingLink,
                AdminNotes = r.AdminNotes,
                Reason = r.Reason,
                WhatsAppOptIn = r.WhatsAppOptIn
            })
            .ToListAsync();

        return View(requests);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var user = await _userManager.FindByIdAsync(userId);

        var request = await _context.TeleconsultationRequests
            .FirstOrDefaultAsync(r =>
                r.Id == id &&
                (r.ApplicationUserId == userId || (user != null && r.Email == user.Email)));

        if (request is null)
        {
            return NotFound();
        }

        if (request.Status != TeleconsultationStatus.Pending)
        {
            TempData["Error"] = "Only pending teleconsultation requests can be cancelled.";
            return RedirectToAction(nameof(Index));
        }

        request.Status = TeleconsultationStatus.Cancelled;
        request.UpdatedAt = DateTime.UtcNow;
        request.AdminNotes = string.IsNullOrWhiteSpace(request.AdminNotes)
            ? "Cancelled by patient."
            : $"{request.AdminNotes} [CANCELLED BY PATIENT]";
        await _context.SaveChangesAsync();
        await PublishCancellationSafelyAsync(request.Id);

        TempData["Success"] = "Teleconsultation request cancelled.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PublishCancellationSafelyAsync(int id)
    {
        try
        {
            await _bookingHub.Clients.Group(BookingHubGroups.AdminQueue).SendAsync("bookingActioned", new
            {
                type = "teleconsultation",
                id,
                status = "Cancelled"
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Patient cancellation for teleconsultation {TeleconsultationRequestId} succeeded, but the admin realtime update failed.", id);
        }
    }
}
