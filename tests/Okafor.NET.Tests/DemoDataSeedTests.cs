using Microsoft.Extensions.Configuration;
using Okafor_.NET.Seed;

namespace Okafor_.NET.Tests;

public sealed class DemoDataSeedTests
{
    private static IConfiguration Configuration(string? demoDataEnabled = null)
    {
        var values = new Dictionary<string, string?>();
        if (demoDataEnabled is not null)
        {
            values["DemoData:Enabled"] = demoDataEnabled;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Theory]
    [InlineData("Development", true)]
    [InlineData("Staging", false)]
    [InlineData("Production", false)]
    [InlineData("E2E", false)]
    [InlineData("Testing", false)]
    public void ShouldSeed_DefaultsToDevelopmentOnly(string environmentName, bool expected)
    {
        var environment = new TestWebHostEnvironment
        {
            EnvironmentName = environmentName
        };

        var result = DemoDataSeed.ShouldSeed(environment, Configuration());

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ShouldSeed_AllowsExplicitOptInForAPrivateStagingHost()
    {
        var environment = new TestWebHostEnvironment
        {
            EnvironmentName = "Staging"
        };

        var result = DemoDataSeed.ShouldSeed(environment, Configuration("true"));

        Assert.True(result);
    }

    [Fact]
    public void ShouldSeed_LetsTheHostedPreviewDisableDemoDataExplicitly()
    {
        var environment = new TestWebHostEnvironment
        {
            EnvironmentName = "Staging"
        };

        var result = DemoDataSeed.ShouldSeed(environment, Configuration("false"));

        Assert.False(result);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("E2E")]
    [InlineData("Testing")]
    public void ShouldSeed_NeverSeedsPublicOrAutomatedHostsEvenWhenOptedIn(string environmentName)
    {
        var environment = new TestWebHostEnvironment
        {
            EnvironmentName = environmentName
        };

        var result = DemoDataSeed.ShouldSeed(environment, Configuration("true"));

        Assert.False(result);
    }

    [Theory]
    [InlineData("CHANGE_ME_USE_USER_SECRETS", true)]
    [InlineData("CHANGE_VIA_USER_SECRETS", true)]
    [InlineData("REPLACE_WITH_PAYSTACK_SECRET_KEY", true)]
    [InlineData("change_me_use_user_secrets", true)]
    [InlineData("a-real-operator-chosen-password", false)]
    public void IsPlaceholderSecret_MatchesEveryShippedPlaceholderConvention(string value, bool expected)
    {
        Assert.Equal(expected, IdentitySeed.IsPlaceholderSecret(value));
    }
}
