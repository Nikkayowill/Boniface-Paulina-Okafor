namespace Okafor_.NET.Models;

public class AppointmentSlot
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;
    public DateTime SlotDateTime { get; set; }
    public bool IsBooked { get; set; } = false;
    public bool ReminderSent { get; set; } = false;
    public int? AppointmentRequestId { get; set; }
    public AppointmentRequest? AppointmentRequest { get; set; }
}
