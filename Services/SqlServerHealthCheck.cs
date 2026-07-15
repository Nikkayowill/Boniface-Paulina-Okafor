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
            return await database.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("SQL Server is reachable.")
                : HealthCheckResult.Unhealthy("SQL Server is not reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQL Server readiness check failed.", ex);
        }
    }
}
