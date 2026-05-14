using System.ComponentModel.DataAnnotations;

namespace Okafor_.NET.Models;

public enum DonationStatus
{
    Pending = 0,
    SandboxApproved = 1,
    Paid = 2,
    Failed = 3,
    Cancelled = 4
}

public class Donation
{
    public int Id { get; set; }

    [StringLength(150)]
    public string? DonorName { get; set; }

    [EmailAddress, StringLength(150)]
    public string? DonorEmail { get; set; }

    [Range(typeof(decimal), "0.01", "999999999.99")]
    public decimal Amount { get; set; }

    [Required, StringLength(10)]
    public string Currency { get; set; } = "NGN";

    [Required, StringLength(100)]
    public string PaymentReference { get; set; } = string.Empty;

    [Required]
    public DonationStatus Status { get; set; } = DonationStatus.Pending;

    [Required, StringLength(40)]
    public string Provider { get; set; } = "Mock";

    [StringLength(100)]
    public string? ProviderReference { get; set; }

    [StringLength(100)]
    public string? Channel { get; set; }

    [StringLength(1000)]
    public string? ProviderMessage { get; set; }

    public bool IsSandbox { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PaidAt { get; set; }
}
