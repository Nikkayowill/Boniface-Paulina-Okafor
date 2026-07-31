using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Okafor_.NET.Data;
using Okafor_.NET.Models;

namespace Okafor_.NET.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Staff")]
public class DonationsController : Controller
{
    private readonly ApplicationDbContext _context;

    public DonationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DonationStatus? status = null, string? purpose = null, string? query = null)
    {
        var donations = _context.Donations
            .AsNoTracking()
            .OrderByDescending(donation => donation.CreatedAt)
            .AsQueryable();

        if (status.HasValue)
        {
            donations = donations.Where(donation => donation.Status == status.Value);
        }

        var normalizedPurpose = DonationPurposeCodes.IsSupported(purpose) ? purpose : null;
        if (!string.IsNullOrWhiteSpace(normalizedPurpose))
        {
            donations = donations.Where(donation => donation.PurposeCode == normalizedPurpose);
        }

        var normalizedQuery = query?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            donations = donations.Where(donation =>
                donation.PaymentReference.Contains(normalizedQuery) ||
                (donation.ProviderReference != null && donation.ProviderReference.Contains(normalizedQuery)) ||
                (donation.DonorName != null && donation.DonorName.Contains(normalizedQuery)) ||
                (donation.DonorEmail != null && donation.DonorEmail.Contains(normalizedQuery)) ||
                (donation.DonorPhone != null && donation.DonorPhone.Contains(normalizedQuery)));
        }

        var matchingCount = await donations.CountAsync();
        var confirmedProductionCount = await donations.CountAsync(donation =>
            donation.Status == DonationStatus.Paid && !donation.IsSandbox);
        var pendingCount = await donations.CountAsync(donation => donation.Status == DonationStatus.Pending);

        ViewData["Status"] = new SelectList(Enum.GetValues<DonationStatus>(), status);
        ViewData["Purpose"] = new SelectList(
            new[]
            {
                new { Value = DonationPurposeCodes.GeneralHospitalSupport, Text = DonationPurposeCodes.GetDisplayName(DonationPurposeCodes.GeneralHospitalSupport) },
                new { Value = DonationPurposeCodes.FatherToochukwuSpiritualCare, Text = DonationPurposeCodes.GetDisplayName(DonationPurposeCodes.FatherToochukwuSpiritualCare) }
            },
            "Value",
            "Text",
            normalizedPurpose);
        ViewData["Query"] = normalizedQuery;
        ViewData["SelectedPurpose"] = normalizedPurpose;
        ViewData["MatchingCount"] = matchingCount;
        ViewData["ConfirmedProductionCount"] = confirmedProductionCount;
        ViewData["PendingCount"] = pendingCount;
        return View(await donations.Take(250).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var donation = await _context.Donations
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        return donation is null ? NotFound() : View(donation);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, DonationStatus status, string? staffNotes)
    {
        var donation = await _context.Donations.FirstOrDefaultAsync(item => item.Id == id);
        if (donation is null)
        {
            return NotFound();
        }

        if (!string.Equals(donation.Provider, "Manual", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Provider-backed donation statuses must be updated through payment verification.");
        }

        if (!CanTransition(donation.Status, status))
        {
            return BadRequest("That donation status transition is not allowed.");
        }

        var normalizedNotes = string.IsNullOrWhiteSpace(staffNotes) ? null : staffNotes.Trim();
        if (normalizedNotes?.Length > 1000)
        {
            ModelState.AddModelError(nameof(staffNotes), "Staff notes cannot exceed 1000 characters.");
            return View("Details", donation);
        }

        donation.Status = status;
        donation.StaffNotes = normalizedNotes;
        donation.ReviewedAt = DateTime.UtcNow;
        donation.ReviewedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        donation.ProviderMessage = status switch
        {
            DonationStatus.Contacted => "Hospital staff contacted the prospective donor.",
            DonationStatus.Paid => "Hospital staff confirmed that the donation was received.",
            DonationStatus.Cancelled => "Donation follow-up was closed without a confirmed payment.",
            _ => donation.ProviderMessage
        };

        if (status == DonationStatus.Paid)
        {
            donation.PaidAt ??= DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    internal static bool CanTransition(DonationStatus current, DonationStatus next)
    {
        return (current, next) switch
        {
            (DonationStatus.Pending, DonationStatus.Contacted or DonationStatus.Paid or DonationStatus.Cancelled) => true,
            (DonationStatus.Contacted, DonationStatus.Paid or DonationStatus.Cancelled) => true,
            _ => false
        };
    }
}
