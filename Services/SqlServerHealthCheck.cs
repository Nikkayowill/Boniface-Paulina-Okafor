using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Okafor_.NET.Data;

namespace Okafor_.NET.Services;

public sealed class SqlServerHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SqlServerHealthCheck(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            if (!await database.Database.CanConnectAsync(cancellationToken))
            {
                return HealthCheckResult.Unhealthy("SQL Server is not reachable.");
            }

            if (database.Database.IsRelational())
            {
                var pendingMigrations = (await database.Database
                        .GetPendingMigrationsAsync(cancellationToken))
                    .Count();
                if (pendingMigrations > 0)
                {
                    return HealthCheckResult.Unhealthy(
                        $"Database schema is not release-ready: {pendingMigrations} migration(s) are pending.");
                }
            }

            return HealthCheckResult.Healthy("SQL Server is reachable and the schema is current.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQL Server readiness check failed.", ex);
        }
    }
}
