using System.ComponentModel.DataAnnotations;
using Okafor_.NET.Models;

namespace Okafor_.NET.ViewModels;

public class TeleconsultationRequestViewModel
{
    [Required, StringLength(150)]
    public string PatientName { get; set; } = string.Empty;

    [EmailAddress, StringLength(150)]
    public string? Email { get; set; }

    [Required, Phone, StringLength(30)]
    public string Phone { get; set; } = string.Empty;

    [Required, StringLength(6)]
    [RegularExpression(@"^\+\d{1,5}$", ErrorMessage = "Please choose a valid country code.")]
    public string PhoneCountryCode { get; set; } = "+234";

    [Required(ErrorMessage = "Please choose a department or specialty.")]
    public int DepartmentId { get; set; }

    public int? DoctorId { get; set; }

    [Required, DataType(DataType.Date)]
    public DateTime PreferredDate { get; set; } = DateTime.Today.AddDays(1);

    [Required, StringLength(20)]
    public string PreferredTime { get; set; } = string.Empty;
}
