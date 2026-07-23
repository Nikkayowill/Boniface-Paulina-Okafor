using System.ComponentModel.DataAnnotations;

namespace Okafor_.NET.Models;

public enum DonationStatus
{
    Pending = 0,
    SandboxApproved = 1,
    Paid = 2,
    Failed = 3,
    Cancelled = 4,
    Contacted = 5
}

public static class DonationPurposeCodes
{
    public const string GeneralHospitalSupport = "general-hospital-support";
    public const string FatherToochukwuSpiritualCare = "father-toochukwu-spiritual-care";

    public static bool IsSupported(string? value) => value is
        GeneralHospitalSupport or FatherToochukwuSpiritualCare;

    public static string GetDisplayName(string? value) => value switch
    {
        FatherToochukwuSpiritualCare => "Father Toochukwu's Spiritual Care & Psychotherapy Program",
        _ => "General Hospital Support"
    };
}

public static class DonationMethodCodes
{
    public const string OnlineCheckout = "online-checkout";
    public const string BankTransfer = "bank-transfer";
    public const string HospitalContact = "hospital-contact";
    public const string InPerson = "in-person";

    public static bool IsSupported(string? value) => value is
        OnlineCheckout or BankTransfer or HospitalContact or InPerson;

    public static string GetDisplayName(string? value) => value switch
    {
        OnlineCheckout => "Online checkout",
        BankTransfer => "Bank transfer details",
        InPerson => "Donate in person",
        _ => "Contact me to arrange the donation"
    };
}

public class Donation
{
    public int Id { get; set; }

    [StringLength(150)]
    public string? DonorName { get; set; }

    [EmailAddress, StringLength(150)]
    public string? DonorEmail { get; set; }

    [Phone, StringLength(30)]
    public string? DonorPhone { get; set; }

    [Range(typeof(decimal), "0.01", "999999999.99")]
    public decimal Amount { get; set; }

    [Required, StringLength(10)]
    public string Currency { get; set; } = "NGN";

    [Required, StringLength(80)]
    public string PurposeCode { get; set; } = DonationPurposeCodes.GeneralHospitalSupport;

    [Required, StringLength(40)]
    public string PreferredMethod { get; set; } = DonationMethodCodes.HospitalContact;

    [StringLength(1000)]
    public string? DonorMessage { get; set; }

    public bool ContactConsent { get; set; }

    [Required, StringLength(100)]
    public string PaymentReference { get; set; } = string.Empty;

    [Required]
    public DonationStatus Status { get; set; } = DonationStatus.Pending;

    [Required, StringLength(40)]
    public string Provider { get; set; } = "Manual";

    [StringLength(100)]
    public string? ProviderReference { get; set; }

    [StringLength(100)]
    public string? Channel { get; set; }

    [StringLength(1000)]
    public string? ProviderMessage { get; set; }

    public bool IsSandbox { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PaidAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    [StringLength(450)]
    public string? ReviewedByUserId { get; set; }

    [StringLength(1000)]
    public string? StaffNotes { get; set; }
}
