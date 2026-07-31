using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;

namespace Okafor_.NET.Seed;

public static class IdentitySeed
{
    /// <summary>
    /// Detects the placeholder convention used across the shipped appsettings files
    /// (for example CHANGE_ME_USE_USER_SECRETS, CHANGE_VIA_USER_SECRETS, REPLACE_WITH_...).
    /// </summary>
    public static bool IsPlaceholderSecret(string value) =>
        value.StartsWith("CHANGE_", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("REPLACE_WITH", StringComparison.OrdinalIgnoreCase);

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

        // Skip seeding while the password is still a shipped placeholder. Every
        // appsettings file uses the CHANGE_/REPLACE_WITH convention, so match the
        // convention rather than one literal value: seeding a known password would
        // create a real Admin account whose credentials are published in this repo.
        if (!string.IsNullOrWhiteSpace(adminPassword) && IsPlaceholderSecret(adminPassword))
        {
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(IdentitySeed));
            logger.LogWarning(
                "Skipping admin user seed: SeedAdmin:Password is still the placeholder '{Placeholder}'. " +
                "Configure SeedAdmin:Password through user secrets or the deployment secret store.",
                adminPassword);
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
