using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;

namespace Okafor_.NET.Seed;

public static class ClinicalDataSeed
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await SeedDepartmentsAsync(context);
        await SeedDoctorsAsync(context);
        await SeedDoctorAvailabilityAsync(context);
    }

    // ── Departments ────────────────────────────────────────────────────────

    private static async Task SeedDepartmentsAsync(ApplicationDbContext context)
    {
        if (await context.Departments.AnyAsync())
            return;

        var departments = new List<Department>
        {
            new() { Name = "General Medicine",          Description = "Comprehensive primary and general adult medical care." },
            new() { Name = "Pediatrics",                Description = "Healthcare for infants, children, and adolescents." },
            new() { Name = "Diagnostics & Laboratory",  Description = "Blood work, imaging, and diagnostic testing services." },
            new() { Name = "Surgical Services",         Description = "Elective and emergency surgical procedures." },
            new() { Name = "Emergency Care",            Description = "24/7 urgent and emergency medical treatment." },
            new() { Name = "Maternity Care",            Description = "Prenatal, delivery, and postnatal care for mothers and newborns." },
        };

        context.Departments.AddRange(departments);
        await context.SaveChangesAsync();
    }

    // ── Doctors ────────────────────────────────────────────────────────────

    private static async Task SeedDoctorsAsync(ApplicationDbContext context)
    {
        if (await context.Doctors.AnyAsync())
            return;

        // Fetch department IDs by name (safe — departments were just seeded above)
        var depts = await context.Departments
            .AsNoTracking()
            .ToDictionaryAsync(d => d.Name, d => d.Id);

        int Dept(string name) => depts.TryGetValue(name, out var id) ? id : throw new InvalidOperationException($"Department '{name}' not found.");

        var doctors = new List<Doctor>
        {
            new()
            {
                FullName          = "Dr. Amara Osei",
                Slug              = "dr-amara-osei",
                Specialty         = "General Practitioner",
                Qualifications    = "MBChB (University of Ghana), MRCGP, Diploma in Tropical Medicine",
                ConsultationHours = "Mon, Wed, Fri — 9:00 AM to 3:00 PM",
                Bio               = "Dr. Osei has over 15 years of experience in general adult medicine, focusing on preventive care, chronic disease management, and health education for underserved communities.",
                ImageUrl          = "/images/placeholders/nigerian-doctor-male.webp",
                DepartmentId      = Dept("General Medicine")
            },
            new()
            {
                FullName          = "Dr. Fatima Yusuf",
                Slug              = "dr-fatima-yusuf",
                Specialty         = "Internal Medicine",
                Qualifications    = "MBBS (Ahmadu Bello University), FWACP (Internal Medicine)",
                ConsultationHours = "Tue, Thu — 10:00 AM to 2:00 PM",
                Bio               = "Dr. Yusuf specialises in complex multi-system conditions and long-term patient care plans, with particular experience in hypertension and diabetes management.",
                ImageUrl          = "/images/placeholders/nigerian-doctor-female.webp",
                DepartmentId      = Dept("General Medicine")
            },
            new()
            {
                FullName          = "Dr. Kofi Mensah",
                Slug              = "dr-kofi-mensah",
                Specialty         = "Paediatrician",
                Qualifications    = "MBChB, FGCP (Paediatrics), Diploma in Child Health",
                ConsultationHours = "Mon to Fri — 8:00 AM to 12:00 PM",
                Bio               = "Dr. Mensah is dedicated to child health from newborn assessments through to adolescent medicine, with special interest in childhood nutrition and immunisation programmes.",
                ImageUrl          = "/images/placeholders/nigerian-doctor-male.webp",
                DepartmentId      = Dept("Pediatrics")
            },
            new()
            {
                FullName          = "Dr. Ngozi Adeyemi",
                Slug              = "dr-ngozi-adeyemi",
                Specialty         = "Neonatologist",
                Qualifications    = "MBBS (University of Lagos), FMCPaed, Fellowship in Neonatal-Perinatal Medicine",
                ConsultationHours = "Mon, Wed, Fri — 8:00 AM to 1:00 PM",
                Bio               = "Dr. Adeyemi focuses on the care of premature and critically ill newborns. She has extensive experience managing complex neonatal cases in regional hospital settings.",
                ImageUrl          = "/images/placeholders/nigerian-doctor-female.webp",
                DepartmentId      = Dept("Pediatrics")
            },
            new()
            {
                FullName          = "Dr. Samuel Boateng",
                Slug              = "dr-samuel-boateng",
                Specialty         = "General Surgeon",
                Qualifications    = "MBChB, FGCS (General Surgery), Postgraduate Diploma in Surgical Oncology",
                ConsultationHours = "Tue, Thu, Sat — 9:00 AM to 2:00 PM",
                Bio               = "Dr. Boateng performs a wide range of elective and emergency surgical procedures, with a focus on minimal recovery time and patient education through the surgical journey.",
                ImageUrl          = "/images/placeholders/nigerian-doctor-male.webp",
                DepartmentId      = Dept("Surgical Services")
            },
            new()
            {
                FullName          = "Dr. Chidinma Eze",
                Slug              = "dr-chidinma-eze",
                Specialty         = "Obstetrician & Gynaecologist",
                Qualifications    = "MBBS (University of Nigeria), FMCOG, Certificate in Maternal-Fetal Medicine",
                ConsultationHours = "Mon, Wed, Fri — 9:00 AM to 4:00 PM",
                Bio               = "Dr. Eze provides comprehensive maternal care, from antenatal check-ups through to post-delivery support. She has delivered over 2,000 babies and is a passionate advocate for maternal health equity.",
                ImageUrl          = "/images/placeholders/nigerian-doctor-female.webp",
                DepartmentId      = Dept("Maternity Care")
            },
            new()
            {
                FullName          = "Dr. Emmanuel Owusu",
                Slug              = "dr-emmanuel-owusu",
                Specialty         = "Emergency Medicine",
                Qualifications    = "MBChB, Diploma in Emergency Medicine (Ghana College), Advanced Trauma Life Support (ATLS)",
                ConsultationHours = "Rotating shifts — 24/7 Emergency Coverage",
                Bio               = "Dr. Owusu leads the emergency team with expertise in trauma, resuscitation, and acute care. He has trained emergency response teams across the region.",
                ImageUrl          = "/images/placeholders/nigerian-doctor-male.webp",
                DepartmentId      = Dept("Emergency Care")
            },
            new()
            {
                FullName          = "Dr. Abena Asante",
                Slug              = "dr-abena-asante",
                Specialty         = "Clinical Pathologist",
                Qualifications    = "MBChB, FGCPath (Clinical Pathology), MSc Medical Biochemistry",
                ConsultationHours = "Mon to Fri — 8:00 AM to 3:00 PM",
                Bio               = "Dr. Asante oversees laboratory diagnostics and ensures accurate, timely test results for clinical decision-making. She has introduced several quality improvement protocols in the diagnostics department.",
                ImageUrl          = "/images/placeholders/nigerian-doctor-female.webp",
                DepartmentId      = Dept("Diagnostics & Laboratory")
            },
        };

        context.Doctors.AddRange(doctors);
        await context.SaveChangesAsync();
    }

    // ── Doctor Availabilities ──────────────────────────────────────────────

    private static async Task SeedDoctorAvailabilityAsync(ApplicationDbContext context)
    {
        if (await context.DoctorAvailabilities.AnyAsync())
            return;

        // Fetch doctor IDs by name
        var doctors = await context.Doctors
            .AsNoTracking()
            .ToDictionaryAsync(d => d.FullName, d => d.Id);

        int DoctorId(string name) => doctors.TryGetValue(name, out var id) ? id : throw new InvalidOperationException($"Doctor '{name}' not found.");

        var availabilities = new List<DoctorAvailability>();

        // Standard time slots
        var morningStart = new TimeSpan(9, 0, 0);
        var afternoonEnd = new TimeSpan(17, 0, 0);
        var earlyEnd = new TimeSpan(15, 0, 0);
        var noonEnd = new TimeSpan(12, 0, 0);
        var lateStart = new TimeSpan(10, 0, 0);

        // Dr. Amara Osei — Mon, Wed, Fri — 9:00 AM to 3:00 PM
        foreach (var day in new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday })
            availabilities.Add(new()
            {
                DoctorId = DoctorId("Dr. Amara Osei"),
                DayOfWeek = day,
                StartTime = morningStart,
                EndTime = new TimeSpan(15, 0, 0),
                SlotDurationMinutes = 30,
                IsActive = true
            });

        // Dr. Fatima Yusuf — Tue, Thu — 10:00 AM to 2:00 PM
        foreach (var day in new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday })
            availabilities.Add(new()
            {
                DoctorId = DoctorId("Dr. Fatima Yusuf"),
                DayOfWeek = day,
                StartTime = lateStart,
                EndTime = new TimeSpan(14, 0, 0),
                SlotDurationMinutes = 30,
                IsActive = true
            });

        // Dr. Kofi Mensah — Mon to Fri — 8:00 AM to 12:00 PM
        foreach (var day in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday })
            availabilities.Add(new()
            {
                DoctorId = DoctorId("Dr. Kofi Mensah"),
                DayOfWeek = day,
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = noonEnd,
                SlotDurationMinutes = 30,
                IsActive = true
            });

        // Dr. Ngozi Adeyemi — Mon, Wed, Fri — 8:00 AM to 1:00 PM
        foreach (var day in new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday })
            availabilities.Add(new()
            {
                DoctorId = DoctorId("Dr. Ngozi Adeyemi"),
                DayOfWeek = day,
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(13, 0, 0),
                SlotDurationMinutes = 30,
                IsActive = true
            });

        // Dr. Samuel Boateng — Tue, Thu, Sat — 9:00 AM to 2:00 PM
        foreach (var day in new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday })
            availabilities.Add(new()
            {
                DoctorId = DoctorId("Dr. Samuel Boateng"),
                DayOfWeek = day,
                StartTime = morningStart,
                EndTime = new TimeSpan(14, 0, 0),
                SlotDurationMinutes = 30,
                IsActive = true
            });

        // Dr. Chidinma Eze — Mon, Wed, Fri — 9:00 AM to 4:00 PM
        foreach (var day in new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday })
            availabilities.Add(new()
            {
                DoctorId = DoctorId("Dr. Chidinma Eze"),
                DayOfWeek = day,
                StartTime = morningStart,
                EndTime = new TimeSpan(16, 0, 0),
                SlotDurationMinutes = 30,
                IsActive = true
            });

        // Dr. Emmanuel Owusu — All days (24/7 emergency coverage, but let's seed standard times for booking)
        foreach (var day in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday })
            availabilities.Add(new()
            {
                DoctorId = DoctorId("Dr. Emmanuel Owusu"),
                DayOfWeek = day,
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(20, 0, 0),  // 8 AM to 8 PM for emergency coverage
                SlotDurationMinutes = 30,
                IsActive = true
            });

        // Dr. Abena Asante — Mon to Fri — 8:00 AM to 3:00 PM
        foreach (var day in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday })
            availabilities.Add(new()
            {
                DoctorId = DoctorId("Dr. Abena Asante"),
                DayOfWeek = day,
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(15, 0, 0),
                SlotDurationMinutes = 30,
                IsActive = true
            });

        context.DoctorAvailabilities.AddRange(availabilities);
        await context.SaveChangesAsync();
    }
}
