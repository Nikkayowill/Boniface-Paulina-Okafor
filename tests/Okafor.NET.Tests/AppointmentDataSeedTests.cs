using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.Seed;

namespace Okafor_.NET.Tests;

public sealed class AppointmentDataSeedTests
{
    private static readonly DateTime ReferenceUtc =
        new(2026, 7, 21, 18, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task SeedAsync_CreatesCurrentClearlySyntheticDemoQueue()
    {
        await using var context = CreateContext();
        await AddClinicalRecordsAsync(context);

        await AppointmentDataSeed.SeedAsync(context, ReferenceUtc);

        var requests = await context.AppointmentRequests
            .OrderBy(request => request.PatientName)
            .ToListAsync();

        Assert.Equal(5, requests.Count);
        Assert.All(requests, request =>
        {
            Assert.Contains("Staging demo record", request.Message);
            Assert.Contains("not a real patient", request.ContactNotes, StringComparison.OrdinalIgnoreCase);
        });
        Assert.All(
            requests.Where(request => request.Status is AppointmentStatus.Pending or AppointmentStatus.Approved),
            request => Assert.True(request.PreferredDate > ReferenceUtc.Date));
        Assert.All(requests, request => Assert.True(request.CreatedAt <= ReferenceUtc));
    }

    [Fact]
    public async Task SeedAsync_UsesDoctorScheduleForFutureDemoRequests()
    {
        await using var context = CreateContext();
        await AddClinicalRecordsAsync(context);

        await AppointmentDataSeed.SeedAsync(context, ReferenceUtc);

        var requests = await context.AppointmentRequests
            .Include(request => request.Doctor)
            .Where(request =>
                request.Status == AppointmentStatus.Pending ||
                request.Status == AppointmentStatus.Approved)
            .ToListAsync();

        Assert.All(requests, request =>
        {
            var allowedDays = request.Doctor!.FullName switch
            {
                "Dr. Amara Osei" or "Dr. Chidinma Eze" =>
                    new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday },
                _ =>
                    new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }
            };

            Assert.Contains(request.PreferredDate.DayOfWeek, allowedDays);
        });
    }

    [Fact]
    public async Task SeedAsync_DoesNotDuplicateAnExistingQueue()
    {
        await using var context = CreateContext();
        await AddClinicalRecordsAsync(context);

        await AppointmentDataSeed.SeedAsync(context, ReferenceUtc);
        await AppointmentDataSeed.SeedAsync(context, ReferenceUtc.AddDays(1));

        Assert.Equal(5, await context.AppointmentRequests.CountAsync());
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"AppointmentDataSeed_{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task AddClinicalRecordsAsync(ApplicationDbContext context)
    {
        var departments = new[]
        {
            new Department { Name = "General Medicine" },
            new Department { Name = "Maternity Care" },
            new Department { Name = "Diagnostics & Laboratory" },
            new Department { Name = "Pediatrics" },
            new Department { Name = "Surgical Services" }
        };
        context.Departments.AddRange(departments);
        await context.SaveChangesAsync();

        context.Doctors.AddRange(
            new Doctor { FullName = "Dr. Amara Osei", DepartmentId = departments[0].Id },
            new Doctor { FullName = "Dr. Chidinma Eze", DepartmentId = departments[1].Id },
            new Doctor { FullName = "Dr. Abena Asante", DepartmentId = departments[2].Id },
            new Doctor { FullName = "Dr. Kofi Mensah", DepartmentId = departments[3].Id },
            new Doctor { FullName = "Dr. Samuel Boateng", DepartmentId = departments[4].Id });
        await context.SaveChangesAsync();
    }
}
