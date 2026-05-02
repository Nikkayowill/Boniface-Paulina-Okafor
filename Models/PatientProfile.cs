using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Okafor_.NET.Models;

public class PatientProfile
{
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Phone, StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ApplicationUser? ApplicationUser { get; set; }
    public ICollection<PatientDocument> Documents { get; set; } = new List<PatientDocument>();
    public ICollection<PatientAppointment> Appointments { get; set; } = new List<PatientAppointment>();
    public ICollection<PatientMessage> Messages { get; set; } = new List<PatientMessage>();
}
