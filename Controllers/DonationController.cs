using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.Services;
using Okafor_.NET.ViewModels;

namespace Okafor_.NET.Controllers;

[Route("Donation")]
public class DonationController : Controller
{
    private static readonly Regex PaymentReferencePattern = new("^[A-Za-z0-9-]{6,100}$", RegexOptions.Compiled);

    private readonly ApplicationDbContext _context;
    private readonly IDonationReceiptEmailSender _emailSender;
    private readonly IPaymentGateway _paymentGateway;
    private readonly ILogger<DonationController> _logger;

    public DonationController(
        ApplicationDbContext context,
        IDonationReceiptEmailSender emailSender,
        IPaymentGateway paymentGateway,
        ILogger<DonationController> logger)
    {
        _context = context;
        _emailSender = emailSender;
        _paymentGateway = paymentGateway;
        _logger = logger;
    }

    [HttpGet("")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Index(string? purpose = null)
    {
        SetDonationViewData();
        var purposeCode = DonationPurposeCodes.IsSupported(purpose)
            ? purpose!
            : DonationPurposeCodes.GeneralHospitalSupport;
        return View(new DonationInterestViewModel
        {
            Currency = "NGN",
            PurposeCode = purposeCode
        });
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(
        [Bind("DonorName,DonorEmail,DonorPhone,Amount,Currency,PurposeCode,PreferredMethod,DonorMessage,ContactConsent")]
        DonationInterestViewModel model)
    {
        model.DonorName = model.DonorName?.Trim() ?? string.Empty;
        model.DonorEmail = string.IsNullOrWhiteSpace(model.DonorEmail) ? null : model.DonorEmail.Trim();
        model.DonorPhone = string.IsNullOrWhiteSpace(model.DonorPhone) ? null : model.DonorPhone.Trim();
        model.Currency = (model.Currency ?? string.Empty).Trim().ToUpperInvariant();
        model.PurposeCode = (model.PurposeCode ?? string.Empty).Trim();
        model.PreferredMethod = (model.PreferredMethod ?? string.Empty).Trim();
        model.DonorMessage = string.IsNullOrWhiteSpace(model.DonorMessage) ? null : model.DonorMessage.Trim();

        ValidateDonationInterestModel(model);

        if (string.IsNullOrWhiteSpace(model.DonorEmail) && string.IsNullOrWhiteSpace(model.DonorPhone))
        {
            ModelState.AddModelError(nameof(model.DonorEmail), "Enter an email address or phone number so the hospital can follow up.");
            ModelState.AddModelError(nameof(model.DonorPhone), "Enter an email address or phone number so the hospital can follow up.");
        }

        if (!DonationPurposeCodes.IsSupported(model.PurposeCode))
        {
            ModelState.AddModelError(nameof(model.PurposeCode), "Choose a valid donation purpose.");
        }

        if (!DonationMethodCodes.IsSupported(model.PreferredMethod))
        {
            ModelState.AddModelError(nameof(model.PreferredMethod), "Choose how you would like to arrange the donation.");
        }

        if (!ModelState.IsValid)
        {
            SetDonationViewData();
            return View(model);
        }

        var donation = new Donation
        {
            DonorName = model.DonorName,
            DonorEmail = model.DonorEmail,
            DonorPhone = model.DonorPhone,
            Amount = model.Amount,
            Currency = model.Currency,
            PurposeCode = model.PurposeCode,
            PreferredMethod = model.PreferredMethod,
            DonorMessage = model.DonorMessage,
            ContactConsent = model.ContactConsent,
            PaymentReference = $"DON-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}".ToUpperInvariant(),
            Status = DonationStatus.Pending,
            Provider = "Manual",
            Channel = "HospitalFollowUp",
            ProviderMessage = "Donation interest received. No payment has been collected.",
            IsSandbox = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Donations.Add(donation);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Confirmation), new { id = donation.Id, reference = donation.PaymentReference });
    }

    [HttpGet("Confirmation/{id:int}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Confirmation(int id, string? reference)
    {
        var donation = await FindByPrivateReferenceAsync(id, reference);
        return donation is null ? NotFound() : View(donation);
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
        PaymentVerificationResult verification;
        try
        {
            verification = await _paymentGateway.VerifyAsync(reference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Donation verification failed for donation {DonationId}.", donation.Id);
            donation.ProviderMessage = "Donation verification is temporarily unavailable.";
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Receipt), new { id = donation.Id, reference = donation.PaymentReference });
        }

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
        var donation = await FindByPrivateReferenceAsync(id, reference);
        return donation is null ? NotFound() : View(donation);
    }

    internal static void ApplyVerification(Donation donation, PaymentVerificationResult verification)
    {
        PaymentVerificationApplicator.ApplyTo(donation, verification);
    }

    private async Task<Donation?> FindByPrivateReferenceAsync(int id, string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return null;
        }

        var normalizedReference = reference.Trim().ToUpperInvariant();
        if (!PaymentReferencePattern.IsMatch(normalizedReference))
        {
            return null;
        }

        return await _context.Donations
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id && item.PaymentReference == normalizedReference);
    }

    private void ValidateDonationInterestModel(DonationInterestViewModel model)
    {
        ModelState.Clear();
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            validationResults,
            validateAllProperties: true);

        foreach (var validationResult in validationResults)
        {
            var memberNames = validationResult.MemberNames.Any()
                ? validationResult.MemberNames
                : [string.Empty];
            foreach (var memberName in memberNames)
            {
                ModelState.AddModelError(
                    memberName,
                    validationResult.ErrorMessage ?? "The submitted value is invalid.");
            }
        }
    }

    private void SetDonationViewData()
    {
        ViewData["DonationPurposes"] = new SelectList(
            new[]
            {
                new { Value = DonationPurposeCodes.GeneralHospitalSupport, Text = DonationPurposeCodes.GetDisplayName(DonationPurposeCodes.GeneralHospitalSupport) },
                new { Value = DonationPurposeCodes.FatherToochukwuSpiritualCare, Text = DonationPurposeCodes.GetDisplayName(DonationPurposeCodes.FatherToochukwuSpiritualCare) }
            },
            "Value",
            "Text");
        ViewData["DonationMethods"] = new SelectList(
            new[]
            {
                new { Value = DonationMethodCodes.HospitalContact, Text = DonationMethodCodes.GetDisplayName(DonationMethodCodes.HospitalContact) },
                new { Value = DonationMethodCodes.BankTransfer, Text = DonationMethodCodes.GetDisplayName(DonationMethodCodes.BankTransfer) },
                new { Value = DonationMethodCodes.InPerson, Text = DonationMethodCodes.GetDisplayName(DonationMethodCodes.InPerson) }
            },
            "Value",
            "Text");
    }
}
