using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.ViewModels;

namespace Okafor_.NET.Areas.Patient.Controllers;

public class ProfileController : PatientBaseController
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;
        var profile = await _context.PatientProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ApplicationUserId == userId);

        if (profile is null)
            return RedirectToAction("Create");

        return View(profile);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var userId = _userManager.GetUserId(User)!;
        var existing = await _context.PatientProfiles
            .AnyAsync(p => p.ApplicationUserId == userId);

        if (existing)
            return RedirectToAction("Index");

        return View(new PatientProfileEditViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PatientProfileEditViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userId = _userManager.GetUserId(User)!;
        var existing = await _context.PatientProfiles
            .AnyAsync(p => p.ApplicationUserId == userId);

        if (existing)
            return RedirectToAction("Index");

        var profile = new PatientProfile
        {
            ApplicationUserId = userId,
            FullName          = model.FullName,
            Phone             = model.Phone,
            Address           = model.Address
        };

        _context.PatientProfiles.Add(profile);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var userId = _userManager.GetUserId(User)!;
        var profile = await _context.PatientProfiles
            .FirstOrDefaultAsync(p => p.ApplicationUserId == userId);

        if (profile is null)
            return RedirectToAction("Create");

        return View(new PatientProfileEditViewModel
        {
            FullName = profile.FullName,
            Phone    = profile.Phone,
            Address  = profile.Address
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PatientProfileEditViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userId = _userManager.GetUserId(User)!;
        var profile = await _context.PatientProfiles
            .FirstOrDefaultAsync(p => p.ApplicationUserId == userId);

        if (profile is null)
            return RedirectToAction("Create");

        profile.FullName = model.FullName;
        profile.Phone    = model.Phone;
        profile.Address  = model.Address;

        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }
}
