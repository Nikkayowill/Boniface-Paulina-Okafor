using System.ComponentModel.DataAnnotations;

namespace Okafor_.NET.ViewModels;

public class BookSlotViewModel
{
    [Required]
    public int DoctorId { get; set; }

    [Required]
    public string SlotDate { get; set; } = string.Empty;   // yyyy-MM-dd

    [Required]
    public string SlotTime { get; set; } = string.Empty;   // HH:mm

    [Required, StringLength(150)]
    public string PatientName { get; set; } = string.Empty;

    [Required, Phone, StringLength(30)]
    public string PatientPhone { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string PatientEmail { get; set; } = string.Empty;

    [StringLength(250)]
    public string? ReasonForVisit { get; set; }
}
