using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Okafor_.NET.Services;

namespace Okafor_.NET.Tests;

public sealed class LaunchFeatureAvailabilityTests
{
    [Theory]
    [InlineData(PaymentProviderMode.Disabled, false)]
    [InlineData(PaymentProviderMode.Paystack, true)]
    public void BillPayments_RequireAnAvailableProvider(
        PaymentProviderMode providerMode,
        bool expected)
    {
        var availability = Create("Production", providerMode);

        Assert.Equal(expected, availability.IsEnabled(LaunchFeature.BillPayments));
    }

    [Fact]
    public void BillPayments_CanBeTurnedOffEvenWhenProviderIsAvailable()
    {
        var availability = Create(
            "Production",
            PaymentProviderMode.Paystack,
            ("LaunchFeatures:BillPayments", "false"));

        Assert.False(availability.IsEnabled(LaunchFeature.BillPayments));
    }

    [Fact]
    public void BillPayments_CannotBeTurnedOnOverADisabledProvider()
    {
        var availability = Create(
            "Production",
            PaymentProviderMode.Disabled,
            ("LaunchFeatures:BillPayments", "true"));

        Assert.False(availability.IsEnabled(LaunchFeature.BillPayments));
    }

    [Theory]
    [InlineData("Development", true)]
    [InlineData("Testing", true)]
    [InlineData("Production", false)]
    public void PatientDocuments_DefaultToOffOnlyInProduction(
        string environmentName,
        bool expected)
    {
        var availability = Create(environmentName, PaymentProviderMode.Mock);

        Assert.Equal(expected, availability.IsEnabled(LaunchFeature.PatientDocuments));
    }

    [Fact]
    public void PatientDocuments_CanBeExplicitlyApprovedForProduction()
    {
        var availability = Create(
            "Production",
            PaymentProviderMode.Disabled,
            ("LaunchFeatures:PatientDocuments", "true"));

        Assert.True(availability.IsEnabled(LaunchFeature.PatientDocuments));
    }

    private static LaunchFeatureAvailability Create(
        string environmentName,
        PaymentProviderMode paymentProviderMode,
        params (string Key, string? Value)[] settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings.ToDictionary(setting => setting.Key, setting => setting.Value))
            .Build();

        return new LaunchFeatureAvailability(
            configuration,
            new TestHostEnvironment { EnvironmentName = environmentName },
            paymentProviderMode);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Okafor.NET.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
