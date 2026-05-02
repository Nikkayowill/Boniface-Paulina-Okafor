using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.ViewModels;

namespace Okafor_.NET.Areas.Admin.Controllers;

public class PatientProfilesController : AdminBaseController
{
    private static readonly HashSet<string> AllowedDocumentMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf", "image/jpeg", "image/png", "image/webp"
    };

    private static readonly HashSet<string> AllowedDocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".webp"
    };

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public PatientProfilesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // ── List all patient profiles ──────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var profiles = await _context.PatientProfiles
            .AsNoTracking()
            .Include(p => p.ApplicationUser)
            .OrderBy(p => p.FullName)
            .ToListAsync();

        return View(profiles);
    }

    // ── Create patient profile ─────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
        ViewBag.Users = new SelectList(users, "Id", "Email");
        return View(new AdminPatientProfileViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminPatientProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
            ViewBag.Users = new SelectList(users, "Id", "Email", model.ApplicationUserId);
            return View(model);
        }

        var exists = await _context.PatientProfiles.AnyAsync(p => p.ApplicationUserId == model.ApplicationUserId);
        if (exists)
        {
            ModelState.AddModelError(nameof(model.ApplicationUserId), "This user already has a patient profile.");
            var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
            ViewBag.Users = new SelectList(users, "Id", "Email", model.ApplicationUserId);
            return View(model);
        }

        var profile = new PatientProfile
        {
            ApplicationUserId = model.ApplicationUserId,
            FullName          = model.FullName,
            Phone             = model.Phone
        };

        _context.PatientProfiles.Add(profile);
        await _context.SaveChangesAsync();

        var user = await _userManager.FindByIdAsync(model.ApplicationUserId);
        if (user is not null && !await _userManager.IsInRoleAsync(user, "Patient"))
        {
            var addRoleResult = await _userManager.AddToRoleAsync(user, "Patient");
            if (!addRoleResult.Succeeded)
            {
                var errors = string.Join("; ", addRoleResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to assign Patient role: {errors}");
            }
        }

        return RedirectToAction("Details", new { id = profile.Id });
    }

    // ── Patient documents ──────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var profile = await _context.PatientProfiles
            .AsNoTracking()
            .Include(p => p.ApplicationUser)
            .Include(p => p.Documents.OrderByDescending(d => d.UploadedAt))
            .FirstOrDefaultAsync(p => p.Id == id);

        if (profile is null)
            return NotFound();

        return View(profile);
    }

    // ── Upload document to patient ─────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> UploadDocument(int patientId)
    {
        var profile = await _context.PatientProfiles.FindAsync(patientId);
        if (profile is null) return NotFound();

        ViewBag.PatientName = profile.FullName;
        return View(new AdminUploadDocumentViewModel { PatientProfileId = patientId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadDocument(AdminUploadDocumentViewModel model)
    {
        if (model.File is null || model.File.Length == 0)
            ModelState.AddModelError("File", "Please select a file to upload.");

        if (!ModelState.IsValid)
        {
            var profile = await _context.PatientProfiles.FindAsync(model.PatientProfileId);
            ViewBag.PatientName = profile?.FullName;
            return View(model);
        }

        var extension = Path.GetExtension(model.File!.FileName);
        if (!AllowedDocumentExtensions.Contains(extension) || !AllowedDocumentMimeTypes.Contains(model.File.ContentType))
        {
            ModelState.AddModelError("File", "Only PDF, JPG, JPEG, PNG, and WebP files are allowed.");
            var profile = await _context.PatientProfiles.FindAsync(model.PatientProfileId);
            ViewBag.PatientName = profile?.FullName;
            return View(model);
        }

        const long maxSize = 10 * 1024 * 1024; // 10 MB
        if (model.File.Length > maxSize)
        {
            ModelState.AddModelError("File", "File size must not exceed 10 MB.");
            var profile = await _context.PatientProfiles.FindAsync(model.PatientProfileId);
            ViewBag.PatientName = profile?.FullName;
            return View(model);
        }

        // Save to wwwroot/uploads/patient-documents/
        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "patient-documents");
        Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(model.File.FileName);
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
            await model.File.CopyToAsync(stream);

        var document = new PatientDocument
        {
            PatientProfileId = model.PatientProfileId,
            Title            = model.Title,
            Description      = model.Description,
            FileUrl          = $"/uploads/patient-documents/{fileName}"
        };

        _context.PatientDocuments.Add(document);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Document \"{model.Title}\" uploaded successfully.";
        return RedirectToAction("Details", new { id = model.PatientProfileId });
    }

    // ── Delete document ────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDocument(int documentId, int patientId)
    {
        var doc = await _context.PatientDocuments.FindAsync(documentId);
        if (doc is not null)
        {
            // Remove physical file if it exists
            var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                doc.FileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(physicalPath))
                System.IO.File.Delete(physicalPath);

            _context.PatientDocuments.Remove(doc);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("Details", new { id = patientId });
    }
}
