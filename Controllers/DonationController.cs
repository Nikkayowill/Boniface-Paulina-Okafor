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
    private readonly IPaymentGateway _paymentGateway;

    public DonationController(
        ApplicationDbContext context,
        IDonationReceiptEmailSender emailSender,
        IPaymentGateway paymentGateway)
    {
        _context = context;
        _emailSender = emailSender;
        _paymentGateway = paymentGateway;
    }

    [HttpGet("")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Index()
    {
        ViewData["PaymentProvider"] = _paymentGateway.ProviderName;
        ViewData["IsSandbox"] = _paymentGateway.IsSandbox;
        return View(new Donation { Currency = "NGN" });
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index([Bind("DonorName,DonorEmail,Amount,Currency")] Donation donation)
    {
        donation.DonorName = donation.DonorName?.Trim();
        donation.DonorEmail = donation.DonorEmail?.Trim();
        donation.Currency = (donation.Currency ?? string.Empty).Trim().ToUpperInvariant();
        donation.PaymentReference = $"DON-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}".ToUpperInvariant();
        donation.Provider = _paymentGateway.ProviderName;
        donation.IsSandbox = _paymentGateway.IsSandbox;
        ModelState.Remove(nameof(donation.PaymentReference));

        if (string.IsNullOrWhiteSpace(donation.DonorEmail))
        {
            ModelState.AddModelError(nameof(donation.DonorEmail), "Enter an email address so we can process the online donation and send a receipt.");
        }

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
            ViewData["PaymentProvider"] = _paymentGateway.ProviderName;
            ViewData["IsSandbox"] = _paymentGateway.IsSandbox;
            return View(donation);
        }

        donation.CreatedAt = DateTime.UtcNow;
        donation.Status = DonationStatus.Pending;

        _context.Donations.Add(donation);
        await _context.SaveChangesAsync();

        var callbackUrl = Url.ActionLink(nameof(Callback), values: null, protocol: Request.Scheme)
            ?? throw new InvalidOperationException("Unable to build donation callback URL.");
        var result = await _paymentGateway.InitializeAsync(new PaymentInitializeRequest(
            Email: donation.DonorEmail!,
            Amount: donation.Amount,
            Currency: donation.Currency,
            Reference: donation.PaymentReference,
            CallbackUrl: callbackUrl,
            Purpose: "Donation",
            CustomerName: string.IsNullOrWhiteSpace(donation.DonorName) ? "Anonymous donor" : donation.DonorName,
            Metadata: new Dictionary<string, string>
            {
                ["DonationId"] = donation.Id.ToString()
            }));

        donation.Provider = result.Provider;
        donation.ProviderReference = result.ProviderReference;
        donation.Channel = result.Channel;
        donation.ProviderMessage = result.Message;
        donation.IsSandbox = result.IsSandbox;

        if (!result.Success)
        {
            donation.Status = DonationStatus.Failed;
            await _context.SaveChangesAsync();
            ModelState.AddModelError(string.Empty, result.Message);
            ViewData["PaymentProvider"] = _paymentGateway.ProviderName;
            ViewData["IsSandbox"] = _paymentGateway.IsSandbox;
            return View(donation);
        }

        if (result.RequiresRedirect && !string.IsNullOrWhiteSpace(result.AuthorizationUrl))
        {
            await _context.SaveChangesAsync();
            return Redirect(result.AuthorizationUrl);
        }

        donation.Status = result.IsSandbox ? DonationStatus.SandboxApproved : DonationStatus.Paid;
        donation.PaidAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(donation.DonorEmail))
        {
            await _emailSender.SendReceiptAsync(donation);
        }

        return RedirectToAction(nameof(Receipt), new { id = donation.Id, reference = donation.PaymentReference });
    }

    [HttpGet("Callback")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Callback(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return RedirectToAction(nameof(Index));
        }

        var donation = await _context.Donations
            .FirstOrDefaultAsync(item => item.PaymentReference == reference || item.ProviderReference == reference);

        if (donation is null)
        {
            return NotFound();
        }

        var wasAlreadyPaid = donation.Status is DonationStatus.Paid or DonationStatus.SandboxApproved;
        var verification = await _paymentGateway.VerifyAsync(reference);
        ApplyVerification(donation, verification);
        await _context.SaveChangesAsync();

        if (!wasAlreadyPaid && donation.Status is DonationStatus.Paid or DonationStatus.SandboxApproved)
        {
            await _emailSender.SendReceiptAsync(donation);
        }

        return RedirectToAction(nameof(Receipt), new { id = donation.Id, reference = donation.PaymentReference });
    }

    [HttpGet("Receipt/{id:int}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
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

    internal static void ApplyVerification(Donation donation, PaymentVerificationResult verification)
    {
        donation.ProviderReference = verification.ProviderReference;
        donation.Channel = verification.Channel;
        donation.ProviderMessage = verification.Message;
        donation.IsSandbox = verification.IsSandbox;

        var amountMatches = !verification.Amount.HasValue || verification.Amount.Value == donation.Amount;
        var currencyMatches = string.IsNullOrWhiteSpace(verification.Currency) ||
            string.Equals(verification.Currency, donation.Currency, StringComparison.OrdinalIgnoreCase);

        donation.Status = verification.Success && amountMatches && currencyMatches
            ? verification.IsSandbox ? DonationStatus.SandboxApproved : DonationStatus.Paid
            : DonationStatus.Failed;
        donation.PaidAt = donation.Status is DonationStatus.Paid or DonationStatus.SandboxApproved
            ? verification.PaidAt ?? DateTime.UtcNow
            : null;
    }
}
