using Okafor_.NET.Models;

namespace Okafor_.NET.Services;

public static class PaymentVerificationApplicator
{
    public static void ApplyTo(BillPayment payment, PaymentVerificationResult verification)
    {
        payment.ProviderReference = verification.ProviderReference;
        payment.Channel = verification.Channel;
        payment.ProviderMessage = verification.Message;
        payment.IsSandbox = verification.IsSandbox;

        payment.Status = IsVerified(payment.Amount, payment.Currency, verification)
            ? verification.IsSandbox ? BillPaymentStatus.SandboxApproved : BillPaymentStatus.Paid
            : BillPaymentStatus.Failed;
        payment.PaidAt = payment.Status is BillPaymentStatus.Paid or BillPaymentStatus.SandboxApproved
            ? verification.PaidAt ?? DateTime.UtcNow
            : null;
    }

    public static void ApplyTo(Donation donation, PaymentVerificationResult verification)
    {
        donation.ProviderReference = verification.ProviderReference;
        donation.Channel = verification.Channel;
        donation.ProviderMessage = verification.Message;
        donation.IsSandbox = verification.IsSandbox;

        donation.Status = IsVerified(donation.Amount, donation.Currency, verification)
            ? verification.IsSandbox ? DonationStatus.SandboxApproved : DonationStatus.Paid
            : DonationStatus.Failed;
        donation.PaidAt = donation.Status is DonationStatus.Paid or DonationStatus.SandboxApproved
            ? verification.PaidAt ?? DateTime.UtcNow
            : null;
    }

    private static bool IsVerified(decimal expectedAmount, string expectedCurrency, PaymentVerificationResult verification)
    {
        var amountMatches = !verification.Amount.HasValue || verification.Amount.Value == expectedAmount;
        var currencyMatches = string.IsNullOrWhiteSpace(verification.Currency) ||
            string.Equals(verification.Currency, expectedCurrency, StringComparison.OrdinalIgnoreCase);

        return verification.Success && amountMatches && currencyMatches;
    }
}
