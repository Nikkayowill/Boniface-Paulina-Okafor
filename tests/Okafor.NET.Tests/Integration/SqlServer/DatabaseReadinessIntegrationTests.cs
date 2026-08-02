using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
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
        services.AddSingleton<DatabaseHealthCheck>();

        await using var provider = services.BuildServiceProvider();
        var healthCheck = provider.GetRequiredService<DatabaseHealthCheck>();

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("schema is current", result.Description);
    }

    [Fact]
    public async Task DataProtectionKey_CanBePersistedAcrossDbContextInstances()
    {
        var friendlyName = $"integration-{Guid.NewGuid():N}";

        await using (var writeContext = Fixture.CreateDbContext())
        {
            writeContext.DataProtectionKeys.Add(new DataProtectionKey
            {
                FriendlyName = friendlyName,
                Xml = "<key id=\"integration-test\" />"
            });
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = Fixture.CreateDbContext();
        var savedKey = await readContext.DataProtectionKeys
            .AsNoTracking()
            .SingleAsync(key => key.FriendlyName == friendlyName);

        Assert.Equal("<key id=\"integration-test\" />", savedKey.Xml);
    }
}
