namespace Okafor_.NET.Models;

public class NotificationLog
{
    public int Id { get; set; }
    public string Channel { get; set; } = string.Empty;       // "Email" | "WhatsApp" | "SMS"
    public string Recipient { get; set; } = string.Empty;     // email address or phone number
    public string MessageBody { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public int? AppointmentRequestId { get; set; }
}
