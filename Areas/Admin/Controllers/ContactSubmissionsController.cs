using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.ViewModels;

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
    public async Task<IActionResult> Index(int page = 1)
    {
        const int pageSize = DefaultPageSize;
        if (page < 1) page = 1;

        var baseQuery = _context.ContactSubmissions.AsNoTracking();

        var totalCount = await baseQuery.CountAsync();

        var items = await baseQuery
            .OrderByDescending(c => c.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return View(new PagedResult<ContactSubmission>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
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
