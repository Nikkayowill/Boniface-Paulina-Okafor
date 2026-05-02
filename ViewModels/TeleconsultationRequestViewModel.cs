using System.ComponentModel.DataAnnotations;
using Okafor_.NET.Models;

namespace Okafor_.NET.ViewModels;

public class TeleconsultationRequestViewModel
{
    [Required, StringLength(150)]
    public string PatientName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, Phone, StringLength(30)]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a department or specialty.")]
    public int DepartmentId { get; set; }

    public int? DoctorId { get; set; }

    [Required]
    public TeleconsultationType ConsultationType { get; set; } = TeleconsultationType.Video;

    [Required, DataType(DataType.Date)]
    public DateTime PreferredDate { get; set; } = DateTime.Today.AddDays(1);

    [Required, StringLength(20)]
    public string PreferredTime { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string Reason { get; set; } = string.Empty;

    [Range(typeof(bool), "true", "true", ErrorMessage = "Please accept the teleconsultation consent terms.")]
    public bool ConsentAccepted { get; set; }
}
