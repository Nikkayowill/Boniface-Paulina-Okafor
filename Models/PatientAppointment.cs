using System.ComponentModel.DataAnnotations;

namespace Okafor_.NET.Models;

public enum PatientAppointmentStatus
{
    Scheduled = 0,
    Confirmed = 1,
    Completed = 2,
    Cancelled = 3
}

public class PatientAppointment
{
    public int Id { get; set; }

    public int? AppointmentRequestId { get; set; }

    [Required]
    public int PatientProfileId { get; set; }

    [Required]
    public int DepartmentId { get; set; }

    public int? DoctorId { get; set; }

    [Required, DataType(DataType.DateTime)]
    public DateTime AppointmentDate { get; set; }

    [Required]
    public PatientAppointmentStatus Status { get; set; } = PatientAppointmentStatus.Scheduled;

    [StringLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public PatientProfile? PatientProfile { get; set; }
    public Department? Department { get; set; }
    public Doctor? Doctor { get; set; }
    public AppointmentRequest? AppointmentRequest { get; set; }
}
