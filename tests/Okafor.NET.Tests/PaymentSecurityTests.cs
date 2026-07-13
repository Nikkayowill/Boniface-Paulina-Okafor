using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Okafor_.NET.Controllers;
using Okafor_.NET.Data;
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
    public async Task BillReceipt_RequiresGeneratedProviderReference_NotInvoiceNumber()
    {
        await using var context = CreateContext();
        var payment = new BillPayment
        {
            InvoiceNumber = "INV-1001",
            PatientName = "Test Patient",
            PatientEmail = "patient@example.com",
            PatientPhone = "08012345678",
            Amount = 5000m,
            Currency = "NGN",
            Status = BillPaymentStatus.SandboxApproved,
            Provider = "Mock",
            ProviderReference = "SANDBOX-BILL-ABC123",
            IsSandbox = true
        };
        context.BillPayments.Add(payment);
        await context.SaveChangesAsync();
        var controller = new BillPaymentsController(context, null!, null!, null!);

        var invoiceResult = await controller.Receipt(payment.Id, payment.InvoiceNumber);
        var providerResult = await controller.Receipt(payment.Id, payment.ProviderReference);

        Assert.IsType<NotFoundResult>(invoiceResult);
        var view = Assert.IsType<ViewResult>(providerResult);
        var receipt = Assert.IsType<BillPayment>(view.Model);
        Assert.Equal(payment.Id, receipt.Id);
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

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
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
}
