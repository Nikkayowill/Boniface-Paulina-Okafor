using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;

namespace Okafor_.NET.Areas.Admin.Controllers;

[Authorize(Roles = "Admin")]
public class PatientMessagesController : AdminBaseController
{
    private readonly ApplicationDbContext _context;

    public PatientMessagesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var messages = await _context.PatientMessages
            .AsNoTracking()
            .Include(message => message.PatientProfile)
                .ThenInclude(profile => profile!.ApplicationUser)
            .OrderBy(message => message.IsRead)
            .ThenByDescending(message => message.SentAt)
            .ToListAsync();

        return View(messages);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var message = await _context.PatientMessages
            .AsNoTracking()
            .Include(item => item.PatientProfile)
                .ThenInclude(profile => profile!.ApplicationUser)
            .FirstOrDefaultAsync(item => item.Id == id);

        return message is null ? NotFound() : View(message);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkReviewed(int id)
    {
        var message = await _context.PatientMessages.FindAsync(id);
        if (message is null)
        {
            return NotFound();
        }

        message.IsRead = true;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Patient message marked as reviewed.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
