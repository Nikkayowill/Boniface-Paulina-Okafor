using System.Net;
using System.Net.Mail;
using Okafor_.NET.Models;

namespace Okafor_.NET.Services;

public interface IDonationReceiptEmailSender
{
    Task SendReceiptAsync(Donation donation, CancellationToken cancellationToken = default);
}

public sealed class DonationReceiptEmailSender : IDonationReceiptEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DonationReceiptEmailSender> _logger;

    public DonationReceiptEmailSender(IConfiguration configuration, ILogger<DonationReceiptEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendReceiptAsync(Donation donation, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(donation.DonorEmail))
        {
            return;
        }

        var section = _configuration.GetSection("DonationReceiptEmail");
        var smtpHost = section["SmtpHost"];
        var fromAddress = section["FromAddress"];

        if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(fromAddress))
        {
            _logger.LogInformation("Donation receipt email skipped because SMTP is not configured.");
            return;
        }

        try
        {
            using var client = new SmtpClient(smtpHost, section.GetValue<int?>("Port") ?? 25)
            {
                EnableSsl = section.GetValue<bool?>("EnableSsl") ?? false
            };

            var username = section["Username"];
            var password = section["Password"];
            if (!string.IsNullOrWhiteSpace(username))
            {
                client.Credentials = new NetworkCredential(username, password ?? string.Empty);
            }

            using var message = new MailMessage(fromAddress, donation.DonorEmail)
            {
                Subject = $"Donation Receipt #{donation.PaymentReference}",
                Body = $"Thank you for supporting Boniface & Paulina Okafor Memorial Hospital.{Environment.NewLine}{Environment.NewLine}" +
                       $"Amount: {donation.Currency} {donation.Amount:N2}{Environment.NewLine}" +
                       $"Date: {donation.CreatedAt:dd MMM yyyy}{Environment.NewLine}" +
                       $"Receipt/Reference Number: {donation.PaymentReference}{Environment.NewLine}{Environment.NewLine}" +
                       "For official tax documentation, please contact the hospital directly."
            };

            cancellationToken.ThrowIfCancellationRequested();
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Donation receipt email could not be sent for donation {DonationId}.", donation.Id);
        }
    }
}