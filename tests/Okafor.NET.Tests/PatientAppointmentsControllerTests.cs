using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Areas.Admin.Controllers;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.ViewModels;

namespace Okafor_.NET.Tests;

/// <summary>
/// Covers /Admin/PatientAppointments Create and Edit, which is the one appointment-writing
/// path in the app that previously did zero conflict checking: an admin scheduling a patient
/// directly could silently double-book a doctor already booked at that exact date/time.
/// </summary>
public sealed class PatientAppointmentsControllerTests : IAsyncLifetime
{
    private ApplicationDbContext _context = null!;
    private PatientAppointmentsController _controller = null!;
    private int _doctorId;
    private int _departmentId;
    private int _firstPatientId;
    private int _secondPatientId;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"PatientAppointmentsControllerTests_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        var department = new Department { Name = "General Medicine" };
        var doctor = new Doctor
        {
            FullName = "Dr. Amaka Nwosu",
            Department = department,
            Specialty = "General Medicine",
            Bio = "Test",
            Qualifications = "Test"
        };
        var firstPatient = new PatientProfile { FullName = "Chidi Okeke", ApplicationUserId = "user-1" };
        var secondPatient = new PatientProfile { FullName = "Ngozi Bello", ApplicationUserId = "user-2" };

        _context.Departments.Add(department);
        _context.Doctors.Add(doctor);
        _context.PatientProfiles.AddRange(firstPatient, secondPatient);
        await _context.SaveChangesAsync();

        _doctorId = doctor.Id;
        _departmentId = department.Id;
        _firstPatientId = firstPatient.Id;
        _secondPatientId = secondPatient.Id;

        _controller = new PatientAppointmentsController(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        _context.Dispose();
    }

    [Fact]
    public async Task Create_SecondAppointmentForSameDoctorAndTime_IsRejectedAsConflict()
    {
        var slotTime = DateTime.Today.AddDays(1).AddHours(9);

        var firstResult = await _controller.Create(new AdminPatientAppointmentViewModel
        {
            PatientProfileId = _firstPatientId,
            DepartmentId = _departmentId,
            DoctorId = _doctorId,
            AppointmentDate = slotTime
        });
        Assert.IsType<RedirectToActionResult>(firstResult);

        var secondResult = await _controller.Create(new AdminPatientAppointmentViewModel
        {
            PatientProfileId = _secondPatientId,
            DepartmentId = _departmentId,
            DoctorId = _doctorId,
            AppointmentDate = slotTime
        });

        var view = Assert.IsType<ViewResult>(secondResult);
        Assert.False(_controller.ModelState.IsValid);
        Assert.Equal(1, await _context.PatientAppointments.CountAsync(a => a.DoctorId == _doctorId));
    }

    [Fact]
    public async Task Create_SameDoctorDifferentTime_Succeeds()
    {
        var baseTime = DateTime.Today.AddDays(1).AddHours(9);

        await _controller.Create(new AdminPatientAppointmentViewModel
        {
            PatientProfileId = _firstPatientId,
            DepartmentId = _departmentId,
            DoctorId = _doctorId,
            AppointmentDate = baseTime
        });

        var result = await _controller.Create(new AdminPatientAppointmentViewModel
        {
            PatientProfileId = _secondPatientId,
            DepartmentId = _departmentId,
            DoctorId = _doctorId,
            AppointmentDate = baseTime.AddHours(1)
        });

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(2, await _context.PatientAppointments.CountAsync(a => a.DoctorId == _doctorId));
    }

    [Fact]
    public async Task Create_SameDoctorAndTime_ButExistingAppointmentCancelled_Succeeds()
    {
        var slotTime = DateTime.Today.AddDays(1).AddHours(9);

        await _controller.Create(new AdminPatientAppointmentViewModel
        {
            PatientProfileId = _firstPatientId,
            DepartmentId = _departmentId,
            DoctorId = _doctorId,
            AppointmentDate = slotTime,
            Status = PatientAppointmentStatus.Cancelled
        });

        var result = await _controller.Create(new AdminPatientAppointmentViewModel
        {
            PatientProfileId = _secondPatientId,
            DepartmentId = _departmentId,
            DoctorId = _doctorId,
            AppointmentDate = slotTime
        });

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(2, await _context.PatientAppointments.CountAsync(a => a.DoctorId == _doctorId));
    }

    [Fact]
    public async Task Edit_MovingAppointmentIntoAnotherAppointmentsSlot_IsRejectedAsConflict()
    {
        var firstSlot = DateTime.Today.AddDays(1).AddHours(9);
        var secondSlot = DateTime.Today.AddDays(1).AddHours(10);

        await _controller.Create(new AdminPatientAppointmentViewModel
        {
            PatientProfileId = _firstPatientId,
            DepartmentId = _departmentId,
            DoctorId = _doctorId,
            AppointmentDate = firstSlot
        });
        await _controller.Create(new AdminPatientAppointmentViewModel
        {
            PatientProfileId = _secondPatientId,
            DepartmentId = _departmentId,
            DoctorId = _doctorId,
            AppointmentDate = secondSlot
        });

        var secondAppointmentId = (await _context.PatientAppointments
            .SingleAsync(a => a.PatientProfileId == _secondPatientId)).Id;

        var result = await _controller.Edit(secondAppointmentId, new AdminPatientAppointmentViewModel
        {
            PatientProfileId = _secondPatientId,
            DepartmentId = _departmentId,
            DoctorId = _doctorId,
            AppointmentDate = firstSlot
        });

        Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);

        var unchanged = await _context.PatientAppointments.FindAsync(secondAppointmentId);
        Assert.Equal(secondSlot, unchanged!.AppointmentDate);
    }

    [Fact]
    public async Task Edit_KeepingItsOwnExistingTime_Succeeds()
    {
        var slotTime = DateTime.Today.AddDays(1).AddHours(9);

        await _controller.Create(new AdminPatientAppointmentViewModel
        {
            PatientProfileId = _firstPatientId,
            DepartmentId = _departmentId,
            DoctorId = _doctorId,
            AppointmentDate = slotTime
        });

        var appointmentId = (await _context.PatientAppointments.SingleAsync()).Id;

        var result = await _controller.Edit(appointmentId, new AdminPatientAppointmentViewModel
        {
            PatientProfileId = _firstPatientId,
            DepartmentId = _departmentId,
            DoctorId = _doctorId,
            AppointmentDate = slotTime,
            Notes = "Updated notes"
        });

        Assert.IsType<RedirectToActionResult>(result);
        var updated = await _context.PatientAppointments.FindAsync(appointmentId);
        Assert.Equal("Updated notes", updated!.Notes);
    }
}
