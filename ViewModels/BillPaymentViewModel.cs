using System.ComponentModel.DataAnnotations;

namespace Okafor_.NET.ViewModels;

public class BillPaymentViewModel
{
    [Required, StringLength(100)]
    [Display(Name = "Invoice or Reference Number")]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string PatientName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string PatientEmail { get; set; } = string.Empty;

    [Required, Phone, StringLength(30)]
    public string PatientPhone { get; set; } = string.Empty;

    [Range(1, 999999999)]
    public decimal Amount { get; set; }

    [Required, StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "NGN";

    [Required]
    public bool SandboxAcknowledged { get; set; }
}
