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
[RequireLaunchFeature(LaunchFeature.OnlineDonations)]
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
        return View(new DonationCheckoutViewModel
        {
            Currency = GetDefaultDonationCurrency(),
            PurposeCode = purposeCode
        });
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(
        [Bind("DonorName,DonorEmail,DonorPhone,Amount,Currency,PurposeCode,DonorMessage")]
        DonationCheckoutViewModel model)
    {
        model.DonorName = model.DonorName?.Trim() ?? string.Empty;
        model.DonorEmail = model.DonorEmail?.Trim() ?? string.Empty;
        model.DonorPhone = string.IsNullOrWhiteSpace(model.DonorPhone) ? null : model.DonorPhone.Trim();
        model.Currency = (model.Currency ?? string.Empty).Trim().ToUpperInvariant();
        model.PurposeCode = (model.PurposeCode ?? string.Empty).Trim();
        model.DonorMessage = string.IsNullOrWhiteSpace(model.DonorMessage) ? null : model.DonorMessage.Trim();

        ValidateDonationCheckoutModel(model);

        if (!DonationPurposeCodes.IsSupported(model.PurposeCode))
        {
            ModelState.AddModelError(nameof(model.PurposeCode), "Choose a valid donation purpose.");
        }

        if (!DonationCurrencyCodes.IsSupportedByProvider(
                model.Currency,
                _paymentGateway.ProviderName))
        {
            ModelState.AddModelError(
                nameof(model.Currency),
                "Choose a currency available for this checkout.");
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
            PreferredMethod = DonationMethodCodes.OnlineCheckout,
            DonorMessage = model.DonorMessage,
            ContactConsent = false,
            PaymentReference = $"DON-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}".ToUpperInvariant(),
            Status = DonationStatus.Pending,
            Provider = _paymentGateway.ProviderName,
            Channel = "Checkout",
            ProviderMessage = "Online donation checkout is being initialized.",
            IsSandbox = _paymentGateway.IsSandbox,
            CreatedAt = DateTime.UtcNow
        };
        donation.ProviderReference = donation.PaymentReference;

        _context.Donations.Add(donation);
        await _context.SaveChangesAsync();

        var callbackUrl = Url.ActionLink(nameof(Callback), values: null, protocol: Request.Scheme)
            ?? throw new InvalidOperationException("Unable to build donation callback URL.");
        PaymentInitializeResult result;
        try
        {
            result = await _paymentGateway.InitializeAsync(new PaymentInitializeRequest(
                Email: donation.DonorEmail!,
                Amount: donation.Amount,
                Currency: donation.Currency,
                Reference: donation.PaymentReference,
                CallbackUrl: callbackUrl,
                Purpose: DonationPurposeCodes.GetDisplayName(donation.PurposeCode),
                CustomerName: donation.DonorName ?? "Donor",
                Metadata: new Dictionary<string, string>
                {
                    ["Donation record"] = donation.Id.ToString()
                }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Donation checkout initialization failed for donation {DonationId}.", donation.Id);
            donation.Status = DonationStatus.Failed;
            donation.ProviderMessage = "The payment provider could not initialize checkout.";
            await _context.SaveChangesAsync();
            ModelState.AddModelError(string.Empty, "Online checkout is temporarily unavailable. No payment was taken; please try again.");
            SetDonationViewData();
            return View(model);
        }

        donation.Provider = NormalizeProviderText(result.Provider, _paymentGateway.ProviderName, 40);
        donation.ProviderReference = NormalizeProviderReference(result.ProviderReference, donation.PaymentReference);
        donation.Channel = NormalizeProviderText(result.Channel, "Checkout", 100);
        donation.ProviderMessage = NormalizeProviderText(result.Message, "Checkout initialization completed.", 1000);
        donation.IsSandbox = result.IsSandbox;

        if (result.Success &&
            !result.IsSandbox &&
            !string.Equals(donation.ProviderReference, donation.PaymentReference, StringComparison.Ordinal))
        {
            donation.Status = DonationStatus.Failed;
            donation.ProviderMessage = "The payment provider returned an unexpected transaction reference.";
            await _context.SaveChangesAsync();
            ModelState.AddModelError(string.Empty, "Online checkout could not be opened. No payment was taken; please try again.");
            SetDonationViewData();
            return View(model);
        }

        if (!result.Success)
        {
            donation.Status = DonationStatus.Failed;
            await _context.SaveChangesAsync();
            ModelState.AddModelError(string.Empty, "Online checkout is temporarily unavailable. No payment was taken; please try again.");
            SetDonationViewData();
            return View(model);
        }

        if (result.RequiresRedirect && TryGetSecureCheckoutUrl(
                result.AuthorizationUrl,
                result.Provider,
                out var checkoutUrl))
        {
            await _context.SaveChangesAsync();
            return Redirect(checkoutUrl);
        }

        if (!result.RequiresRedirect && result.IsSandbox)
        {
            donation.Status = DonationStatus.SandboxApproved;
            donation.PaidAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _emailSender.SendReceiptAsync(donation);
            return RedirectToAction(nameof(Receipt), new { id = donation.Id, reference = donation.PaymentReference });
        }

        donation.Status = DonationStatus.Failed;
        donation.ProviderMessage = "The payment provider did not return a secure hosted checkout URL.";
        await _context.SaveChangesAsync();
        ModelState.AddModelError(string.Empty, "Online checkout could not be opened. No payment was taken; please try again.");
        SetDonationViewData();
        return View(model);
    }

    [HttpGet("Callback")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Callback(string? reference)
    {
        var normalizedReference = reference?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedReference) ||
            !PaymentReferencePattern.IsMatch(normalizedReference))
        {
            return RedirectToAction(nameof(Index));
        }

        var donation = await _context.Donations
            .FirstOrDefaultAsync(item =>
                item.PaymentReference == normalizedReference ||
                item.ProviderReference == normalizedReference);

        if (donation is null)
        {
            return NotFound();
        }

        var wasAlreadyPaid = donation.Status is DonationStatus.Paid or DonationStatus.SandboxApproved;
        PaymentVerificationResult verification;
        try
        {
            verification = await _paymentGateway.VerifyAsync(
                donation.ProviderReference ?? donation.PaymentReference);
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

    private void ValidateDonationCheckoutModel(DonationCheckoutViewModel model)
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
        ViewData["PaymentProvider"] = _paymentGateway.ProviderName;
        ViewData["IsSandbox"] = _paymentGateway.IsSandbox;
        ViewData["DonationPurposes"] = new SelectList(
            new[]
            {
                new { Value = DonationPurposeCodes.GeneralHospitalSupport, Text = DonationPurposeCodes.GetDisplayName(DonationPurposeCodes.GeneralHospitalSupport) },
                new { Value = DonationPurposeCodes.FatherToochukwuSpiritualCare, Text = DonationPurposeCodes.GetDisplayName(DonationPurposeCodes.FatherToochukwuSpiritualCare) }
            },
            "Value",
            "Text");
        var currencies = string.Equals(
                _paymentGateway.ProviderName,
                "Paystack",
                StringComparison.OrdinalIgnoreCase)
            ? new[]
            {
                new { Value = DonationCurrencyCodes.UnitedStatesDollar, Text = "US dollar (USD)" }
            }
            : new[]
            {
                new { Value = DonationCurrencyCodes.CanadianDollar, Text = "Canadian dollar (CAD)" },
                new { Value = DonationCurrencyCodes.UnitedStatesDollar, Text = "US dollar (USD)" },
                new { Value = DonationCurrencyCodes.Euro, Text = "Euro (EUR)" }
            };
        ViewData["DonationCurrencies"] = new SelectList(currencies, "Value", "Text");
    }

    private string GetDefaultDonationCurrency() =>
        string.Equals(
            _paymentGateway.ProviderName,
            "Paystack",
            StringComparison.OrdinalIgnoreCase)
            ? DonationCurrencyCodes.UnitedStatesDollar
            : DonationCurrencyCodes.CanadianDollar;

    private static string NormalizeProviderReference(string? value, string fallback) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length <= 100 &&
        PaymentReferencePattern.IsMatch(value)
            ? value
            : fallback;

    private static string NormalizeProviderText(string? value, string fallback, int maximumLength)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return normalized.Length <= maximumLength ? normalized : normalized[..maximumLength];
    }

    private static bool TryGetSecureCheckoutUrl(
        string? value,
        string provider,
        out string checkoutUrl)
    {
        checkoutUrl = string.Empty;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            uri.Port != 443)
        {
            return false;
        }

        if (string.Equals(provider, "Paystack", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Host, "checkout.paystack.com", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        checkoutUrl = uri.AbsoluteUri;
        return true;
    }
}
