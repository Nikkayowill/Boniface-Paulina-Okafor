using System.ComponentModel.DataAnnotations;
using Okafor_.NET.Models;

namespace Okafor_.NET.ViewModels;

public sealed class DonationCheckoutViewModel
{
    [Required, StringLength(150)]
    public string DonorName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string DonorEmail { get; set; } = string.Empty;

    [Phone, StringLength(30)]
    public string? DonorPhone { get; set; }

    [Range(typeof(decimal), "0.01", "999999999.99")]
    public decimal Amount { get; set; }

    [Required, RegularExpression("^NGN$", ErrorMessage = "Online donations are currently processed in NGN.")]
    public string Currency { get; set; } = "NGN";

    [Required, StringLength(80)]
    public string PurposeCode { get; set; } = DonationPurposeCodes.GeneralHospitalSupport;

    [StringLength(1000)]
    public string? DonorMessage { get; set; }
}
