using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Okafor_.NET.Areas.Admin.Controllers;
using Okafor_.NET.Services;
using Okafor_.NET.ViewModels;

namespace Okafor_.NET.Tests;

public sealed class IntegrationReadinessControllerTests
{
    [Fact]
    public void Index_DoesNotRequireOptionalInfrastructureWhenFeaturesAreDisabled()
    {
        var controller = CreateController(
            onlineDonationsEnabled: true,
            billPaymentsEnabled: false,
            patientDocumentsEnabled: false);

        var view = Assert.IsType<ViewResult>(controller.Index());
        var model = Assert.IsType<IntegrationReadinessViewModel>(view.Model);

        Assert.False(Find(model, "Patient document storage").IsRequiredForLaunch);
        Assert.True(Find(model, "SMTP email").IsRequiredForLaunch);
    }

    [Fact]
    public void Index_MakesOptionalInfrastructureRequiredOnlyWhenFeatureIsEnabled()
    {
        var controller = CreateController(
            onlineDonationsEnabled: true,
            billPaymentsEnabled: true,
            patientDocumentsEnabled: true,
            ("PatientDocuments:StorageRoot", "/data/patient-documents"),
            ("PatientDocuments:PersistentStorageConfirmed", "true"));

        var view = Assert.IsType<ViewResult>(controller.Index());
        var model = Assert.IsType<IntegrationReadinessViewModel>(view.Model);

        Assert.True(Find(model, "Patient document storage").IsRequiredForLaunch);
        Assert.True(Find(model, "Patient document storage").IsConfigured);
    }

    private static IntegrationsController CreateController(
        bool onlineDonationsEnabled,
        bool billPaymentsEnabled,
        bool patientDocumentsEnabled,
        params (string Key, string? Value)[] extraSettings)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Authentication:RequireConfirmedAccount"] = "true",
            ["Hospital:Email"] = "info@hospital.example",
            ["Hospital:EmergencyNumbers"] = "112",
            ["Payments:Provider"] = onlineDonationsEnabled || billPaymentsEnabled ? "Mock" : "Disabled"
        };
        foreach (var (key, value) in extraSettings)
        {
            settings[key] = value;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        return new IntegrationsController(
            configuration,
            new TestWebHostEnvironment { EnvironmentName = Environments.Production },
            new TestLaunchFeatures(onlineDonationsEnabled, billPaymentsEnabled, patientDocumentsEnabled));
    }

    private static IntegrationReadinessItemViewModel Find(
        IntegrationReadinessViewModel model,
        string name) => model.Integrations.Single(item => item.Name == name);

    private sealed class TestLaunchFeatures(
        bool onlineDonationsEnabled,
        bool billPaymentsEnabled,
        bool patientDocumentsEnabled) : ILaunchFeatureAvailability
    {
        public bool IsEnabled(LaunchFeature feature) => feature switch
        {
            LaunchFeature.OnlineDonations => onlineDonationsEnabled,
            LaunchFeature.BillPayments => billPaymentsEnabled,
            LaunchFeature.PatientDocuments => patientDocumentsEnabled,
            _ => false
        };
    }
}
