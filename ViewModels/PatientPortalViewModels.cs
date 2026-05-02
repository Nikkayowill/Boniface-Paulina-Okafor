using System.ComponentModel.DataAnnotations;

namespace Okafor_.NET.ViewModels;

public class PatientProfileEditViewModel
{
    [Required, StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Phone, StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }
}

public class PatientMessageViewModel
{
    [Required, StringLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required, StringLength(3000)]
    public string Body { get; set; } = string.Empty;
}

/// <summary>
/// Row for the portal "My Teleconsultations" list.
/// </summary>
public class PortalTeleconsultationViewModel
{
    public int Id { get; set; }
    public DateTime PreferredDate { get; set; }
    public string PreferredTime { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string? Doctor { get; set; }
    public string ConsultationType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? MeetingLink { get; set; }
    public string? AdminNotes { get; set; }
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Unified row for the portal "My Appointments" list.
/// Merges public booking requests and admin-created appointments.
/// </summary>
public class PortalAppointmentViewModel
{
    public int SourceId { get; set; }
    public DateTime Date { get; set; }
    public string Department { get; set; } = string.Empty;
    public string? Doctor { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    /// <summary>"Booking Request" or "Scheduled Appointment"</summary>
    public string Source { get; set; } = string.Empty;
    /// <summary>request|scheduled</summary>
    public string SourceType { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
}
