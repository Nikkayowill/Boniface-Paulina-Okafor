using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.Services;

namespace Okafor_.NET.Tests;

public sealed class AppointmentRequestMaintenanceServiceTests
{
    [Fact]
    public async Task DeleteRequestAsync_ReleasesBookedSlotAndRemovesLinkedPortalAppointment()
    {
        await using var context = CreateContext();
        var service = new AppointmentRequestMaintenanceService(context);
        var seed = await SeedApprovedAppointmentAsync(context);

        var deleted = await service.DeleteRequestAsync(seed.AppointmentRequestId);

        Assert.True(deleted);
        Assert.False(await context.AppointmentRequests.AnyAsync(a => a.Id == seed.AppointmentRequestId));
        Assert.False(await context.PatientAppointments.AnyAsync(a => a.AppointmentRequestId == seed.AppointmentRequestId));

        var slot = await context.AppointmentSlots.SingleAsync(s => s.Id == seed.AppointmentSlotId);
        Assert.False(slot.IsBooked);
        Assert.Null(slot.AppointmentRequestId);
        Assert.False(slot.ReminderSent);
    }

    [Fact]
    public async Task DeleteRequestAsync_ReturnsFalseWhenRequestDoesNotExist()
    {
        await using var context = CreateContext();
        var service = new AppointmentRequestMaintenanceService(context);

        var deleted = await service.DeleteRequestAsync(9999);

        Assert.False(deleted);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"AppointmentMaintenance_{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<(int AppointmentRequestId, int AppointmentSlotId)> SeedApprovedAppointmentAsync(ApplicationDbContext context)
    {
        var department = new Department { Name = "General Medicine" };
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        var doctor = new Doctor
        {
            FullName = "Dr. Ada Okafor",
            DepartmentId = department.Id,
            Specialty = "Family Medicine",
            Bio = "Primary care",
            Qualifications = "MBBS"
        };

        var profile = new PatientProfile
        {
            FullName = "Patient Example",
            Phone = "+2348012345678",
            ApplicationUserId = "patient-user-id"
        };

        context.Doctors.Add(doctor);
        context.PatientProfiles.Add(profile);
        await context.SaveChangesAsync();

        var appointmentDate = DateTime.Today.AddDays(7).AddHours(10);
        var request = new AppointmentRequest
        {
            PatientName = profile.FullName,
            Email = "patient@example.com",
            Phone = profile.Phone!,
            DepartmentId = department.Id,
            DoctorId = doctor.Id,
            PreferredDate = appointmentDate.Date,
            PreferredTime = appointmentDate.ToString("HH:mm"),
            Status = AppointmentStatus.Approved,
            ContactConfirmed = true,
            ContactMethod = "Call",
            CreatedAt = DateTime.UtcNow
        };

        context.AppointmentRequests.Add(request);
        await context.SaveChangesAsync();

        var slot = new AppointmentSlot
        {
            DoctorId = doctor.Id,
            SlotDateTime = appointmentDate,
            IsBooked = true,
            ReminderSent = true,
            AppointmentRequestId = request.Id
        };

        var portalAppointment = new PatientAppointment
        {
            AppointmentRequestId = request.Id,
            PatientProfileId = profile.Id,
            DepartmentId = department.Id,
            DoctorId = doctor.Id,
            AppointmentDate = appointmentDate,
            Status = PatientAppointmentStatus.Confirmed
        };

        context.AppointmentSlots.Add(slot);
        context.PatientAppointments.Add(portalAppointment);
        await context.SaveChangesAsync();

        return (request.Id, slot.Id);
    }
}
