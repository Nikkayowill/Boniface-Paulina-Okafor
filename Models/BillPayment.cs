using System.ComponentModel.DataAnnotations;

namespace Okafor_.NET.Models;

public enum BillPaymentStatus
{
    Pending = 0,
    SandboxApproved = 1,
    Paid = 2,
    Failed = 3,
    Cancelled = 4
}

public class BillPayment
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string PatientName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string PatientEmail { get; set; } = string.Empty;

    [Required, Phone, StringLength(30)]
    public string PatientPhone { get; set; } = string.Empty;

    [Required]
    public decimal Amount { get; set; }

    [Required, StringLength(3)]
    public string Currency { get; set; } = "NGN";

    [Required]
    public BillPaymentStatus Status { get; set; } = BillPaymentStatus.Pending;

    [Required, StringLength(40)]
    public string Provider { get; set; } = "Mock";

    [StringLength(100)]
    public string? ProviderReference { get; set; }

    [StringLength(100)]
    public string? Channel { get; set; }

    [StringLength(1000)]
    public string? ProviderMessage { get; set; }

    public bool IsSandbox { get; set; } = true;

    [StringLength(450)]
    public string? ApplicationUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PaidAt { get; set; }

    public ApplicationUser? ApplicationUser { get; set; }
}
