using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;

namespace Okafor_.NET.Tests.Integration.SqlServer;

[Collection(SqlServerIntegrationCollection.Name)]
[Trait("Category", "DatabaseIntegration")]
public sealed class AppointmentTransactionWorkflowTests : SqlServerIntegrationTestBase
{
    public AppointmentTransactionWorkflowTests(SqlServerIntegrationFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task ApprovalWorkflow_WhenFailureOccurs_RollsBackNewRowsAndExistingChanges()
    {
        var doctorId = await SeedDoctorWithAvailabilityAsync();

        Func<Task> workflow = () => ExecuteFailingApprovalWorkflowAsync(doctorId);

        await workflow.Should()
            .ThrowAsync<SimulatedWorkflowException>()
            .WithMessage("Simulated failure after the appointment slot was reserved.");

        await using var verificationContext = Fixture.CreateDbContext();
        var availability = await verificationContext.DoctorAvailabilities
            .AsNoTracking()
            .SingleAsync(item => item.DoctorId == doctorId);

        availability.IsActive.Should().BeTrue("the pre-existing availability change must roll back");
        (await verificationContext.AppointmentRequests.CountAsync()).Should().Be(0);
        (await verificationContext.AppointmentSlots.CountAsync()).Should().Be(0);
        (await verificationContext.Doctors.CountAsync()).Should().Be(1);
        (await verificationContext.Departments.CountAsync()).Should().Be(1);
    }

    private async Task<int> SeedDoctorWithAvailabilityAsync()
    {
        await using var context = Fixture.CreateDbContext();
        var department = new Department
        {
            Name = "General Medicine",
            Description = "SQL Server integration-test department"
        };
        var doctor = new Doctor
        {
            FullName = "Dr. Integration Test",
            Slug = "dr-integration-test",
            Specialty = "General Medicine",
            Bio = "Fictional doctor used only by automated tests.",
            Qualifications = "Test qualification",
            Department = department
        };
        doctor.Department = department;
        context.Doctors.Add(doctor);
        context.DoctorAvailabilities.Add(new DoctorAvailability
        {
            Doctor = doctor,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(12, 0, 0),
            SlotDurationMinutes = 30,
            IsActive = true
        });

        await context.SaveChangesAsync();
        return doctor.Id;
    }

    private async Task ExecuteFailingApprovalWorkflowAsync(int doctorId)
    {
        await using var context = Fixture.CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var doctor = await context.Doctors
                .Include(item => item.Department)
                .SingleAsync(item => item.Id == doctorId);
            var availability = await context.DoctorAvailabilities
                .SingleAsync(item => item.DoctorId == doctorId);
            var slotDateTime = NextOccurrence(DayOfWeek.Monday).AddHours(9);
            var request = new AppointmentRequest
            {
                PatientName = "Ada Test Patient",
                Email = "ada.integration@example.test",
                Phone = "+2348000000000",
                DepartmentId = doctor.DepartmentId,
                DoctorId = doctor.Id,
                PreferredDate = slotDateTime.Date,
                PreferredTime = slotDateTime.ToString("HH:mm"),
                Message = "Fictional integration-test appointment.",
                Status = AppointmentStatus.Approved,
                ContactConfirmed = true,
                ContactMethod = "Email",
                ContactConfirmedAt = DateTime.UtcNow,
                ApprovedAt = DateTime.UtcNow,
                ApprovedByUserId = "integration-test"
            };

            context.AppointmentRequests.Add(request);
            await context.SaveChangesAsync();

            context.AppointmentSlots.Add(new AppointmentSlot
            {
                DoctorId = doctor.Id,
                SlotDateTime = slotDateTime,
                IsBooked = true,
                AppointmentRequestId = request.Id
            });
            availability.IsActive = false;
            await context.SaveChangesAsync();

            throw new SimulatedWorkflowException(
                "Simulated failure after the appointment slot was reserved.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static DateTime NextOccurrence(DayOfWeek dayOfWeek)
    {
        var date = DateTime.Today.AddDays(1);
        while (date.DayOfWeek != dayOfWeek)
        {
            date = date.AddDays(1);
        }

        return date;
    }

    private sealed class SimulatedWorkflowException : Exception
    {
        public SimulatedWorkflowException(string message)
            : base(message)
        {
        }
    }
}
