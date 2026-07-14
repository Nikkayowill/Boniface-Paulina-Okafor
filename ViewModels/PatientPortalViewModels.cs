using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

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

public class PatientDocumentUploadViewModel
{
    [Required, StringLength(200)]
    [Display(Name = "Document title")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public IFormFile? File { get; set; }
}

public class PushNotificationsViewModel
{
    public string PublicKey { get; set; } = string.Empty;

    public string SaveUrl { get; set; } = "/PushNotifications/SaveSubscription";

    public string UnsubscribeUrl { get; set; } = "/PushNotifications/Unsubscribe";

    public string TestUrl { get; set; } = "/PushNotifications/SendTestNotification";
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
    public bool WhatsAppOptIn { get; set; }
}

/// <summary>
/// Unified row for the portal "My Appointments" list.
/// Merges public booking requests and admin-created appointments.
/// </summary>
public class PortalAppointmentViewModel
{
    public int SourceId { get; set; }
    public int BookingStatusId { get; set; }
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
