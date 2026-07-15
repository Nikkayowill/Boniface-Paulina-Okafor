using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.Services;
using Okafor_.NET.ViewModels;

namespace Okafor_.NET.Controllers;

[Route("BillPayments")]
public class BillPaymentsController : Controller
{
    private static readonly Regex InvoicePattern = new("^[A-Za-z0-9-]{4,100}$", RegexOptions.Compiled);
    private static readonly Regex CurrencyPattern = new("^[A-Z]{3}$", RegexOptions.Compiled);

    private readonly ApplicationDbContext _context;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IBillPaymentReceiptEmailSender _receiptEmailSender;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<BillPaymentsController> _logger;

    public BillPaymentsController(
        ApplicationDbContext context,
        IPaymentGateway paymentGateway,
        IBillPaymentReceiptEmailSender receiptEmailSender,
        UserManager<ApplicationUser> userManager,
        ILogger<BillPaymentsController> logger)
    {
        _context = context;
        _paymentGateway = paymentGateway;
        _receiptEmailSender = receiptEmailSender;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet("")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Index()
    {
        ViewData["PaymentProvider"] = _paymentGateway.ProviderName;
        ViewData["IsSandbox"] = _paymentGateway.IsSandbox;
        return View(new BillPaymentViewModel { Currency = "NGN" });
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(BillPaymentViewModel model)
    {
        model.InvoiceNumber = (model.InvoiceNumber ?? string.Empty).Trim().ToUpperInvariant();
        model.PatientName = (model.PatientName ?? string.Empty).Trim();
        model.PatientEmail = (model.PatientEmail ?? string.Empty).Trim();
        model.PatientPhone = (model.PatientPhone ?? string.Empty).Trim();
        model.Currency = (model.Currency ?? string.Empty).Trim().ToUpperInvariant();

        if (!InvoicePattern.IsMatch(model.InvoiceNumber))
        {
            ModelState.AddModelError(nameof(model.InvoiceNumber), "Use letters, numbers, and hyphens only.");
        }

        if (!CurrencyPattern.IsMatch(model.Currency))
        {
            ModelState.AddModelError(nameof(model.Currency), "Enter a valid 3-letter currency code.");
        }

        if (_paymentGateway.IsSandbox && !model.SandboxAcknowledged)
        {
            ModelState.AddModelError(nameof(model.SandboxAcknowledged), "Please acknowledge this sandbox/test payment mode.");
        }

        var existingPayment = await _context.BillPayments
            .FirstOrDefaultAsync(payment => payment.InvoiceNumber == model.InvoiceNumber);

        if (existingPayment?.Status is BillPaymentStatus.Paid or BillPaymentStatus.SandboxApproved)
        {
            ModelState.AddModelError(nameof(model.InvoiceNumber), "This invoice/reference number has already been paid.");
        }
        else if (existingPayment?.Status == BillPaymentStatus.Pending)
        {
            ModelState.AddModelError(
                nameof(model.InvoiceNumber),
                "A payment for this invoice is already awaiting confirmation. Do not pay again; contact the hospital billing team if you need help.");
        }

        if (!ModelState.IsValid)
        {
            ViewData["PaymentProvider"] = _paymentGateway.ProviderName;
            ViewData["IsSandbox"] = _paymentGateway.IsSandbox;
            return View(model);
        }

        var currentUser = User.Identity?.IsAuthenticated == true
            ? await _userManager.GetUserAsync(User)
            : null;

        var payment = existingPayment ?? new BillPayment { InvoiceNumber = model.InvoiceNumber };
        payment.PatientName = model.PatientName;
        payment.PatientEmail = model.PatientEmail;
        payment.PatientPhone = model.PatientPhone;
        payment.Amount = model.Amount;
        payment.Currency = model.Currency;
        payment.Status = BillPaymentStatus.Pending;
        payment.Provider = _paymentGateway.ProviderName;
        payment.ProviderReference = $"BILL-{Guid.NewGuid():N}".ToUpperInvariant();
        payment.Channel = "Checkout";
        payment.ProviderMessage = "Payment checkout is being initialized.";
        payment.IsSandbox = _paymentGateway.IsSandbox;
        payment.ApplicationUserId = currentUser?.Id;
        payment.CreatedAt = DateTime.UtcNow;
        payment.PaidAt = null;

        if (existingPayment is null)
        {
            _context.BillPayments.Add(payment);
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Bill payment could not be created for invoice {InvoiceNumber}.", model.InvoiceNumber);
            ModelState.AddModelError(nameof(model.InvoiceNumber), "This invoice is already being processed. Please do not submit it again.");
            SetPaymentViewData();
            return View(model);
        }

        var callbackUrl = Url.ActionLink(nameof(Callback), values: null, protocol: Request.Scheme)
            ?? throw new InvalidOperationException("Unable to build payment callback URL.");
        PaymentInitializeResult result;
        try
        {
            result = await _paymentGateway.InitializeAsync(new PaymentInitializeRequest(
                Email: payment.PatientEmail,
                Amount: payment.Amount,
                Currency: payment.Currency,
                Reference: payment.ProviderReference,
                CallbackUrl: callbackUrl,
                Purpose: "Bill payment",
                CustomerName: payment.PatientName,
                Metadata: new Dictionary<string, string>
                {
                    ["Invoice"] = payment.InvoiceNumber,
                    ["Phone"] = payment.PatientPhone
                }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment checkout initialization failed for bill payment {PaymentId}.", payment.Id);
            payment.ProviderMessage = "Payment confirmation is pending because the provider could not be reached.";
            await _context.SaveChangesAsync();
            ModelState.AddModelError(string.Empty, "The payment provider could not be reached. Do not submit another payment for this invoice; contact the hospital billing team if the problem continues.");
            SetPaymentViewData();
            return View(model);
        }

        payment.ProviderReference = result.ProviderReference;
        payment.Channel = result.Channel;
        payment.ProviderMessage = result.Message;
        payment.IsSandbox = result.IsSandbox;

        if (!result.Success)
        {
            payment.Status = BillPaymentStatus.Failed;
            await _context.SaveChangesAsync();
            ModelState.AddModelError(string.Empty, result.Message);
            SetPaymentViewData();
            return View(model);
        }

        if (result.RequiresRedirect && !string.IsNullOrWhiteSpace(result.AuthorizationUrl))
        {
            await _context.SaveChangesAsync();
            return Redirect(result.AuthorizationUrl);
        }

        payment.Status = result.IsSandbox ? BillPaymentStatus.SandboxApproved : BillPaymentStatus.Paid;
        payment.PaidAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _receiptEmailSender.SendReceiptAsync(payment);

        return RedirectToAction(nameof(Receipt), new { id = payment.Id, reference = payment.ProviderReference });
    }

    [HttpGet("Callback")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Callback(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return RedirectToAction(nameof(Index));
        }

        var payment = await _context.BillPayments
            .FirstOrDefaultAsync(p => p.ProviderReference == reference);

        if (payment is null)
        {
            return NotFound();
        }

        var wasAlreadyPaid = payment.Status is BillPaymentStatus.Paid or BillPaymentStatus.SandboxApproved;
        PaymentVerificationResult verification;
        try
        {
            verification = await _paymentGateway.VerifyAsync(reference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment verification failed for bill payment {PaymentId}.", payment.Id);
            payment.ProviderMessage = "Payment verification is temporarily unavailable.";
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Receipt), new { id = payment.Id, reference = payment.ProviderReference });
        }

        ApplyVerification(payment, verification);
        await _context.SaveChangesAsync();

        if (!wasAlreadyPaid && payment.Status is BillPaymentStatus.Paid or BillPaymentStatus.SandboxApproved)
        {
            await _receiptEmailSender.SendReceiptAsync(payment);
        }

        return RedirectToAction(nameof(Receipt), new { id = payment.Id, reference = payment.ProviderReference });
    }

    [HttpGet("Receipt/{id:int}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Receipt(int id, string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return NotFound();
        }

        var normalizedReference = reference.Trim();
        var payment = await _context.BillPayments
            .AsNoTracking()
            .FirstOrDefaultAsync(p =>
                p.Id == id &&
                p.ProviderReference != null &&
                p.ProviderReference == normalizedReference);

        if (payment is null)
        {
            return NotFound();
        }

        return View(payment);
    }

    internal static void ApplyVerification(BillPayment payment, PaymentVerificationResult verification)
    {
        PaymentVerificationApplicator.ApplyTo(payment, verification);
    }

    private void SetPaymentViewData()
    {
        ViewData["PaymentProvider"] = _paymentGateway.ProviderName;
        ViewData["IsSandbox"] = _paymentGateway.IsSandbox;
    }
}
