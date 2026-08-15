using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;

namespace Okafor_.NET.Seed;

public static class ClinicalDataSeed
{
    private const string SpiritualCareDepartment = "Spiritual Care and Psychotherapy";
    private const string FatherToochukwuSlug = "rev-fr-dr-toochukwu-bartholomew-okafor";
    private const string MedicalOfficerSlug = "dr-opie-thomas-n";
    private const string LegacyMaleDoctorImage = "/images/placeholders/nigerian-doctor-male.webp";
    private const string LegacyFemaleDoctorImage = "/images/placeholders/nigerian-doctor-female.webp";

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await SeedDepartmentsAsync(context);
        await RepairLegacyDoctorImagesAsync(context);
        await RemoveUnapprovedDoctorsAsync(context);
        await SeedDoctorsAsync(context);
        await SeedDoctorAvailabilityAsync(context);
    }

    private static async Task RemoveUnapprovedDoctorsAsync(ApplicationDbContext context)
    {
        var realDoctorSlugs = new[] { FatherToochukwuSlug, MedicalOfficerSlug };
        var fakeDoctorIds = await context.Doctors
            .IgnoreQueryFilters()
            .Where(doctor => doctor.Slug == null || !realDoctorSlugs.Contains(doctor.Slug))
            .Select(doctor => doctor.Id)
            .ToListAsync();

        if (fakeDoctorIds.Count == 0)
            return;

        // Preserve historical requests and appointments, but remove their links to
        // profiles that must no longer appear on the site.
        await context.PatientAppointments
            .Where(appointment => appointment.DoctorId.HasValue && fakeDoctorIds.Contains(appointment.DoctorId.Value))
            .ExecuteUpdateAsync(update => update.SetProperty(appointment => appointment.DoctorId, (int?)null));
        await context.TeleconsultationRequests
            .Where(request => request.DoctorId.HasValue && fakeDoctorIds.Contains(request.DoctorId.Value))
            .ExecuteUpdateAsync(update => update.SetProperty(request => request.DoctorId, (int?)null));
        await context.AppointmentSlots
            .Where(slot => fakeDoctorIds.Contains(slot.DoctorId))
            .ExecuteDeleteAsync();
        await context.DoctorAvailabilities
            .Where(availability => fakeDoctorIds.Contains(availability.DoctorId))
            .ExecuteDeleteAsync();
        await context.Doctors
            .Where(doctor => fakeDoctorIds.Contains(doctor.Id))
            .ExecuteDeleteAsync();
    }

    private static async Task RepairLegacyDoctorImagesAsync(ApplicationDbContext context)
    {
        var doctorsWithMissingSeedImages = await context.Doctors
            .Where(doctor => doctor.ImageUrl == LegacyMaleDoctorImage || doctor.ImageUrl == LegacyFemaleDoctorImage)
            .ToListAsync();

        if (doctorsWithMissingSeedImages.Count == 0)
            return;

        foreach (var doctor in doctorsWithMissingSeedImages)
            doctor.ImageUrl = null;

        await context.SaveChangesAsync();
    }

    // ── Departments ────────────────────────────────────────────────────────

    private static async Task SeedDepartmentsAsync(ApplicationDbContext context)
    {
        var departments = new List<Department>
        {
            new() { Name = "General Medicine",          Description = "Comprehensive primary and general adult medical care." },
            new() { Name = "Pediatrics",                Description = "Healthcare for infants, children, and adolescents." },
            new() { Name = "Diagnostics & Laboratory",  Description = "Blood work, imaging, and diagnostic testing services." },
            new() { Name = "Surgical Services",         Description = "Elective and emergency surgical procedures." },
            new() { Name = "Emergency Care",            Description = "24/7 urgent and emergency medical treatment." },
            new() { Name = "Maternity Care",            Description = "Prenatal, delivery, and postnatal care for mothers and newborns." },
            new() { Name = SpiritualCareDepartment,      Description = "Confidential spiritual-emotional support, counselling, and psychotherapy through reviewed teleconsultation requests." },
        };

        var existingNames = await context.Departments
            .AsNoTracking()
            .Select(department => department.Name)
            .ToListAsync();
        var missingDepartments = departments
            .Where(department => !existingNames.Contains(department.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (missingDepartments.Count > 0)
        {
            context.Departments.AddRange(missingDepartments);
            await context.SaveChangesAsync();
        }
    }

    // ── Doctors ────────────────────────────────────────────────────────────

    private static async Task SeedDoctorsAsync(ApplicationDbContext context)
    {
        // Fetch department IDs by name after additive department seeding.
        var depts = await context.Departments
            .AsNoTracking()
            .ToDictionaryAsync(d => d.Name, d => d.Id);

        int Dept(string name) => depts.TryGetValue(name, out var id) ? id : throw new InvalidOperationException($"Department '{name}' not found.");

        var doctors = new List<Doctor>
        {
            new()
            {
                FullName          = "Dr. Opie Thomas N.",
                Slug              = MedicalOfficerSlug,
                Specialty         = "Medical Officer & General Practitioner",
                Qualifications    = "MBBS, Benue State University, Makurdi; BSc Human Physiology, University of Calabar, Cross River State",
                ConsultationHours = "Contact the hospital for current clinic availability",
                Bio               = "Dr. Opie Thomas N. is a Nigerian medical officer and general practitioner supporting patients and hospital operations at Boniface & Paulina Okafor Memorial Hospital. His practice includes acute and chronic care, emergency stabilization, maternal and child health, preventive screening, inpatient review, referrals, and community outreach.",
                ImageUrl          = "/images/team/dr-opie-thomas.webp",
                DepartmentId      = Dept("General Medicine")
            },
            new()
            {
                FullName          = "Rev. Fr. Dr. Toochukwu Bartholomew Okafor",
                Slug              = FatherToochukwuSlug,
                Specialty         = "Spiritual Care, Counselling & Psychotherapy",
                Qualifications    = "B.Phil, Claretian Institute of Philosophy; B.Th, Bigard Memorial Seminary; Diploma in Drug Dependency Counselling, St. Bonaventure University in association with Hogares Claret; MA in Counselling Psychology, Yorkville University; PhD in Clinical Psychology, Enugu State University of Science and Technology",
                ConsultationHours = "Teleconsultation by request — final date and time confirmed by staff",
                Bio               = "Fr. Toochukwu Okafor was born in Isuochi, Abia State, Nigeria, and is a Canadian citizen. He is the founder of B&P Memorial Hospital and B&P Charity Foundation, Project Coordinator for the Nigeria Family Helper Program in Halifax, Canada, and Pastor of Christ the King Parish in Dartmouth, Nova Scotia. He provides spiritual-emotional support and counselling to individuals, families, couples, and people navigating a range of personal challenges.",
                ImageUrl          = "/images/placeholders/Hospital/UILL6048.webp",
                DepartmentId      = Dept(SpiritualCareDepartment)
            },
        };

        var existingSlugs = await context.Doctors
            .AsNoTracking()
            .Where(doctor => doctor.Slug != null)
            .Select(doctor => doctor.Slug!)
            .ToListAsync();
        var existingNames = await context.Doctors
            .AsNoTracking()
            .Select(doctor => doctor.FullName)
            .ToListAsync();
        var missingDoctors = doctors
            .Where(doctor =>
                !existingSlugs.Contains(doctor.Slug ?? string.Empty, StringComparer.OrdinalIgnoreCase) &&
                !existingNames.Contains(doctor.FullName, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (missingDoctors.Count > 0)
        {
            context.Doctors.AddRange(missingDoctors);
            await context.SaveChangesAsync();
        }
    }

    // ── Doctor Availabilities ──────────────────────────────────────────────

    private static async Task SeedDoctorAvailabilityAsync(ApplicationDbContext context)
    {
        if (await context.DoctorAvailabilities.AnyAsync())
            return;

        var doctor = await context.Doctors
            .AsNoTracking()
            .SingleAsync(item => item.Slug == MedicalOfficerSlug);
        var availabilities = new List<DoctorAvailability>();
        foreach (var day in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday })
            availabilities.Add(new()
            {
                DoctorId = doctor.Id,
                DayOfWeek = day,
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(17, 0, 0),
                SlotDurationMinutes = 30,
                IsActive = true
            });

        context.DoctorAvailabilities.AddRange(availabilities);
        await context.SaveChangesAsync();
    }
}
