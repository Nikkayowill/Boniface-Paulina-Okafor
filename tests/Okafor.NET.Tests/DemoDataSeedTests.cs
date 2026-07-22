using Okafor_.NET.Seed;

namespace Okafor_.NET.Tests;

public sealed class DemoDataSeedTests
{
    [Theory]
    [InlineData("Development", true)]
    [InlineData("Staging", true)]
    [InlineData("Production", false)]
    [InlineData("E2E", false)]
    [InlineData("Testing", false)]
    public void ShouldSeed_RestrictsDemoRecordsToDevelopmentAndStaging(
        string environmentName,
        bool expected)
    {
        var environment = new TestWebHostEnvironment
        {
            EnvironmentName = environmentName
        };

        var result = DemoDataSeed.ShouldSeed(environment);

        Assert.Equal(expected, result);
    }
}
