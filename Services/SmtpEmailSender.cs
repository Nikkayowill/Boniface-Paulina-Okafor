using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Okafor_.NET.Services;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogInformation("Email skipped because recipient was empty. Subject: {Subject}", subject);
            return;
        }

        var section = _configuration.GetSection("Email");
        var smtpHost = section["SmtpHost"];
        var fromAddress = section["FromAddress"];

        if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(fromAddress))
        {
            _logger.LogInformation("Email skipped because SMTP is not configured. Recipient: {Recipient}; Subject: {Subject}", email, subject);
            return;
        }

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

        using var message = new MailMessage(fromAddress, email)
        {
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true
        };

        try
        {
            await client.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email send failed.");
        }
    }
}
