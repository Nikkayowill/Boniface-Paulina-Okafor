using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;

namespace Okafor_.NET.Seed;

public static class AppointmentDataSeed
{
    public static async Task SeedAsync(ApplicationDbContext context, DateTime? referenceUtc = null)
    {
        if (await context.AppointmentRequests.AnyAsync())
            return;

        var nowUtc = (referenceUtc ?? DateTime.UtcNow).ToUniversalTime();
        var today = nowUtc.Date;

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

        var generalMedicineDate = FindNextDate(
            today,
            minimumDaysAhead: 2,
            DayOfWeek.Monday,
            DayOfWeek.Wednesday,
            DayOfWeek.Friday);
        var maternityDate = FindNextDate(
            today,
            minimumDaysAhead: 4,
            DayOfWeek.Monday,
            DayOfWeek.Wednesday,
            DayOfWeek.Friday);
        var diagnosticsDate = FindNextDate(
            today,
            minimumDaysAhead: 1,
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday);
        var pediatricsDate = FindNextDate(
            today,
            minimumDaysAhead: 3,
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday);

        var appointments = new List<AppointmentRequest>
        {
            new()
            {
                PatientName    = "Emeka Nwosu",
                Email          = "emeka.nwosu@example.com",
                Phone          = "+234 801 234 5678",
                DepartmentId   = DeptId("General Medicine"),
                DoctorId       = DoctorId("Dr. Amara Osei"),
                PreferredDate  = generalMedicineDate,
                PreferredTime  = "10:00 AM",
                Message        = "[Staging demo record] Persistent fatigue and mild chest discomfort for three weeks.",
                Status         = AppointmentStatus.Pending,
                ContactNotes   = "Synthetic appointment request for launch rehearsal. Not a real patient.",
                CreatedAt      = today.AddDays(-1).AddHours(9.5)
            },
            new()
            {
                PatientName    = "Adaeze Obi",
                Email          = "adaeze.obi@example.com",
                Phone          = "+234 802 345 6789",
                DepartmentId   = DeptId("Maternity Care"),
                DoctorId       = DoctorId("Dr. Chidinma Eze"),
                PreferredDate  = maternityDate,
                PreferredTime  = "09:00 AM",
                Message        = "[Staging demo record] First antenatal visit — approximately 10 weeks pregnant.",
                Status         = AppointmentStatus.Approved,
                ContactConfirmed   = true,
                ContactMethod      = "Call",
                ContactNotes       = "[Staging demo record] Confirmed by phone. Synthetic record; not a real patient.",
                ContactConfirmedAt = today.AddDays(-1).AddHours(10),
                ApprovedAt         = today.AddDays(-1).AddHours(10).AddMinutes(5),
                CreatedAt          = today.AddDays(-2).AddHours(14)
            },
            new()
            {
                PatientName    = "Tunde Bakare",
                Email          = "tunde.bakare@example.com",
                Phone          = "+234 803 456 7890",
                DepartmentId   = DeptId("Diagnostics & Laboratory"),
                DoctorId       = DoctorId("Dr. Abena Asante"),
                PreferredDate  = diagnosticsDate,
                PreferredTime  = "08:00 AM",
                Message        = "[Staging demo record] Referred for fasting blood glucose and lipid panel.",
                Status         = AppointmentStatus.Approved,
                ContactConfirmed   = true,
                ContactMethod      = "Email",
                ContactNotes       = "[Staging demo record] Confirmation sent. Synthetic record; not a real patient.",
                ContactConfirmedAt = today.AddDays(-1).AddHours(8),
                ApprovedAt         = today.AddDays(-1).AddHours(8).AddMinutes(10),
                CreatedAt          = today.AddDays(-2).AddHours(11).AddMinutes(30)
            },
            new()
            {
                PatientName    = "Chinyere Okeke",
                Email          = string.Empty,
                Phone          = "+234 804 567 8901",
                DepartmentId   = DeptId("Pediatrics"),
                DoctorId       = DoctorId("Dr. Kofi Mensah"),
                PreferredDate  = pediatricsDate,
                PreferredTime  = "11:00 AM",
                Message        = "[Staging demo record] Routine paediatric check-up and vaccination review.",
                Status         = AppointmentStatus.Pending,
                ContactNotes   = "Synthetic appointment request for launch rehearsal. Not a real patient.",
                CreatedAt      = nowUtc.AddMinutes(-30)
            },
            new()
            {
                PatientName    = "Ibrahim Musa",
                Email          = "ibrahim.musa@example.com",
                Phone          = "+234 805 678 9012",
                DepartmentId   = DeptId("Surgical Services"),
                DoctorId       = DoctorId("Dr. Samuel Boateng"),
                PreferredDate  = today.AddDays(-2),
                PreferredTime  = "02:00 PM",
                Message        = "[Staging demo record] Follow-up consultation request after a prior procedure.",
                Status         = AppointmentStatus.Rejected,
                ContactConfirmed   = true,
                ContactMethod      = "Call",
                ContactNotes       = "[Staging demo record] Referred back to the original care team. Synthetic record; not a real patient.",
                ContactConfirmedAt = today.AddDays(-3).AddHours(12),
                CreatedAt          = today.AddDays(-4).AddHours(9)
            },
        };

        context.AppointmentRequests.AddRange(appointments);
        await context.SaveChangesAsync();
    }

    private static DateTime FindNextDate(
        DateTime today,
        int minimumDaysAhead,
        params DayOfWeek[] allowedDays)
    {
        var candidate = today.AddDays(minimumDaysAhead);
        while (!allowedDays.Contains(candidate.DayOfWeek))
            candidate = candidate.AddDays(1);

        return candidate;
    }
}
