using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;

namespace Okafor_.NET.Areas.Admin.Controllers;

[Authorize(Roles = "Admin")]
public class ContactSubmissionsController : AdminBaseController
{
    private readonly ApplicationDbContext _context;

    public ContactSubmissionsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var submissions = await _context.ContactSubmissions
            .AsNoTracking()
            .OrderByDescending(c => c.SubmittedAt)
            .ToListAsync();

        return View(submissions);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
            return NotFound();

        var submission = await _context.ContactSubmissions
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (submission is null)
            return NotFound();

        return View(submission);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var submission = await _context.ContactSubmissions.FindAsync(id);
        if (submission is not null)
        {
            _context.ContactSubmissions.Remove(submission);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}
