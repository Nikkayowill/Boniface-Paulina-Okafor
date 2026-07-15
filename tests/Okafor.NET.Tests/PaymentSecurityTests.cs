using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Okafor_.NET.Models;
using Okafor_.NET.Services;

namespace Okafor_.NET.Tests;

public sealed class PaymentSecurityTests
{
    [Fact]
    public void PaystackSignature_WithWrongSignature_IsRejected()
    {
        var gateway = CreatePaystackGateway("sk_test_example");

        var valid = gateway.IsValidWebhookSignature("{\"event\":\"charge.success\"}", "not-the-signature");

        Assert.False(valid);
    }

    [Fact]
    public void PaystackSignature_WithMatchingSignature_IsAccepted()
    {
        const string secret = "sk_test_example";
        const string body = "{\"event\":\"charge.success\"}";
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
        var signature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
        var gateway = CreatePaystackGateway(secret);

        var valid = gateway.IsValidWebhookSignature(body, signature);

        Assert.True(valid);
    }

    [Fact]
    public async Task BillReceipt_EncodesPatientControlledHtml()
    {
        var emailSender = new RecordingEmailSender();
        var service = new BillPaymentReceiptEmailSender(
            emailSender,
            NullLogger<BillPaymentReceiptEmailSender>.Instance);
        var payment = new BillPayment
        {
            Id = 12,
            InvoiceNumber = "INV-1001",
            PatientName = "<img src=x onerror=alert(1)>",
            PatientEmail = "patient@example.com",
            PatientPhone = "08012345678",
            Amount = 5000m,
            Currency = "NGN",
            Status = BillPaymentStatus.Paid,
            Provider = "Paystack",
            ProviderReference = "BILL-12-ABC"
        };

        await service.SendReceiptAsync(payment);

        var message = Assert.Single(emailSender.Messages);
        Assert.DoesNotContain("<img", message.Body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("&lt;img", message.Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DonationReceipt_EncodesDonorControlledHtml()
    {
        var emailSender = new RecordingEmailSender();
        var service = new DonationReceiptEmailSender(
            emailSender,
            NullLogger<DonationReceiptEmailSender>.Instance);
        var donation = new Donation
        {
            DonorName = "<script>alert(1)</script>",
            DonorEmail = "donor@example.com",
            Amount = 1000m,
            Currency = "NGN",
            PaymentReference = "DON-ABC123",
            Status = DonationStatus.Paid,
            Provider = "Paystack"
        };

        await service.SendReceiptAsync(donation);

        var message = Assert.Single(emailSender.Messages);
        Assert.DoesNotContain("<script", message.Body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("&lt;script&gt;", message.Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BillReceipt_WhenEmailDeliveryFails_LogsWarningWithoutBreakingPaymentFlow()
    {
        var logger = new RecordingLogger<BillPaymentReceiptEmailSender>();
        var service = new BillPaymentReceiptEmailSender(new FailingEmailSender(), logger);

        await service.SendReceiptAsync(new BillPayment
        {
            Id = 21,
            InvoiceNumber = "INV-FAILURE",
            PatientName = "Test Patient",
            PatientEmail = "patient@example.test",
            Amount = 5000m,
            Currency = "NGN",
            Status = BillPaymentStatus.SandboxApproved,
            ProviderReference = "SANDBOX-BILL-FAILURE",
            IsSandbox = true
        });

        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Warning && entry.Message.Contains("could not be sent", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DonationReceipt_WhenEmailDeliveryFails_LogsWarningWithoutBreakingPaymentFlow()
    {
        var logger = new RecordingLogger<DonationReceiptEmailSender>();
        var service = new DonationReceiptEmailSender(new FailingEmailSender(), logger);

        await service.SendReceiptAsync(new Donation
        {
            Id = 22,
            DonorEmail = "donor@example.test",
            Amount = 1000m,
            Currency = "NGN",
            PaymentReference = "DON-FAILURE",
            Status = DonationStatus.SandboxApproved,
            Provider = "Mock",
            IsSandbox = true
        });

        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Warning && entry.Message.Contains("could not be sent", StringComparison.Ordinal));
    }

    private static PaystackPaymentGateway CreatePaystackGateway(string secret)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Payments:Paystack:SecretKey"] = secret,
                ["Payments:Paystack:BaseUrl"] = "https://api.paystack.co"
            })
            .Build();

        return new PaystackPaymentGateway(new HttpClient(), configuration);
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public List<(string Email, string Subject, string Body)> Messages { get; } = [];

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            Messages.Add((email, subject, htmlMessage));
            return Task.CompletedTask;
        }
    }

    private sealed class FailingEmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage) =>
            throw new InvalidOperationException("Simulated SMTP failure.");
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }
    }
}
