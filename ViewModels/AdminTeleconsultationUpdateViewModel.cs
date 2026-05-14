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

public class AdminTeleconsultationDetailsViewModel
{
    public TeleconsultationRequest Request { get; set; } = new();

    public IReadOnlyList<NotificationTimelineItemViewModel> Notifications { get; set; } = [];
}

public class NotificationTimelineItemViewModel
{
    public string Channel { get; set; } = string.Empty;

    public string Recipient { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public string? DeliveryStatus { get; set; }

    public DateTime SentAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? ReadAt { get; set; }
}
