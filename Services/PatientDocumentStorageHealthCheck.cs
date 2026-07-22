using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Okafor_.NET.Services;

public sealed class PatientDocumentStorageHealthCheck(
    ILaunchFeatureAvailability launchFeatures,
    IWebHostEnvironment environment,
    IOptions<PatientDocumentStorageOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!launchFeatures.IsEnabled(LaunchFeature.PatientDocuments))
        {
            return HealthCheckResult.Healthy("Patient document uploads are disabled.");
        }

        string? probePath = null;
        try
        {
            var storageRoot = PatientDocumentStoragePath.Resolve(environment, options.Value);
            PatientDocumentStoragePath.EnsureOutsideWebRoot(environment, storageRoot);
            Directory.CreateDirectory(storageRoot);

            probePath = Path.Combine(storageRoot, $".readiness-{Guid.NewGuid():N}.tmp");
            await File.WriteAllTextAsync(probePath, "ok", cancellationToken);
            File.Delete(probePath);

            return HealthCheckResult.Healthy("Patient document storage is writable and private.");
        }
        catch (Exception ex)
        {
            try
            {
                if (probePath is not null && File.Exists(probePath))
                    File.Delete(probePath);
            }
            catch
            {
                // Preserve the readiness failure that caused cleanup to run.
            }

            return HealthCheckResult.Unhealthy("Patient document storage is not ready.", ex);
        }
    }
}
