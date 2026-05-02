using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;

namespace Okafor_.NET.Areas.Patient.Controllers;

public class DocumentsController : PatientBaseController
{
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx"
    };

    private const long MaxFileBytes = 10 * 1024 * 1024; // 10 MB

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public DocumentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
    {
        _context = context;
        _userManager = userManager;
        _env = env;
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

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file, string title, string description)
    {
        var userId = _userManager.GetUserId(User)!;
        var profile = await _context.PatientProfiles
            .FirstOrDefaultAsync(p => p.ApplicationUserId == userId);

        if (profile is null)
            return RedirectToAction("Create", "Profile");

        title = title?.Trim() ?? string.Empty;
        description = description?.Trim();

        if (string.IsNullOrWhiteSpace(title))
            ModelState.AddModelError(nameof(title), "Document title is required.");

        if (file is null || file.Length == 0)
            ModelState.AddModelError(nameof(file), "Please select a file to upload.");
        else
        {
            var validationError = ValidateFile(file);
            if (validationError is not null)
                ModelState.AddModelError(nameof(file), validationError);
        }

        if (!ModelState.IsValid)
            return View();

        string? fileUrl = null;
        try
        {
            fileUrl = await SaveDocumentAsync(file!);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(nameof(file), $"Failed to save file: {ex.Message}");
            return View();
        }

        var document = new PatientDocument
        {
            PatientProfileId = profile.Id,
            Title = title,
            Description = description,
            FileUrl = fileUrl,
            UploadedAt = DateTime.UtcNow
        };

        _context.PatientDocuments.Add(document);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Document uploaded successfully.";
        return RedirectToAction(nameof(Index));
    }

    private string? ValidateFile(IFormFile file)
    {
        if (file.Length > MaxFileBytes)
            return $"File size exceeds {MaxFileBytes / (1024 * 1024)} MB limit.";

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return "Only PDF, JPG, PNG, DOC, and DOCX files are allowed.";

        if (!AllowedMimeTypes.Contains(file.ContentType))
            return "Invalid file type.";

        return null;
    }

    private async Task<string> SaveDocumentAsync(IFormFile file)
    {
        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "patient-documents");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return Path.Combine("/uploads/patient-documents", fileName).Replace("\\", "/");
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

        // Delete file from disk
        try
        {
            var filePath = Path.Combine(_env.WebRootPath, document.FileUrl.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }
        catch
        {
            // Log but don't fail
        }

        _context.PatientDocuments.Remove(document);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Document deleted.";
        return RedirectToAction(nameof(Index));
    }
}
