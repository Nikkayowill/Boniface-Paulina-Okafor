using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.ViewModels;

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
    public async Task<IActionResult> Index(int page = 1)
    {
        const int pageSize = DefaultPageSize;
        if (page < 1) page = 1;

        var baseQuery = _context.PatientMessages.AsNoTracking();

        var totalCount = await baseQuery.CountAsync();
        ViewData["PendingCount"] = await baseQuery.CountAsync(message => !message.IsRead);

        var items = await baseQuery
            .OrderBy(message => message.IsRead)
            .ThenByDescending(message => message.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(message => new PatientMessageListItemViewModel
            {
                Id = message.Id,
                Subject = message.Subject,
                IsRead = message.IsRead,
                SentAt = message.SentAt,
                PatientName = message.PatientProfile != null ? message.PatientProfile.FullName : null,
                PatientEmail = message.PatientProfile != null && message.PatientProfile.ApplicationUser != null
                    ? message.PatientProfile.ApplicationUser.Email
                    : null
            })
            .ToListAsync();

        return View(new PagedResult<PatientMessageListItemViewModel>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
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
