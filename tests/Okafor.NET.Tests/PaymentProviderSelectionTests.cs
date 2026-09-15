using Microsoft.Extensions.Configuration;
using Okafor_.NET.Services;

namespace Okafor_.NET.Tests;

public sealed class PaymentProviderSelectionTests
{
    [Theory]
    [InlineData("Development", "Mock", PaymentProviderMode.Mock)]
    [InlineData("Development", "Auto", PaymentProviderMode.Disabled)]
    [InlineData("Testing", null, PaymentProviderMode.Disabled)]
    [InlineData("Staging", "Mock", PaymentProviderMode.Mock)]
    [InlineData("Production", "Auto", PaymentProviderMode.Disabled)]
    public void Resolve_UsesTheConfiguredProviderForTheEnvironment(
        string environmentName,
        string? configuredProvider,
        PaymentProviderMode expected)
    {
        var result = PaymentProviderSelection.Resolve(
            BuildConfiguration(configuredProvider),
            BuildEnvironment(environmentName));

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void Resolve_AllowsOnlinePaymentsToBeExplicitlyDisabled(string environmentName)
    {
        var result = PaymentProviderSelection.Resolve(
            BuildConfiguration("Disabled"),
            BuildEnvironment(environmentName));

        Assert.Equal(PaymentProviderMode.Disabled, result);
    }

    [Fact]
    public async Task DisabledGateway_NeverReportsPaymentSuccess()
    {
        var gateway = new DisabledPaymentGateway();

        var initialized = await gateway.InitializeAsync(new PaymentInitializeRequest(
            "donor@example.test",
            1000m,
            "NGN",
            "DON-123456",
            "https://hospital.example.test/callback",
            "Hospital support",
            "Test Donor"));
        var verified = await gateway.VerifyAsync("DON-123456");

        Assert.False(initialized.Success);
        Assert.False(initialized.IsSandbox);
        Assert.False(verified.Success);
        Assert.False(verified.IsSandbox);
    }

    [Theory]
    [InlineData("Development", "unsupported")]
    [InlineData("Production", "unsupported")]
    public void Resolve_RejectsUnsupportedConfiguration(
        string environmentName,
        string? configuredProvider)
    {
        Action action = () =>
        {
            _ = PaymentProviderSelection.Resolve(
                BuildConfiguration(configuredProvider),
                BuildEnvironment(environmentName));
        };

        Assert.Throws<InvalidOperationException>(action);
    }

    [Fact]
    public void Resolve_RejectsMockInProduction()
    {
        // MockPaymentGateway always reports success without collecting real money;
        // it must never be reachable in Production, regardless of which real
        // payment provider is or isn't configured.
        Action action = () =>
        {
            _ = PaymentProviderSelection.Resolve(
                BuildConfiguration("Mock"),
                BuildEnvironment("Production"));
        };

        Assert.Throws<InvalidOperationException>(action);
    }

    private static IConfiguration BuildConfiguration(string? provider) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Payments:Provider"] = provider
            })
            .Build();

    private static TestWebHostEnvironment BuildEnvironment(string environmentName) =>
        new()
        {
            EnvironmentName = environmentName
        };
}
