using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.Services;

namespace Okafor_.NET.Tests.Integration.SqlServer;

[Collection(SqlServerIntegrationCollection.Name)]
[Trait("Category", "DatabaseIntegration")]
public sealed class OperationalReadinessIntegrationTests : SqlServerIntegrationTestBase
{
    public OperationalReadinessIntegrationTests(SqlServerIntegrationFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task AdminReadiness_TracksConfirmedAdminRoleAssignmentInSqlServer()
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(Fixture.ConnectionString));

        await using var provider = services.BuildServiceProvider();
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
                Id = "sql-admin-role",
                Name = "Admin",
                NormalizedName = "ADMIN"
            });
            database.Users.Add(new ApplicationUser
            {
                Id = "sql-admin-user",
                UserName = "admin@example.test",
                NormalizedUserName = "ADMIN@EXAMPLE.TEST",
                Email = "admin@example.test",
                NormalizedEmail = "ADMIN@EXAMPLE.TEST",
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString("N")
            });
            database.UserRoles.Add(new IdentityUserRole<string>
            {
                RoleId = "sql-admin-role",
                UserId = "sql-admin-user"
            });
            await database.SaveChangesAsync();
        }

        var readyResult = await healthCheck.CheckHealthAsync(new HealthCheckContext());
        Assert.Equal(HealthStatus.Healthy, readyResult.Status);
    }
}
