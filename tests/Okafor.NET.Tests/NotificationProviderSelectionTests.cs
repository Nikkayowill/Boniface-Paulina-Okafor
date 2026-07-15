using Microsoft.Extensions.Configuration;
using Okafor_.NET.Services;

namespace Okafor_.NET.Tests;

public sealed class NotificationProviderSelectionTests
{
    [Theory]
    [InlineData("AfricasTalking", NotificationProviderMode.AfricasTalking)]
    [InlineData("africastalking", NotificationProviderMode.AfricasTalking)]
    [InlineData("SMS", NotificationProviderMode.AfricasTalking)]
    [InlineData("Lean", NotificationProviderMode.Lean)]
    [InlineData("Email", NotificationProviderMode.Lean)]
    [InlineData("Composite", NotificationProviderMode.Composite)]
    [InlineData("all", NotificationProviderMode.Composite)]
    [InlineData("Auto", NotificationProviderMode.Composite)]
    [InlineData(null, NotificationProviderMode.Composite)]
    [InlineData("unsupported-provider", NotificationProviderMode.Lean)]
    public void Resolve_MapsConfigurationToSafeProviderMode(
        string? configuredProvider,
        NotificationProviderMode expected)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Notifications:Provider"] = configuredProvider
            })
            .Build();

        var result = NotificationProviderSelection.Resolve(configuration);

        Assert.Equal(expected, result);
    }
}
