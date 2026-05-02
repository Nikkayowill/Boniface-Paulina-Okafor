using System.ComponentModel.DataAnnotations;

namespace Okafor_.NET.Models;

public enum AppointmentStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public class AppointmentRequest
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string PatientName { get; set; } = string.Empty;

    [EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, Phone, StringLength(30)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public int DepartmentId { get; set; }

    [Required(ErrorMessage = "Please choose a doctor.")]
    public int? DoctorId { get; set; }

    [Required, DataType(DataType.Date)]
    public DateTime PreferredDate { get; set; }

    [Required, StringLength(20)]
    public string PreferredTime { get; set; } = string.Empty;

    [StringLength(3000)]
    public string? Message { get; set; }

    [Required]
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

    public bool ContactConfirmed { get; set; } = false;

    [StringLength(20)]
    public string? ContactMethod { get; set; }

    public DateTime? ContactConfirmedAt { get; set; }

    [StringLength(1000)]
    public string? ContactNotes { get; set; }

    public DateTime? ApprovedAt { get; set; }

    [StringLength(450)]
    public string? ApprovedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Department? Department { get; set; }
    public Doctor? Doctor { get; set; }
}
