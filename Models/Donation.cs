using System.ComponentModel.DataAnnotations;

namespace Okafor_.NET.Models;

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

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}