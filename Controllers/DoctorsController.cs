using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;

namespace Okafor_.NET.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DoctorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DoctorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Doctors
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Doctors.Include(d => d.Department);
            return View(await applicationDbContext.ToListAsync());
        }

        // AJAX: Get doctors by department (used by booking widget)
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetByDepartment(int deptId)
        {
            var doctors = await _context.Doctors
                .AsNoTracking()
                .Where(d => d.DepartmentId == deptId)
                .OrderBy(d => d.FullName)
                .Select(d => new { d.Id, d.FullName, d.Specialty })
                .ToListAsync();
            return Json(doctors);
        }

        // GET: Doctors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors
                .Include(d => d.Department)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (doctor == null)
            {
                return NotFound();
            }

            return View(doctor);
        }

        // GET: Doctors/Create
        public IActionResult Create()
        {
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Name");
            return View();
        }

        // POST: Doctors/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FullName,Slug,Specialty,Bio,Qualifications,ConsultationHours,ImageUrl,DepartmentId")] Doctor doctor)
        {
            doctor.Slug = BuildSlug(doctor.Slug, doctor.FullName);

            if (!string.IsNullOrWhiteSpace(doctor.Slug) && await _context.Doctors.AnyAsync(d => d.Slug == doctor.Slug))
            {
                ModelState.AddModelError(nameof(doctor.Slug), "Slug already exists for another doctor.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(doctor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Name", doctor.DepartmentId);
            return View(doctor);
        }

        // GET: Doctors/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null)
            {
                return NotFound();
            }
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Name", doctor.DepartmentId);
            return View(doctor);
        }

        // POST: Doctors/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,Slug,Specialty,Bio,Qualifications,ConsultationHours,ImageUrl,DepartmentId")] Doctor doctor)
        {
            if (id != doctor.Id)
            {
                return NotFound();
            }

            doctor.Slug = BuildSlug(doctor.Slug, doctor.FullName);

            if (!string.IsNullOrWhiteSpace(doctor.Slug) && await _context.Doctors.AnyAsync(d => d.Slug == doctor.Slug && d.Id != doctor.Id))
            {
                ModelState.AddModelError(nameof(doctor.Slug), "Slug already exists for another doctor.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(doctor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DoctorExists(doctor.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Name", doctor.DepartmentId);
            return View(doctor);
        }

        // GET: Doctors/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors
                .Include(d => d.Department)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (doctor == null)
            {
                return NotFound();
            }

            return View(doctor);
        }

        // POST: Doctors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor != null)
            {
                _context.Doctors.Remove(doctor);
            }
 
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DoctorExists(int id)
        {
            return _context.Doctors.Any(e => e.Id == id);
        }

        private static string BuildSlug(string? slug, string fullName)
        {
            var source = string.IsNullOrWhiteSpace(slug) ? fullName : slug;
            source = source.Trim().ToLowerInvariant();
            source = Regex.Replace(source, "[^a-z0-9\\s-]", string.Empty);
            source = Regex.Replace(source, "\\s+", "-");
            source = Regex.Replace(source, "-+", "-");
            return source.Trim('-');
        }
    }
}
