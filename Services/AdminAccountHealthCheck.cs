using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Okafor_.NET.Data;

namespace Okafor_.NET.Services;

public sealed class AdminAccountHealthCheck(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment environment) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (environment.IsEnvironment("Testing") || environment.IsEnvironment("E2E"))
        {
            return HealthCheckResult.Healthy("Admin account readiness is not required in automated test hosts.");
        }

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var hasConfirmedAdmin = await (
                from userRole in database.UserRoles
                join role in database.Roles on userRole.RoleId equals role.Id
                join user in database.Users on userRole.UserId equals user.Id
                where role.NormalizedName == "ADMIN" && user.EmailConfirmed
                select user.Id)
                .AnyAsync(cancellationToken);

            return hasConfirmedAdmin
                ? HealthCheckResult.Healthy("A confirmed Admin account is available.")
                : HealthCheckResult.Unhealthy(
                    "No confirmed Admin account is available. Configure SeedAdmin credentials before launch.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Admin account readiness check failed.", ex);
        }
    }
}
