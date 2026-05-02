using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;

namespace Okafor_.NET.Seed;

public static class IdentitySeed
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var config = services.GetRequiredService<IConfiguration>();
        var db = services.GetRequiredService<ApplicationDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roles = ["Admin", "Staff", "Patient"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var createRole = await roleManager.CreateAsync(new IdentityRole(role));
                if (!createRole.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to create role: {role}");
                }
            }
        }

        var adminEmail = config["SeedAdmin:Email"];
        var adminPassword = config["SeedAdmin:Password"];

        // Skip seeding if password is still the default placeholder
        if (adminPassword == "CHANGE_ME_USE_USER_SECRETS")
        {
            System.Console.WriteLine("⚠️  Skipping admin user seed: Password is still the default placeholder. Configure SeedAdmin:Password in appsettings or user-secrets to seed an admin user.");
            adminPassword = null;
        }

        if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
        {
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser is null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var createAdmin = await userManager.CreateAsync(adminUser, adminPassword);
                if (!createAdmin.Succeeded)
                {
                    var errors = string.Join("; ", createAdmin.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create configured admin user: {errors}");
                }
            }

            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                var addRole = await userManager.AddToRoleAsync(adminUser, "Admin");
                if (!addRole.Succeeded)
                {
                    throw new InvalidOperationException("Failed to assign Admin role to configured admin user.");
                }
            }
        }

        var patientUserIds = await db.PatientProfiles
            .AsNoTracking()
            .Select(profile => profile.ApplicationUserId)
            .Where(userId => !string.IsNullOrWhiteSpace(userId))
            .Distinct()
            .ToListAsync();

        foreach (var patientUserId in patientUserIds)
        {
            var patientUser = await userManager.FindByIdAsync(patientUserId);
            if (patientUser is null)
            {
                continue;
            }

            if (await userManager.IsInRoleAsync(patientUser, "Patient"))
            {
                continue;
            }

            var addPatientRole = await userManager.AddToRoleAsync(patientUser, "Patient");
            if (!addPatientRole.Succeeded)
            {
                var errors = string.Join("; ", addPatientRole.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Failed to assign Patient role to linked user '{patientUser.Email ?? patientUser.Id}': {errors}");
            }
        }
    }
}
