using System.ComponentModel.DataAnnotations;
using Okafor_.NET.Models;

namespace Okafor_.NET.ViewModels;

public class AdminTeleconsultationUpdateViewModel
{
    public int Id { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string DepartmentName { get; set; } = string.Empty;

    public string? DoctorName { get; set; }

    public TeleconsultationType ConsultationType { get; set; }

    [Required]
    public TeleconsultationStatus Status { get; set; }

    [Required, DataType(DataType.Date)]
    public DateTime PreferredDate { get; set; }

    [Required, StringLength(20)]
    public string PreferredTime { get; set; } = string.Empty;

    [StringLength(500)]
    public string? MeetingLink { get; set; }

    [StringLength(1000)]
    public string? AdminNotes { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
