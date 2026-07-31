using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.Services;

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

    public async Task<IActionResult> Index(BillPaymentStatus? status = null, string? query = null)
    {
        var payments = _context.BillPayments
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .AsQueryable();

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

        ViewData["Status"] = new SelectList(Enum.GetValues<BillPaymentStatus>(), status);
        ViewData["Query"] = query?.Trim();
        return View(await payments.ToListAsync());
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
