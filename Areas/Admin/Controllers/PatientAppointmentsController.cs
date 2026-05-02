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
    public async Task<IActionResult> Index()
    {
        var appointments = await _context.PatientAppointments
            .AsNoTracking()
            .Include(a => a.PatientProfile)
            .Include(a => a.Department)
            .Include(a => a.Doctor)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync();

        return View(appointments);
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
        await _context.SaveChangesAsync();

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

        await _context.SaveChangesAsync();

        return RedirectToAction("Details", "PatientProfiles", new { id = appt.PatientProfileId });
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
