using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.Services;

namespace Okafor_.NET.Tests;

public sealed class AdminAccountHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_RequiresAConfirmedAdminAccount()
    {
        await using var provider = BuildServiceProvider();
        var healthCheck = new AdminAccountHealthCheck(
            provider.GetRequiredService<IServiceScopeFactory>(),
            new TestWebHostEnvironment { EnvironmentName = Environments.Production });

        var missingResult = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, missingResult.Status);

        await using (var scope = provider.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            database.Roles.Add(new IdentityRole
            {
                Id = "admin-role",
                Name = "Admin",
                NormalizedName = "ADMIN"
            });
            database.Users.Add(new ApplicationUser
            {
                Id = "admin-user",
                UserName = "admin@example.test",
                NormalizedUserName = "ADMIN@EXAMPLE.TEST",
                Email = "admin@example.test",
                NormalizedEmail = "ADMIN@EXAMPLE.TEST",
                EmailConfirmed = true
            });
            database.UserRoles.Add(new IdentityUserRole<string>
            {
                RoleId = "admin-role",
                UserId = "admin-user"
            });
            await database.SaveChangesAsync();
        }

        var readyResult = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, readyResult.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_DoesNotRequireSeededIdentityInAutomatedTestHost()
    {
        await using var provider = BuildServiceProvider();
        var healthCheck = new AdminAccountHealthCheck(
            provider.GetRequiredService<IServiceScopeFactory>(),
            new TestWebHostEnvironment { EnvironmentName = "Testing" });

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var databaseName = $"admin-health-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        return services.BuildServiceProvider();
    }
}
