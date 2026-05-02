using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Models;
using Okafor_.NET.ViewModels;

namespace Okafor_.NET.Areas.Admin.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : AdminBaseController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.AsNoTracking().ToListAsync();

        var viewModels = new List<UserListItemViewModel>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            viewModels.Add(new UserListItemViewModel
            {
                Id       = user.Id,
                Email    = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                Roles    = roles.ToList()
            });
        }

        return View(viewModels);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateUserViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (model.Role != "Admin" && model.Role != "Staff")
        {
            ModelState.AddModelError(nameof(model.Role), "Role must be Admin or Staff.");
            return View(model);
        }

        if (!await _roleManager.RoleExistsAsync(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "The selected role does not exist.");
            return View(model);
        }

        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "A user with this email already exists.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName       = model.Email,
            Email          = model.Email,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, model.Role);

        TempData["SuccessMessage"] = $"User {model.Email} created and assigned to {model.Role}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> EditRoles(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var model = new EditRolesViewModel
        {
            UserId  = user.Id,
            Email   = user.Email ?? string.Empty,
            IsAdmin = roles.Contains("Admin"),
            IsStaff = roles.Contains("Staff")
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRoles(EditRolesViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user is null)
            return NotFound();

        var currentRoles = await _userManager.GetRolesAsync(user);

        // Prevent removing Admin from the last remaining admin
        if (currentRoles.Contains("Admin") && !model.IsAdmin)
        {
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            if (adminUsers.Count <= 1)
            {
                ModelState.AddModelError(string.Empty,
                    "Cannot remove Admin role — this is the only Admin account.");
                return View(model);
            }
        }

        // Apply Admin role change
        if (model.IsAdmin && !currentRoles.Contains("Admin"))
            await _userManager.AddToRoleAsync(user, "Admin");
        else if (!model.IsAdmin && currentRoles.Contains("Admin"))
            await _userManager.RemoveFromRoleAsync(user, "Admin");

        // Apply Staff role change
        if (model.IsStaff && !currentRoles.Contains("Staff"))
            await _userManager.AddToRoleAsync(user, "Staff");
        else if (!model.IsStaff && currentRoles.Contains("Staff"))
            await _userManager.RemoveFromRoleAsync(user, "Staff");

        TempData["SuccessMessage"] = $"Roles updated for {user.Email}.";
        return RedirectToAction(nameof(Index));
    }
}
