using System.ComponentModel.DataAnnotations;
using Okafor_.NET.Models;

namespace Okafor_.NET.ViewModels;

public sealed class DonationInterestViewModel
{
    [Required, StringLength(150)]
    public string DonorName { get; set; } = string.Empty;

    [EmailAddress, StringLength(150)]
    public string? DonorEmail { get; set; }

    [Phone, StringLength(30)]
    public string? DonorPhone { get; set; }

    [Range(typeof(decimal), "0.01", "999999999.99")]
    public decimal Amount { get; set; }

    [Required, RegularExpression("^[A-Z]{3}$", ErrorMessage = "Enter a valid 3-letter currency code.")]
    public string Currency { get; set; } = "NGN";

    [Required, StringLength(80)]
    public string PurposeCode { get; set; } = DonationPurposeCodes.GeneralHospitalSupport;

    [Required, StringLength(40)]
    public string PreferredMethod { get; set; } = DonationMethodCodes.HospitalContact;

    [StringLength(1000)]
    public string? DonorMessage { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "Please confirm that the hospital may contact you about this donation.")]
    public bool ContactConsent { get; set; }
}
