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

    public BillPaymentsController(
        ApplicationDbContext context,
        IPaymentGateway paymentGateway,
        IBillPaymentReceiptEmailSender receiptEmailSender,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _paymentGateway = paymentGateway;
        _receiptEmailSender = receiptEmailSender;
        _userManager = userManager;
    }

    [HttpGet("")]
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

        if (await _context.BillPayments.AnyAsync(p => p.InvoiceNumber == model.InvoiceNumber))
        {
            ModelState.AddModelError(nameof(model.InvoiceNumber), "This invoice/reference number has already been paid or recorded.");
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

        var payment = new BillPayment
        {
            InvoiceNumber = model.InvoiceNumber,
            PatientName = model.PatientName,
            PatientEmail = model.PatientEmail,
            PatientPhone = model.PatientPhone,
            Amount = model.Amount,
            Currency = model.Currency,
            Status = BillPaymentStatus.Pending,
            Provider = _paymentGateway.ProviderName,
            ApplicationUserId = currentUser?.Id,
            CreatedAt = DateTime.UtcNow
        };

        _context.BillPayments.Add(payment);
        await _context.SaveChangesAsync();

        var callbackUrl = Url.ActionLink(nameof(Callback), values: null, protocol: Request.Scheme)
            ?? throw new InvalidOperationException("Unable to build payment callback URL.");
        var result = await _paymentGateway.InitializeAsync(new PaymentInitializeRequest(
            Email: payment.PatientEmail,
            Amount: payment.Amount,
            Currency: payment.Currency,
            Reference: $"BILL-{payment.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}",
            CallbackUrl: callbackUrl,
            Purpose: "Bill payment",
            CustomerName: payment.PatientName,
            Metadata: new Dictionary<string, string>
            {
                ["Invoice"] = payment.InvoiceNumber,
                ["Phone"] = payment.PatientPhone
            }));

        payment.ProviderReference = result.ProviderReference;
        payment.Channel = result.Channel;
        payment.ProviderMessage = result.Message;
        payment.IsSandbox = result.IsSandbox;

        if (!result.Success)
        {
            payment.Status = BillPaymentStatus.Failed;
            await _context.SaveChangesAsync();
            ModelState.AddModelError(string.Empty, result.Message);
            ViewData["PaymentProvider"] = _paymentGateway.ProviderName;
            ViewData["IsSandbox"] = _paymentGateway.IsSandbox;
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

        return RedirectToAction(nameof(Receipt), new { id = payment.Id, reference = payment.InvoiceNumber });
    }

    [HttpGet("Callback")]
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
        var verification = await _paymentGateway.VerifyAsync(reference);
        ApplyVerification(payment, verification);
        await _context.SaveChangesAsync();

        if (!wasAlreadyPaid && payment.Status is BillPaymentStatus.Paid or BillPaymentStatus.SandboxApproved)
        {
            await _receiptEmailSender.SendReceiptAsync(payment);
        }

        return RedirectToAction(nameof(Receipt), new { id = payment.Id, reference = payment.InvoiceNumber });
    }

    [HttpGet("Receipt/{id:int}")]
    public async Task<IActionResult> Receipt(int id, string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return NotFound();
        }

        var normalizedReference = reference.Trim().ToUpperInvariant();
        var payment = await _context.BillPayments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && p.InvoiceNumber == normalizedReference);

        if (payment is null)
        {
            return NotFound();
        }

        return View(payment);
    }

    internal static void ApplyVerification(BillPayment payment, PaymentVerificationResult verification)
    {
        payment.ProviderReference = verification.ProviderReference;
        payment.Channel = verification.Channel;
        payment.ProviderMessage = verification.Message;
        payment.IsSandbox = verification.IsSandbox;

        var amountMatches = !verification.Amount.HasValue || verification.Amount.Value == payment.Amount;
        var currencyMatches = string.IsNullOrWhiteSpace(verification.Currency) ||
            string.Equals(verification.Currency, payment.Currency, StringComparison.OrdinalIgnoreCase);

        payment.Status = verification.Success && amountMatches && currencyMatches
            ? verification.IsSandbox ? BillPaymentStatus.SandboxApproved : BillPaymentStatus.Paid
            : BillPaymentStatus.Failed;
        payment.PaidAt = payment.Status is BillPaymentStatus.Paid or BillPaymentStatus.SandboxApproved
            ? verification.PaidAt ?? DateTime.UtcNow
            : null;
    }
}
