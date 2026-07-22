namespace Okafor_.NET.Services;

public enum LaunchFeature
{
    BillPayments,
    PatientDocuments
}

public interface ILaunchFeatureAvailability
{
    bool IsEnabled(LaunchFeature feature);
}

public sealed class LaunchFeatureAvailability : ILaunchFeatureAvailability
{
    private readonly IReadOnlyDictionary<LaunchFeature, bool> _availability;

    public LaunchFeatureAvailability(
        IConfiguration configuration,
        IHostEnvironment environment,
        PaymentProviderMode paymentProviderMode)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var billPaymentsConfigured = configuration.GetValue<bool?>("LaunchFeatures:BillPayments");
        var patientDocumentsConfigured = configuration.GetValue<bool?>("LaunchFeatures:PatientDocuments");

        _availability = new Dictionary<LaunchFeature, bool>
        {
            // An override may turn payment off, but cannot expose a disabled provider.
            [LaunchFeature.BillPayments] = paymentProviderMode != PaymentProviderMode.Disabled &&
                billPaymentsConfigured != false,
            // Local/test storage is useful for demos. Production storage requires an explicit decision.
            [LaunchFeature.PatientDocuments] = patientDocumentsConfigured ?? !environment.IsProduction()
        };
    }

    public bool IsEnabled(LaunchFeature feature) => _availability[feature];
}
