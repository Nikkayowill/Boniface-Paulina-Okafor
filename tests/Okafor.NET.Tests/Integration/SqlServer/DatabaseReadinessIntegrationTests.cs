using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Okafor_.NET.Data;
using Okafor_.NET.Services;

namespace Okafor_.NET.Tests.Integration.SqlServer;

[Collection(SqlServerIntegrationCollection.Name)]
[Trait("Category", "DatabaseIntegration")]
public sealed class DatabaseReadinessIntegrationTests : SqlServerIntegrationTestBase
{
    public DatabaseReadinessIntegrationTests(SqlServerIntegrationFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task HealthCheck_ReportsHealthyOnlyAfterMigrationsAreCurrent()
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(Fixture.ConnectionString));
        services.AddSingleton<SqlServerHealthCheck>();

        await using var provider = services.BuildServiceProvider();
        var healthCheck = provider.GetRequiredService<SqlServerHealthCheck>();

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("schema is current", result.Description);
    }
}
