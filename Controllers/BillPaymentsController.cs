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
    private readonly IBillPaymentProvider _paymentProvider;
    private readonly IBillPaymentReceiptEmailSender _receiptEmailSender;
    private readonly UserManager<ApplicationUser> _userManager;

    public BillPaymentsController(
        ApplicationDbContext context,
        IBillPaymentProvider paymentProvider,
        IBillPaymentReceiptEmailSender receiptEmailSender,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _paymentProvider = paymentProvider;
        _receiptEmailSender = receiptEmailSender;
        _userManager = userManager;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        return View(new BillPaymentViewModel { Currency = "NGN" });
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(BillPaymentViewModel model)
    {
        model.InvoiceNumber = model.InvoiceNumber.Trim().ToUpperInvariant();
        model.PatientName = model.PatientName.Trim();
        model.PatientEmail = model.PatientEmail.Trim();
        model.PatientPhone = model.PatientPhone.Trim();
        model.Currency = model.Currency.Trim().ToUpperInvariant();

        if (!InvoicePattern.IsMatch(model.InvoiceNumber))
        {
            ModelState.AddModelError(nameof(model.InvoiceNumber), "Use letters, numbers, and hyphens only.");
        }

        if (!CurrencyPattern.IsMatch(model.Currency))
        {
            ModelState.AddModelError(nameof(model.Currency), "Enter a valid 3-letter currency code.");
        }

        if (!model.SandboxAcknowledged)
        {
            ModelState.AddModelError(nameof(model.SandboxAcknowledged), "Please acknowledge this sandbox payment mode.");
        }

        if (await _context.BillPayments.AnyAsync(p => p.InvoiceNumber == model.InvoiceNumber))
        {
            ModelState.AddModelError(nameof(model.InvoiceNumber), "This invoice/reference number has already been paid or recorded.");
        }

        if (!ModelState.IsValid)
        {
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
            Provider = "Mock",
            ApplicationUserId = currentUser?.Id,
            CreatedAt = DateTime.UtcNow
        };

        _context.BillPayments.Add(payment);
        await _context.SaveChangesAsync();

        var result = await _paymentProvider.ProcessAsync(payment);
        payment.ProviderReference = result.ProviderReference;
        payment.Channel = result.Channel;
        payment.ProviderMessage = result.Message;
        payment.IsSandbox = result.IsSandbox;
        payment.Status = result.Success
            ? result.IsSandbox ? BillPaymentStatus.SandboxApproved : BillPaymentStatus.Paid
            : BillPaymentStatus.Failed;
        payment.PaidAt = result.Success ? DateTime.UtcNow : null;

        await _context.SaveChangesAsync();
        await _receiptEmailSender.SendReceiptAsync(payment);

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
}
