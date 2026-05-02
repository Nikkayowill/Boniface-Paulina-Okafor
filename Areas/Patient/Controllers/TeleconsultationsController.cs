using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.ViewModels;

namespace Okafor_.NET.Areas.Patient.Controllers;

public class TeleconsultationsController : PatientBaseController
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public TeleconsultationsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;
        var user = await _userManager.FindByIdAsync(userId);

        // Match by userId (authenticated submissions) or by email (guest submissions later linked)
        var requests = await _context.TeleconsultationRequests
            .AsNoTracking()
            .Where(r => r.ApplicationUserId == userId ||
                        (user != null && r.Email == user.Email))
            .Include(r => r.Department)
            .Include(r => r.Doctor)
            .OrderByDescending(r => r.PreferredDate)
            .Select(r => new PortalTeleconsultationViewModel
            {
                Id = r.Id,
                PreferredDate = r.PreferredDate,
                PreferredTime = r.PreferredTime,
                Department = r.Department != null ? r.Department.Name : "—",
                Doctor = r.Doctor != null ? r.Doctor.FullName : null,
                ConsultationType = r.ConsultationType.ToString(),
                Status = r.Status.ToString(),
                MeetingLink = r.MeetingLink,
                AdminNotes = r.AdminNotes,
                Reason = r.Reason
            })
            .ToListAsync();

        return View(requests);
    }
}
