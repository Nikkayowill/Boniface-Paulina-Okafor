using Microsoft.AspNetCore.Identity.UI.Services;
using Okafor_.NET.Models;
using System.Text.Encodings.Web;

namespace Okafor_.NET.Services;

public interface IBillPaymentReceiptEmailSender
{
    Task SendReceiptAsync(BillPayment payment, CancellationToken cancellationToken = default);
}

public sealed class BillPaymentReceiptEmailSender : IBillPaymentReceiptEmailSender
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<BillPaymentReceiptEmailSender> _logger;

    public BillPaymentReceiptEmailSender(IEmailSender emailSender, ILogger<BillPaymentReceiptEmailSender> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task SendReceiptAsync(BillPayment payment, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(payment.PatientEmail))
        {
            return;
        }

        var safeInvoice = HtmlEncoder.Default.Encode(payment.InvoiceNumber);
        var safePatientName = HtmlEncoder.Default.Encode(payment.PatientName);
        var safeCurrency = HtmlEncoder.Default.Encode(payment.Currency);
        var safeProviderReference = HtmlEncoder.Default.Encode(payment.ProviderReference ?? "Not available");
        var safeStatus = HtmlEncoder.Default.Encode(payment.Status.ToString());
        var subject = payment.IsSandbox
            ? $"Sandbox bill payment receipt - {payment.InvoiceNumber}"
            : $"Bill payment receipt - {payment.InvoiceNumber}";

        var body = $"""
            <h2>Boniface &amp; Paulina Okafor Memorial Hospital</h2>
            <p>Dear {safePatientName},</p>
            <p>Your bill payment has been recorded.</p>
            <table cellpadding="6" cellspacing="0" border="1">
                <tr><td>Invoice</td><td>{safeInvoice}</td></tr>
                <tr><td>Amount</td><td>{safeCurrency} {payment.Amount:N2}</td></tr>
                <tr><td>Status</td><td>{safeStatus}</td></tr>
                <tr><td>Provider reference</td><td>{safeProviderReference}</td></tr>
                <tr><td>Mode</td><td>{(payment.IsSandbox ? "Sandbox - no real money collected" : "Production")}</td></tr>
            </table>
            <p>Thank you.</p>
            """;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _emailSender.SendEmailAsync(payment.PatientEmail, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Bill payment receipt email could not be sent for payment {PaymentId}.", payment.Id);
        }
    }
}
