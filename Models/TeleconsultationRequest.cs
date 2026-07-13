using System.ComponentModel.DataAnnotations;

namespace Okafor_.NET.Models;

public enum TeleconsultationStatus
{
    Pending = 0,
    Confirmed = 1,
    Rescheduled = 2,
    Completed = 3,
    Rejected = 4,
    Cancelled = 5
}

public enum TeleconsultationType
{
    Video = 0,
    Phone = 1,
    FollowUp = 2
}

public class TeleconsultationRequest
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string PatientName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, Phone, StringLength(30)]
    public string Phone { get; set; } = string.Empty;

    public bool WhatsAppOptIn { get; set; }

    [Required]
    public int DepartmentId { get; set; }

    public int? DoctorId { get; set; }

    [Required]
    public TeleconsultationType ConsultationType { get; set; } = TeleconsultationType.Video;

    [Required, DataType(DataType.Date)]
    public DateTime PreferredDate { get; set; }

    [Required, StringLength(20)]
    public string PreferredTime { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string Reason { get; set; } = string.Empty;

    [Required]
    public bool ConsentAccepted { get; set; }

    [Required]
    public TeleconsultationStatus Status { get; set; } = TeleconsultationStatus.Pending;

    [StringLength(500)]
    public string? MeetingLink { get; set; }

    [StringLength(1000)]
    public string? AdminNotes { get; set; }

    [StringLength(450)]
    public string? ApplicationUserId { get; set; }

    public int? PatientProfileId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public Department? Department { get; set; }

    public Doctor? Doctor { get; set; }

    public ApplicationUser? ApplicationUser { get; set; }

    public PatientProfile? PatientProfile { get; set; }
}
