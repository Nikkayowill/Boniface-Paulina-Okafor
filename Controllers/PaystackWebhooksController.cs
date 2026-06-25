using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.Services;

namespace Okafor_.NET.Controllers;

[ApiController]
[Route("webhooks/paystack")]
[IgnoreAntiforgeryToken]
public class PaystackWebhooksController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly PaystackPaymentGateway _paystack;
    private readonly IBillPaymentReceiptEmailSender _billReceiptEmailSender;
    private readonly IDonationReceiptEmailSender _donationReceiptEmailSender;
    private readonly ILogger<PaystackWebhooksController> _logger;

    public PaystackWebhooksController(
        ApplicationDbContext context,
        PaystackPaymentGateway paystack,
        IBillPaymentReceiptEmailSender billReceiptEmailSender,
        IDonationReceiptEmailSender donationReceiptEmailSender,
        ILogger<PaystackWebhooksController> logger)
    {
        _context = context;
        _paystack = paystack;
        _billReceiptEmailSender = billReceiptEmailSender;
        _donationReceiptEmailSender = donationReceiptEmailSender;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["x-paystack-signature"].FirstOrDefault();

        if (!_paystack.IsValidWebhookSignature(body, signature))
        {
            _logger.LogWarning("Rejected Paystack webhook with invalid signature.");
            return Unauthorized();
        }

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var eventName = root.TryGetProperty("event", out var eventElement)
            ? eventElement.GetString()
            : null;

        if (!string.Equals(eventName, "charge.success", StringComparison.OrdinalIgnoreCase))
        {
            return Ok();
        }

        if (!root.TryGetProperty("data", out var data) ||
            !data.TryGetProperty("reference", out var referenceElement))
        {
            return Ok();
        }

        var reference = referenceElement.GetString();
        if (string.IsNullOrWhiteSpace(reference))
        {
            return Ok();
        }

        var verification = await _paystack.VerifyAsync(reference, cancellationToken);
        await ApplyToMatchingRecordAsync(reference, verification, cancellationToken);

        return Ok();
    }

    private async Task ApplyToMatchingRecordAsync(
        string reference,
        PaymentVerificationResult verification,
        CancellationToken cancellationToken)
    {
        var billPayment = await _context.BillPayments
            .FirstOrDefaultAsync(payment => payment.ProviderReference == reference, cancellationToken);

        if (billPayment is not null)
        {
            var wasAlreadyPaid = billPayment.Status is BillPaymentStatus.Paid or BillPaymentStatus.SandboxApproved;
            PaymentVerificationApplicator.ApplyTo(billPayment, verification);
            await _context.SaveChangesAsync(cancellationToken);

            if (!wasAlreadyPaid && billPayment.Status is BillPaymentStatus.Paid or BillPaymentStatus.SandboxApproved)
            {
                await _billReceiptEmailSender.SendReceiptAsync(billPayment, cancellationToken);
            }

            return;
        }

        var donation = await _context.Donations
            .FirstOrDefaultAsync(item => item.PaymentReference == reference || item.ProviderReference == reference, cancellationToken);

        if (donation is null)
        {
            _logger.LogInformation("Paystack webhook reference {Reference} did not match a local record.", reference);
            return;
        }

        var donationWasAlreadyPaid = donation.Status is DonationStatus.Paid or DonationStatus.SandboxApproved;
        PaymentVerificationApplicator.ApplyTo(donation, verification);
        await _context.SaveChangesAsync(cancellationToken);

        if (!donationWasAlreadyPaid && donation.Status is DonationStatus.Paid or DonationStatus.SandboxApproved)
        {
            await _donationReceiptEmailSender.SendReceiptAsync(donation, cancellationToken);
        }
    }
}
