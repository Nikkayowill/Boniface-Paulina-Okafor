using System.ComponentModel.DataAnnotations;

namespace Okafor_.NET.Models;

public class PatientMessage
{
    public int Id { get; set; }

    [Required]
    public int PatientProfileId { get; set; }

    [Required, StringLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required, StringLength(3000)]
    public string Body { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public PatientProfile? PatientProfile { get; set; }
}
