using Microsoft.Extensions.Configuration;
using Okafor_.NET.Services;

namespace Okafor_.NET.Tests;

public sealed class PaymentProviderSelectionTests
{
    [Theory]
    [InlineData("Development", "Mock", null, PaymentProviderMode.Mock)]
    [InlineData("Development", "Auto", null, PaymentProviderMode.Mock)]
    [InlineData("Testing", null, null, PaymentProviderMode.Mock)]
    [InlineData("E2E", "Auto", "sk_test_e2e", PaymentProviderMode.Paystack)]
    [InlineData("Development", "Paystack", "sk_test_development", PaymentProviderMode.Paystack)]
    [InlineData("Staging", "Auto", "sk_test_staging", PaymentProviderMode.Paystack)]
    [InlineData("Production", "Auto", "sk_live_production", PaymentProviderMode.Paystack)]
    [InlineData("Production", "Paystack", "sk_live_production", PaymentProviderMode.Paystack)]
    public void Resolve_UsesOnlyTheProviderSafeForTheEnvironment(
        string environmentName,
        string? configuredProvider,
        string? secretKey,
        PaymentProviderMode expected)
    {
        var result = PaymentProviderSelection.Resolve(
            BuildConfiguration(configuredProvider, secretKey),
            BuildEnvironment(environmentName));

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Production", "Mock", null)]
    [InlineData("Production", "Auto", null)]
    [InlineData("Production", "Auto", "sk_test_not-live")]
    [InlineData("Production", "Auto", "sk_live_")]
    [InlineData("Production", "Auto", "SK_LIVE_NOT_CASE_VALID")]
    [InlineData("Production", "Paystack", "REPLACE_WITH_PAYSTACK_SECRET_KEY")]
    [InlineData("Development", "Paystack", null)]
    [InlineData("Staging", "Paystack", "sk_live_not-staging")]
    [InlineData("Development", "unsupported", "sk_test_valid")]
    public void Resolve_RejectsUnsafeOrUnsupportedConfiguration(
        string environmentName,
        string? configuredProvider,
        string? secretKey)
    {
        Action action = () =>
        {
            _ = PaymentProviderSelection.Resolve(
                BuildConfiguration(configuredProvider, secretKey),
                BuildEnvironment(environmentName));
        };

        var exception = Assert.Throws<InvalidOperationException>(action);

        if (!string.IsNullOrEmpty(secretKey) && secretKey.Length > "sk_live_".Length)
        {
            Assert.DoesNotContain(secretKey, exception.Message, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("sk_test_example", true)]
    [InlineData("sk_live_example", false)]
    [InlineData("REPLACE_WITH_PAYSTACK_SECRET_KEY", true)]
    [InlineData("not-a-paystack-key", true)]
    [InlineData(null, true)]
    public void PaystackGateway_IsSandbox_RequiresRecognizedLiveKey(
        string? secretKey,
        bool expected)
    {
        using var httpClient = new HttpClient();
        var gateway = new PaystackPaymentGateway(
            httpClient,
            BuildConfiguration("Paystack", secretKey));

        Assert.Equal(expected, gateway.IsSandbox);
    }

    [Theory]
    [InlineData("http://api.paystack.co")]
    [InlineData("https://paystack.example.test")]
    [InlineData("https://api.paystack.co.example.test")]
    [InlineData("https://user@api.paystack.co")]
    [InlineData("https://api.paystack.co/alternate")]
    public void PaystackGateway_RejectsNonOfficialApiEndpoint(string baseUrl)
    {
        using var httpClient = new HttpClient();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Payments:Paystack:SecretKey"] = "sk_test_example",
                ["Payments:Paystack:BaseUrl"] = baseUrl
            })
            .Build();

        var action = () => new PaystackPaymentGateway(httpClient, configuration);

        Assert.Throws<InvalidOperationException>(action);
    }

    private static IConfiguration BuildConfiguration(string? provider, string? secretKey) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Payments:Provider"] = provider,
                ["Payments:Paystack:SecretKey"] = secretKey,
                ["Payments:Paystack:BaseUrl"] = "https://api.paystack.co"
            })
            .Build();

    private static TestWebHostEnvironment BuildEnvironment(string environmentName) =>
        new()
        {
            EnvironmentName = environmentName
        };
}
