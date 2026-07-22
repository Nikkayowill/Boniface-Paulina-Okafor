using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Okafor_.NET.Services;

namespace Okafor_.NET.Tests;

public sealed class DatabaseMigrationPolicyTests
{
    [Theory]
    [InlineData("Development", null, true)]
    [InlineData("Production", null, false)]
    [InlineData("Production", "false", false)]
    [InlineData("Production", "true", true)]
    public void ShouldApplyOnStartup_RequiresDevelopmentOrExplicitOptIn(
        string environmentName,
        string? configuredValue,
        bool expected)
    {
        var values = new Dictionary<string, string?>
        {
            ["Database:ApplyMigrationsOnStartup"] = configuredValue
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        var environment = new TestWebHostEnvironment
        {
            EnvironmentName = environmentName
        };

        var result = DatabaseMigrationPolicy.ShouldApplyOnStartup(
            configuration,
            environment);

        Assert.Equal(expected, result);
    }
}
