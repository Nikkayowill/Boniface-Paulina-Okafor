using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.Services;

namespace Okafor_.NET.Controllers;

[Route("Donation")]
public class DonationController : Controller
{
    private static readonly Regex PaymentReferencePattern = new("^[A-Za-z0-9-]{6,100}$", RegexOptions.Compiled);
    private static readonly Regex CurrencyPattern = new("^[A-Z]{3}$", RegexOptions.Compiled);

    private readonly ApplicationDbContext _context;
    private readonly IDonationReceiptEmailSender _emailSender;

    public DonationController(ApplicationDbContext context, IDonationReceiptEmailSender emailSender)
    {
        _context = context;
        _emailSender = emailSender;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        return View(new Donation { Currency = "NGN" });
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index([Bind("DonorName,DonorEmail,Amount,Currency,PaymentReference")] Donation donation)
    {
        donation.DonorName = donation.DonorName?.Trim();
        donation.DonorEmail = donation.DonorEmail?.Trim();
        donation.Currency = donation.Currency.Trim().ToUpperInvariant();
        donation.PaymentReference = donation.PaymentReference.Trim().ToUpperInvariant();

        if (!CurrencyPattern.IsMatch(donation.Currency))
        {
            ModelState.AddModelError(nameof(donation.Currency), "Enter a valid 3-letter currency code.");
        }

        if (!PaymentReferencePattern.IsMatch(donation.PaymentReference))
        {
            ModelState.AddModelError(nameof(donation.PaymentReference), "Enter a valid payment reference using letters, numbers, or hyphens only.");
        }

        if (await _context.Donations.AnyAsync(item => item.PaymentReference == donation.PaymentReference))
        {
            ModelState.AddModelError(nameof(donation.PaymentReference), "This payment reference has already been used.");
        }

        if (!ModelState.IsValid)
        {
            return View(donation);
        }

        donation.CreatedAt = DateTime.UtcNow;

        _context.Donations.Add(donation);
        await _context.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(donation.DonorEmail))
        {
            await _emailSender.SendReceiptAsync(donation);
        }

        return RedirectToAction(nameof(Receipt), new { id = donation.Id, reference = donation.PaymentReference });
    }

    [HttpGet("Receipt/{id:int}")]
    public async Task<IActionResult> Receipt(int id, string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return NotFound();
        }

        var normalizedReference = reference.Trim().ToUpperInvariant();
        if (!PaymentReferencePattern.IsMatch(normalizedReference))
        {
            return NotFound();
        }

        var donation = await _context.Donations
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id && item.PaymentReference == normalizedReference);

        if (donation is null)
        {
            return NotFound();
        }

        return View(donation);
    }
}