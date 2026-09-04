using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.ViewModels;

namespace Okafor_.NET.Areas.Admin.Controllers;

public class PatientAppointmentsController : AdminBaseController
{
    private readonly ApplicationDbContext _context;

    public PatientAppointmentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1)
    {
        const int pageSize = AdminBaseController.DefaultPageSize;
        if (page < 1) page = 1;

        var baseQuery = _context.PatientAppointments.AsNoTracking();

        var totalCount = await baseQuery.CountAsync();
        ViewData["AheadCount"] = await baseQuery.CountAsync(a => a.AppointmentDate > DateTime.Now);

        var items = await baseQuery
            .OrderByDescending(a => a.AppointmentDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new PatientAppointmentListItemViewModel
            {
                Id = a.Id,
                PatientProfileId = a.PatientProfileId,
                PatientName = a.PatientProfile != null ? a.PatientProfile.FullName : null,
                AppointmentDate = a.AppointmentDate,
                DepartmentName = a.Department != null ? a.Department.Name : null,
                DoctorName = a.Doctor != null ? a.Doctor.FullName : null,
                Notes = a.Notes,
                Status = a.Status
            })
            .ToListAsync();

        return View(new PagedResult<PatientAppointmentListItemViewModel>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? patientId)
    {
        await PopulateDropdowns(patientId);
        return View(new AdminPatientAppointmentViewModel { PatientProfileId = patientId ?? 0 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminPatientAppointmentViewModel model)
    {
        if (ModelState.IsValid &&
            await HasConflictingAppointmentAsync(model.DoctorId, model.AppointmentDate, excludeAppointmentId: null))
        {
            ModelState.AddModelError(
                nameof(model.AppointmentDate),
                "This doctor already has an appointment scheduled at that date and time.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdowns(model.PatientProfileId);
            return View(model);
        }

        var appointment = new PatientAppointment
        {
            PatientProfileId = model.PatientProfileId,
            DepartmentId     = model.DepartmentId,
            DoctorId         = model.DoctorId,
            AppointmentDate  = model.AppointmentDate,
            Status           = model.Status,
            Notes            = model.Notes
        };

        _context.PatientAppointments.Add(appointment);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // HasConflictingAppointmentAsync above already checked for this, but two concurrent
            // submissions can both pass that check before either commits — the doctor/date unique
            // index (PatientAppointmentConfiguration) is the real backstop that catches that race.
            ModelState.AddModelError(
                nameof(model.AppointmentDate),
                "This doctor already has an appointment scheduled at that date and time.");
            await PopulateDropdowns(model.PatientProfileId);
            return View(model);
        }

        return RedirectToAction("Details", "PatientProfiles", new { id = model.PatientProfileId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var appt = await _context.PatientAppointments.FindAsync(id);
        if (appt is null) return NotFound();

        await PopulateDropdowns(appt.PatientProfileId);
        return View(new AdminPatientAppointmentViewModel
        {
            PatientProfileId = appt.PatientProfileId,
            DepartmentId     = appt.DepartmentId,
            DoctorId         = appt.DoctorId,
            AppointmentDate  = appt.AppointmentDate,
            Status           = appt.Status,
            Notes            = appt.Notes
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AdminPatientAppointmentViewModel model)
    {
        if (ModelState.IsValid &&
            await HasConflictingAppointmentAsync(model.DoctorId, model.AppointmentDate, excludeAppointmentId: id))
        {
            ModelState.AddModelError(
                nameof(model.AppointmentDate),
                "This doctor already has an appointment scheduled at that date and time.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdowns(model.PatientProfileId);
            return View(model);
        }

        var appt = await _context.PatientAppointments.FindAsync(id);
        if (appt is null) return NotFound();

        appt.DepartmentId    = model.DepartmentId;
        appt.DoctorId        = model.DoctorId;
        appt.AppointmentDate = model.AppointmentDate;
        appt.Status          = model.Status;
        appt.Notes           = model.Notes;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Same race as Create above: the unique index is the real backstop.
            ModelState.AddModelError(
                nameof(model.AppointmentDate),
                "This doctor already has an appointment scheduled at that date and time.");
            await PopulateDropdowns(model.PatientProfileId);
            return View(model);
        }

        return RedirectToAction("Details", "PatientProfiles", new { id = appt.PatientProfileId });
    }

    private async Task<bool> HasConflictingAppointmentAsync(
        int? doctorId,
        DateTime appointmentDate,
        int? excludeAppointmentId)
    {
        if (doctorId is null)
            return false;

        return await _context.PatientAppointments.AsNoTracking().AnyAsync(a =>
            a.DoctorId == doctorId &&
            a.AppointmentDate == appointmentDate &&
            a.Status != PatientAppointmentStatus.Cancelled &&
            a.Id != excludeAppointmentId);
    }

    private async Task PopulateDropdowns(int? patientId = null)
    {
        var patients = await _context.PatientProfiles.AsNoTracking()
            .OrderBy(p => p.FullName).ToListAsync();
        ViewBag.Patients = new SelectList(patients, "Id", "FullName", patientId);

        var departments = await _context.Departments.AsNoTracking()
            .OrderBy(d => d.Name).ToListAsync();
        ViewBag.Departments = new SelectList(departments, "Id", "Name");

        var doctors = await _context.Doctors.AsNoTracking()
            .OrderBy(d => d.FullName).ToListAsync();
        ViewBag.Doctors = new SelectList(doctors, "Id", "FullName");

        ViewBag.Statuses = Enum.GetValues<PatientAppointmentStatus>()
            .Select(s => new SelectListItem(s.ToString(), s.ToString()));
    }
}
