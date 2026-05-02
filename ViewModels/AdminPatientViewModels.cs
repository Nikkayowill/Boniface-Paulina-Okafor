using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Okafor_.NET.Models;

namespace Okafor_.NET.ViewModels;

public class AdminPatientProfileViewModel
{
    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Phone, StringLength(30)]
    public string? Phone { get; set; }
}

public class AdminUploadDocumentViewModel
{
    [Required]
    public int PatientProfileId { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public IFormFile? File { get; set; }
}

public class AdminPatientAppointmentViewModel
{
    [Required]
    public int PatientProfileId { get; set; }

    [Required]
    public int DepartmentId { get; set; }

    public int? DoctorId { get; set; }

    [Required, DataType(DataType.DateTime)]
    public DateTime AppointmentDate { get; set; } = DateTime.Today.AddDays(1);

    [Required]
    public PatientAppointmentStatus Status { get; set; } = PatientAppointmentStatus.Scheduled;

    [StringLength(1000)]
    public string? Notes { get; set; }
}
