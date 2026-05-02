using System.ComponentModel.DataAnnotations;
using Okafor_.NET.Models;

namespace Okafor_.NET.ViewModels;

public class AdminAppointmentRequestUpdateViewModel
{
    public int Id { get; set; }

    [Required]
    public AppointmentStatus Status { get; set; }

    public bool ContactConfirmed { get; set; }

    [StringLength(20)]
    public string? ContactMethod { get; set; }

    [StringLength(1000)]
    public string? ContactNotes { get; set; }

    public int? DoctorId { get; set; }

    public string PatientName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public DateTime PreferredDate { get; set; }
    public string PreferredTime { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; }
}
