using Microsoft.AspNetCore.Identity.UI.Services;
using Okafor_.NET.Models;
using System.Text.Encodings.Web;

namespace Okafor_.NET.Services;

public interface IDonationReceiptEmailSender
{
    Task SendReceiptAsync(Donation donation, CancellationToken cancellationToken = default);
}

public sealed class DonationReceiptEmailSender : IDonationReceiptEmailSender
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<DonationReceiptEmailSender> _logger;

    public DonationReceiptEmailSender(IEmailSender emailSender, ILogger<DonationReceiptEmailSender> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task SendReceiptAsync(Donation donation, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(donation.DonorEmail))
        {
            return;
        }

        var subject = donation.IsSandbox
            ? $"Test donation receipt - {donation.PaymentReference}"
            : $"Donation receipt - {donation.PaymentReference}";

        var donorName = HtmlEncoder.Default.Encode(
            string.IsNullOrWhiteSpace(donation.DonorName) ? "Friend" : donation.DonorName);
        var safeCurrency = HtmlEncoder.Default.Encode(donation.Currency);
        var safeStatus = HtmlEncoder.Default.Encode(donation.Status.ToString());
        var safePaymentReference = HtmlEncoder.Default.Encode(donation.PaymentReference);
        var safeProvider = HtmlEncoder.Default.Encode(donation.Provider);
        var body = $"""
            <h2>Boniface &amp; Paulina Okafor Memorial Hospital</h2>
            <p>Dear {donorName},</p>
            <p>Thank you for supporting the hospital. Your donation has been confirmed.</p>
            <table cellpadding="6" cellspacing="0" border="1">
                <tr><td>Amount</td><td>{safeCurrency} {donation.Amount:N2}</td></tr>
                <tr><td>Status</td><td>{safeStatus}</td></tr>
                <tr><td>Reference</td><td>{safePaymentReference}</td></tr>
                <tr><td>Provider</td><td>{safeProvider}</td></tr>
                <tr><td>Mode</td><td>{(donation.IsSandbox ? "Test - no real money collected" : "Production")}</td></tr>
            </table>
            <p>For official tax documentation, please contact the hospital directly.</p>
            """;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _emailSender.SendEmailAsync(donation.DonorEmail, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Donation receipt email could not be sent for donation {DonationId}.", donation.Id);
        }
    }
}
