using System.ComponentModel.DataAnnotations;

namespace Okafor_.NET.Models;

public class PatientDocument
{
    public int Id { get; set; }

    [Required]
    public int PatientProfileId { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required, StringLength(500)]
    public string FileUrl { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public PatientProfile? PatientProfile { get; set; }
}
