using System.ComponentModel.DataAnnotations;

namespace Okafor_.NET.Models;

public class Doctor
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(180)]
    public string? Slug { get; set; }

    [Required, StringLength(150)]
    public string Specialty { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Bio { get; set; }

    [StringLength(1000)]
    public string? Qualifications { get; set; }

    [StringLength(200)]
    public string? ConsultationHours { get; set; }

    [StringLength(500)]
    public string? ImageUrl { get; set; }

    [Required]
    public int DepartmentId { get; set; }

    public Department? Department { get; set; }
}
