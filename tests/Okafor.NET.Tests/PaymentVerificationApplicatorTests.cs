using Okafor_.NET.Models;
using Okafor_.NET.Services;

namespace Okafor_.NET.Tests;

public sealed class PaymentVerificationApplicatorTests
{
    [Fact]
    public void ApplyTo_BillPayment_WhenVerifiedLivePayment_MarksPaid()
    {
        var paidAt = new DateTime(2026, 6, 24, 12, 30, 0, DateTimeKind.Utc);
        var payment = new BillPayment
        {
            Amount = 2500m,
            Currency = "NGN",
            Status = BillPaymentStatus.Pending
        };

        PaymentVerificationApplicator.ApplyTo(payment, new PaymentVerificationResult(
            Success: true,
            ProviderReference: "PSK-123",
            Channel: "card",
            Message: "Approved",
            IsSandbox: false,
            PaidAt: paidAt,
            Amount: 2500m,
            Currency: "ngn"));

        Assert.Equal(BillPaymentStatus.Paid, payment.Status);
        Assert.Equal("PSK-123", payment.ProviderReference);
        Assert.Equal("card", payment.Channel);
        Assert.Equal("Approved", payment.ProviderMessage);
        Assert.False(payment.IsSandbox);
        Assert.Equal(paidAt, payment.PaidAt);
    }

    [Fact]
    public void ApplyTo_Donation_WhenVerifiedSandboxPayment_MarksSandboxApproved()
    {
        var donation = new Donation
        {
            Amount = 100m,
            Currency = "NGN",
            Status = DonationStatus.Pending
        };

        PaymentVerificationApplicator.ApplyTo(donation, new PaymentVerificationResult(
            Success: true,
            ProviderReference: "SANDBOX-DON",
            Channel: "Sandbox",
            Message: "Sandbox verified",
            IsSandbox: true,
            Amount: 100m,
            Currency: "NGN"));

        Assert.Equal(DonationStatus.SandboxApproved, donation.Status);
        Assert.Equal("SANDBOX-DON", donation.ProviderReference);
        Assert.Equal("Sandbox", donation.Channel);
        Assert.Equal("Sandbox verified", donation.ProviderMessage);
        Assert.True(donation.IsSandbox);
        Assert.NotNull(donation.PaidAt);
    }

    [Fact]
    public void ApplyTo_WhenAmountDoesNotMatch_MarksFailed()
    {
        var payment = new BillPayment
        {
            Amount = 2500m,
            Currency = "NGN",
            Status = BillPaymentStatus.Pending
        };

        PaymentVerificationApplicator.ApplyTo(payment, new PaymentVerificationResult(
            Success: true,
            ProviderReference: "PSK-123",
            Channel: "card",
            Message: "Approved",
            IsSandbox: false,
            PaidAt: DateTime.UtcNow,
            Amount: 2400m,
            Currency: "NGN"));

        Assert.Equal(BillPaymentStatus.Failed, payment.Status);
        Assert.Null(payment.PaidAt);
    }

    [Fact]
    public void ApplyTo_WhenCurrencyDoesNotMatch_MarksFailed()
    {
        var donation = new Donation
        {
            Amount = 100m,
            Currency = "NGN",
            Status = DonationStatus.Pending
        };

        PaymentVerificationApplicator.ApplyTo(donation, new PaymentVerificationResult(
            Success: true,
            ProviderReference: "PSK-123",
            Channel: "card",
            Message: "Approved",
            IsSandbox: false,
            PaidAt: DateTime.UtcNow,
            Amount: 100m,
            Currency: "USD"));

        Assert.Equal(DonationStatus.Failed, donation.Status);
        Assert.Null(donation.PaidAt);
    }
}
