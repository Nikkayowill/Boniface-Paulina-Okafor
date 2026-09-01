using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.Services;
using Okafor_.NET.ViewModels;

namespace Okafor_.NET.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Staff")]
[RequireLaunchFeature(LaunchFeature.BillPayments)]
public class BillPaymentsController : Controller
{
    private readonly ApplicationDbContext _context;

    public BillPaymentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(BillPaymentStatus? status = null, string? query = null, int page = 1)
    {
        const int pageSize = AdminBaseController.DefaultPageSize;
        if (page < 1) page = 1;

        var payments = _context.BillPayments.AsNoTracking().AsQueryable();

        if (status.HasValue)
        {
            payments = payments.Where(p => p.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalized = query.Trim();
            payments = payments.Where(p =>
                p.InvoiceNumber.Contains(normalized) ||
                p.PatientName.Contains(normalized) ||
                p.PatientEmail.Contains(normalized));
        }

        var totalCount = await payments.CountAsync();

        var items = await payments
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewData["Status"] = new SelectList(Enum.GetValues<BillPaymentStatus>(), status);
        ViewData["Query"] = query?.Trim();
        return View(new PagedResult<BillPayment>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var payment = await _context.BillPayments
            .AsNoTracking()
            .Include(p => p.ApplicationUser)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment is null)
        {
            return NotFound();
        }

        return View(payment);
    }
}
