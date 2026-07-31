namespace Okafor_.NET.Services;

public enum LaunchFeature
{
    OnlineDonations,
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

        var onlineDonationsConfigured = configuration.GetValue<bool?>("LaunchFeatures:OnlineDonations");
        var billPaymentsConfigured = configuration.GetValue<bool?>("LaunchFeatures:BillPayments");
        var patientDocumentsConfigured = configuration.GetValue<bool?>("LaunchFeatures:PatientDocuments");
        var documentStorageRoot = configuration["PatientDocuments:StorageRoot"];
        var persistentDocumentStorageConfirmed =
            configuration.GetValue<bool>("PatientDocuments:PersistentStorageConfirmed");

        _availability = new Dictionary<LaunchFeature, bool>
        {
            [LaunchFeature.OnlineDonations] = paymentProviderMode != PaymentProviderMode.Disabled &&
                onlineDonationsConfigured != false,
            // An override may turn payment off, but cannot expose a disabled provider.
            [LaunchFeature.BillPayments] = paymentProviderMode != PaymentProviderMode.Disabled &&
                billPaymentsConfigured != false,
            // Local/test storage is useful for demos. Production requires an explicit feature decision,
            // a persistent-volume attestation, and a fully qualified private storage path.
            [LaunchFeature.PatientDocuments] = environment.IsProduction()
                ? patientDocumentsConfigured == true &&
                    persistentDocumentStorageConfirmed &&
                    !string.IsNullOrWhiteSpace(documentStorageRoot) &&
                    Path.IsPathFullyQualified(documentStorageRoot)
                : patientDocumentsConfigured != false
        };
    }

    public bool IsEnabled(LaunchFeature feature) => _availability[feature];
}
