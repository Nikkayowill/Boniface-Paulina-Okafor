using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.Services;
using Okafor_.NET.ViewModels;

namespace Okafor_.NET.Areas.Patient.Controllers;

[RequireLaunchFeature(LaunchFeature.PatientDocuments)]
public class DocumentsController : PatientBaseController
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPatientDocumentStorageService _documentStorage;

    public DocumentsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IPatientDocumentStorageService documentStorage)
    {
        _context = context;
        _userManager = userManager;
        _documentStorage = documentStorage;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;
        var profile = await _context.PatientProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ApplicationUserId == userId);

        if (profile is null)
        {
            ViewBag.HasProfile = false;
            return View(Array.Empty<PatientDocument>());
        }

        ViewBag.HasProfile = true;
        ViewBag.PatientName = profile.FullName;

        var documents = await _context.PatientDocuments
            .AsNoTracking()
            .Where(d => d.PatientProfileId == profile.Id)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        return View(documents);
    }

    [HttpGet]
    public async Task<IActionResult> Upload()
    {
        var userId = _userManager.GetUserId(User)!;
        var profile = await _context.PatientProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ApplicationUserId == userId);

        if (profile is null)
            return RedirectToAction("Create", "Profile");

        return View(new PatientDocumentUploadViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(PatientDocumentUploadViewModel model)
    {
        var userId = _userManager.GetUserId(User)!;
        var profile = await _context.PatientProfiles
            .FirstOrDefaultAsync(p => p.ApplicationUserId == userId);

        if (profile is null)
            return RedirectToAction("Create", "Profile");

        model.Title = (model.Title ?? string.Empty).Trim();
        model.Description = string.IsNullOrWhiteSpace(model.Description)
            ? null
            : model.Description.Trim();

        if (model.File is null || model.File.Length == 0)
            ModelState.AddModelError(nameof(model.File), "Please select a file to upload.");
        else
        {
            var validation = _documentStorage.Validate(model.File, PatientDocumentUploadPolicy.Patient);
            if (!validation.IsValid)
                ModelState.AddModelError(nameof(model.File), validation.ErrorMessage!);
        }

        if (!ModelState.IsValid || model.File is null)
            return View(model);

        StoredPatientDocument? storedDocument = null;
        try
        {
            storedDocument = await _documentStorage.SaveAsync(model.File, PatientDocumentUploadPolicy.Patient);
        }
        catch
        {
            ModelState.AddModelError(nameof(model.File), "The document could not be saved. Please try again.");
            return View(model);
        }

        var document = new PatientDocument
        {
            PatientProfileId = profile.Id,
            Title = model.Title,
            Description = model.Description,
            FileUrl = storedDocument.StorageKey,
            UploadedAt = DateTime.UtcNow
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

        TempData["Success"] = "Document uploaded successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var profile = await _context.PatientProfiles
            .FirstOrDefaultAsync(p => p.ApplicationUserId == userId);

        if (profile is null)
            return RedirectToAction("Create", "Profile");

        var document = await _context.PatientDocuments
            .FirstOrDefaultAsync(d => d.Id == id && d.PatientProfileId == profile.Id);

        if (document is null)
            return NotFound();

        await _documentStorage.DeleteAsync(document.FileUrl);

        _context.PatientDocuments.Remove(document);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Document deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Download(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var document = await _context.PatientDocuments
            .AsNoTracking()
            .Include(d => d.PatientProfile)
            .FirstOrDefaultAsync(d => d.Id == id && d.PatientProfile!.ApplicationUserId == userId);

        if (document is null)
            return NotFound();

        var file = await _documentStorage.OpenReadAsync(document.FileUrl);
        if (file is null)
            return NotFound();

        return File(file.Stream, file.ContentType, file.FileName);
    }
}
