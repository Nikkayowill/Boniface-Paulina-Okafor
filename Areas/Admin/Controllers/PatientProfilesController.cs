using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.Services;
using Okafor_.NET.ViewModels;

namespace Okafor_.NET.Areas.Admin.Controllers;

public class PatientProfilesController : AdminBaseController
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPatientDocumentStorageService _documentStorage;

    public PatientProfilesController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IPatientDocumentStorageService documentStorage)
    {
        _context = context;
        _userManager = userManager;
        _documentStorage = documentStorage;
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

        if (model.File is not null && model.File.Length > 0)
        {
            var validation = _documentStorage.Validate(model.File, PatientDocumentUploadPolicy.Admin);
            if (!validation.IsValid)
                ModelState.AddModelError("File", validation.ErrorMessage!);
        }

        if (!ModelState.IsValid || model.File is null)
        {
            var profile = await _context.PatientProfiles.FindAsync(model.PatientProfileId);
            ViewBag.PatientName = profile?.FullName;
            return View(model);
        }

        var patientExists = await _context.PatientProfiles
            .AnyAsync(p => p.Id == model.PatientProfileId);
        if (!patientExists)
            return NotFound();

        StoredPatientDocument storedDocument;
        try
        {
            storedDocument = await _documentStorage.SaveAsync(model.File, PatientDocumentUploadPolicy.Admin);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("File", $"Failed to save file: {ex.Message}");
            var profile = await _context.PatientProfiles.FindAsync(model.PatientProfileId);
            ViewBag.PatientName = profile?.FullName;
            return View(model);
        }

        var document = new PatientDocument
        {
            PatientProfileId = model.PatientProfileId,
            Title            = model.Title,
            Description      = model.Description,
            FileUrl          = storedDocument.StorageKey
        };

        _context.PatientDocuments.Add(document);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch
        {
            await _documentStorage.DeleteAsync(storedDocument.StorageKey);
            throw;
        }

        TempData["Success"] = $"Document \"{model.Title}\" uploaded successfully.";
        return RedirectToAction("Details", new { id = model.PatientProfileId });
    }

    // ── Delete document ────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDocument(int documentId, int patientId)
    {
        var doc = await _context.PatientDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && d.PatientProfileId == patientId);
        if (doc is not null)
        {
            await _documentStorage.DeleteAsync(doc.FileUrl);

            _context.PatientDocuments.Remove(doc);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("Details", new { id = patientId });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadDocument(int documentId, int patientId)
    {
        var doc = await _context.PatientDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId && d.PatientProfileId == patientId);

        if (doc is null)
            return NotFound();

        var file = await _documentStorage.OpenReadAsync(doc.FileUrl);
        if (file is null)
            return NotFound();

        return File(file.Stream, file.ContentType, file.FileName);
    }
}
