namespace Okafor_.NET.Services;

public interface INotificationService
{
    Task<bool> SendConfirmationAsync(NotificationRequest request);
    Task<bool> SendAdminAlertAsync(NotificationRequest request);
    Task<bool> SendReminderAsync(NotificationRequest request);
    Task<bool> SendTeleconsultationReceivedAsync(NotificationRequest request);
    Task<bool> SendTeleconsultationStatusAsync(NotificationRequest request, string status, string nextStep);
}

public class NotificationRequest
{
    public string PatientName { get; set; } = string.Empty;
    public string PatientEmail { get; set; } = string.Empty;
    public string PatientPhone { get; set; } = string.Empty;   // +234XXXXXXXXXX
    public string DoctorName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public DateTime AppointmentDateTime { get; set; }
    public string ConfirmationRef { get; set; } = string.Empty;
    public int? AppointmentRequestId { get; set; }
    public int? TeleconsultationRequestId { get; set; }
}
