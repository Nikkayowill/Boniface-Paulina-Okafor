using System.ComponentModel.DataAnnotations;

namespace Okafor_.NET.Models;

public class WhatsAppSchedulingSession
{
    public int Id { get; set; }

    [Required, StringLength(30)]
    public string PatientPhone { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string Status { get; set; } = "Pending";

    [Required]
    public string SlotOptionsJson { get; set; } = "[]";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public int? SelectedOptionNumber { get; set; }

    public int? AppointmentRequestId { get; set; }

    public AppointmentRequest? AppointmentRequest { get; set; }
}
