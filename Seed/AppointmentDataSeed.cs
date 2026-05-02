using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;

namespace Okafor_.NET.Seed;

public static class AppointmentDataSeed
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.AppointmentRequests.AnyAsync())
            return;

        // Fetch IDs seeded by ClinicalDataSeed
        var depts = await context.Departments
            .AsNoTracking()
            .ToDictionaryAsync(d => d.Name, d => d.Id);

        var doctors = await context.Doctors
            .AsNoTracking()
            .ToDictionaryAsync(d => d.FullName, d => d.Id);

        if (!depts.Any() || !doctors.Any())
            return; // Clinical data not yet seeded; skip

        int DeptId(string name)   => depts.TryGetValue(name, out var id) ? id : 0;
        int DoctorId(string name) => doctors.TryGetValue(name, out var id) ? id : 0;

        var appointments = new List<AppointmentRequest>
        {
            new()
            {
                PatientName    = "Emeka Nwosu",
                Email          = "emeka.nwosu@example.com",
                Phone          = "+234 801 234 5678",
                DepartmentId   = DeptId("General Medicine"),
                DoctorId       = DoctorId("Dr. Amara Osei"),
                PreferredDate  = new DateTime(2026, 5, 5),
                PreferredTime  = "10:00 AM",
                Message        = "I have been experiencing persistent fatigue and mild chest discomfort for three weeks.",
                Status         = AppointmentStatus.Pending,
                CreatedAt      = new DateTime(2026, 4, 21, 9, 30, 0, DateTimeKind.Utc)
            },
            new()
            {
                PatientName    = "Adaeze Obi",
                Email          = "adaeze.obi@example.com",
                Phone          = "+234 802 345 6789",
                DepartmentId   = DeptId("Maternity Care"),
                DoctorId       = DoctorId("Dr. Chidinma Eze"),
                PreferredDate  = new DateTime(2026, 4, 28),
                PreferredTime  = "09:00 AM",
                Message        = "First antenatal visit — approximately 10 weeks pregnant.",
                Status         = AppointmentStatus.Approved,
                ContactConfirmed   = true,
                ContactMethod      = "Call",
                ContactNotes       = "Confirmed by phone on 22 Apr. Patient is aware of what to bring.",
                ContactConfirmedAt = new DateTime(2026, 4, 22, 10, 0, 0, DateTimeKind.Utc),
                ApprovedAt         = new DateTime(2026, 4, 22, 10, 5, 0, DateTimeKind.Utc),
                CreatedAt          = new DateTime(2026, 4, 20, 14, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                PatientName    = "Tunde Bakare",
                Email          = "tunde.bakare@example.com",
                Phone          = "+234 803 456 7890",
                DepartmentId   = DeptId("Diagnostics & Laboratory"),
                DoctorId       = DoctorId("Dr. Abena Asante"),
                PreferredDate  = new DateTime(2026, 4, 25),
                PreferredTime  = "08:00 AM",
                Message        = "Referred for fasting blood glucose and lipid panel by my GP.",
                Status         = AppointmentStatus.Approved,
                ContactConfirmed   = true,
                ContactMethod      = "Email",
                ContactNotes       = "Email confirmation sent. Patient confirmed fasting instructions received.",
                ContactConfirmedAt = new DateTime(2026, 4, 21, 8, 0, 0, DateTimeKind.Utc),
                ApprovedAt         = new DateTime(2026, 4, 21, 8, 10, 0, DateTimeKind.Utc),
                CreatedAt          = new DateTime(2026, 4, 19, 11, 30, 0, DateTimeKind.Utc)
            },
            new()
            {
                PatientName    = "Chinyere Okeke",
                Email          = string.Empty,
                Phone          = "+234 804 567 8901",
                DepartmentId   = DeptId("Pediatrics"),
                DoctorId       = DoctorId("Dr. Kofi Mensah"),
                PreferredDate  = new DateTime(2026, 5, 2),
                PreferredTime  = "11:00 AM",
                Message        = "Routine check-up for my 2-year-old son. He is due for his 24-month vaccinations.",
                Status         = AppointmentStatus.Pending,
                CreatedAt      = new DateTime(2026, 4, 22, 15, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                PatientName    = "Ibrahim Musa",
                Email          = "ibrahim.musa@example.com",
                Phone          = "+234 805 678 9012",
                DepartmentId   = DeptId("Surgical Services"),
                DoctorId       = DoctorId("Dr. Samuel Boateng"),
                PreferredDate  = new DateTime(2026, 4, 30),
                PreferredTime  = "02:00 PM",
                Message        = "Follow-up consultation after hernia repair six weeks ago. I have some discomfort.",
                Status         = AppointmentStatus.Rejected,
                ContactConfirmed   = true,
                ContactMethod      = "Call",
                ContactNotes       = "Patient contacted — referred back to initial surgeon at referring hospital as the procedure was performed there.",
                ContactConfirmedAt = new DateTime(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc),
                CreatedAt          = new DateTime(2026, 4, 18, 9, 0, 0, DateTimeKind.Utc)
            },
        };

        context.AppointmentRequests.AddRange(appointments);
        await context.SaveChangesAsync();
    }
}
