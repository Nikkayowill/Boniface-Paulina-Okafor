using Okafor_.NET.Models;

namespace Okafor_.NET.Services;

public static class PaymentVerificationApplicator
{
    public static void ApplyTo(BillPayment payment, PaymentVerificationResult verification)
    {
        var expectedReference = payment.ProviderReference;
        payment.Channel = Limit(verification.Channel, 100);
        payment.ProviderMessage = Limit(verification.Message, 1000);
        payment.IsSandbox = verification.IsSandbox;

        payment.Status = IsVerified(payment.Amount, payment.Currency, expectedReference, verification)
            ? verification.IsSandbox ? BillPaymentStatus.SandboxApproved : BillPaymentStatus.Paid
            : BillPaymentStatus.Failed;
        payment.PaidAt = payment.Status is BillPaymentStatus.Paid or BillPaymentStatus.SandboxApproved
            ? verification.PaidAt ?? DateTime.UtcNow
            : null;
    }

    public static void ApplyTo(Donation donation, PaymentVerificationResult verification)
    {
        var expectedReference = donation.ProviderReference ?? donation.PaymentReference;
        donation.Channel = Limit(verification.Channel, 100);
        donation.ProviderMessage = Limit(verification.Message, 1000);
        donation.IsSandbox = verification.IsSandbox;

        donation.Status = IsVerified(donation.Amount, donation.Currency, expectedReference, verification)
            ? verification.IsSandbox ? DonationStatus.SandboxApproved : DonationStatus.Paid
            : DonationStatus.Failed;
        donation.PaidAt = donation.Status is DonationStatus.Paid or DonationStatus.SandboxApproved
            ? verification.PaidAt ?? DateTime.UtcNow
            : null;
    }

    private static bool IsVerified(
        decimal expectedAmount,
        string expectedCurrency,
        string? expectedReference,
        PaymentVerificationResult verification)
    {
        var referenceMatches = !string.IsNullOrWhiteSpace(expectedReference) &&
            string.Equals(expectedReference, verification.ProviderReference, StringComparison.Ordinal);
        var amountMatches = verification.IsSandbox
            ? !verification.Amount.HasValue || verification.Amount.Value == expectedAmount
            : verification.Amount.HasValue && verification.Amount.Value == expectedAmount;
        var currencyMatches = verification.IsSandbox
            ? string.IsNullOrWhiteSpace(verification.Currency) ||
                string.Equals(verification.Currency, expectedCurrency, StringComparison.OrdinalIgnoreCase)
            : !string.IsNullOrWhiteSpace(verification.Currency) &&
                string.Equals(verification.Currency, expectedCurrency, StringComparison.OrdinalIgnoreCase);

        return verification.Success && referenceMatches && amountMatches && currencyMatches;
    }

    private static string Limit(string? value, int maximumLength)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "No provider message was supplied." : value.Trim();
        return normalized.Length <= maximumLength ? normalized : normalized[..maximumLength];
    }
}
