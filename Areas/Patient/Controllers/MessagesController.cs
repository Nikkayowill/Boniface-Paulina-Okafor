using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.ViewModels;

namespace Okafor_.NET.Areas.Patient.Controllers;

public class MessagesController : PatientBaseController
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public MessagesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
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
            return RedirectToAction("Create", "Profile");

        var messages = await _context.PatientMessages
            .AsNoTracking()
            .Where(m => m.PatientProfileId == profile.Id)
            .OrderByDescending(m => m.SentAt)
            .ToListAsync();

        return View(messages);
    }

    [HttpGet]
    public async Task<IActionResult> Send()
    {
        var userId = _userManager.GetUserId(User)!;
        var profile = await _context.PatientProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ApplicationUserId == userId);

        if (profile is null)
            return RedirectToAction("Create", "Profile");

        return View(new PatientMessageViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(PatientMessageViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userId = _userManager.GetUserId(User)!;
        var profile = await _context.PatientProfiles
            .FirstOrDefaultAsync(p => p.ApplicationUserId == userId);

        if (profile is null)
            return RedirectToAction("Create", "Profile");

        var message = new PatientMessage
        {
            PatientProfileId = profile.Id,
            Subject          = model.Subject,
            Body             = model.Body
        };

        _context.PatientMessages.Add(message);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Your message has been sent.";
        return RedirectToAction("Index");
    }
}
