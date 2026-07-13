using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;

namespace Okafor_.NET.Areas.Admin.Controllers;

public sealed class PatientMessagesController : AdminBaseController
{
    private readonly ApplicationDbContext _context;

    public PatientMessagesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(bool? handled)
    {
        var query = _context.PatientMessages
            .AsNoTracking()
            .Include(message => message.PatientProfile)
                .ThenInclude(profile => profile!.ApplicationUser)
            .AsQueryable();

        if (handled.HasValue)
        {
            query = query.Where(message => message.IsRead == handled.Value);
        }

        ViewBag.Handled = handled;
        var messages = await query
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
    public async Task<IActionResult> SetHandled(int id, bool handled)
    {
        var message = await _context.PatientMessages.FindAsync(id);
        if (message is null)
        {
            return NotFound();
        }

        message.IsRead = handled;
        await _context.SaveChangesAsync();

        TempData["Success"] = handled
            ? "Patient message marked as reviewed."
            : "Patient message returned to the review queue.";

        return RedirectToAction(nameof(Details), new { id });
    }
}
