using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Okafor_.NET.Data;

namespace Okafor_.NET.Seed;

public static class DemoDataSeed
{
    /// <summary>
    /// Decides whether the fictional clinical, news, and appointment records may be seeded.
    /// The hosted preview runs the public launch site under the Staging environment name, so
    /// Staging can no longer imply demo data. Demo seeding is on by default only in
    /// Development; any other host must opt in explicitly with DemoData:Enabled.
    /// </summary>
    public static bool ShouldSeed(IHostEnvironment environment, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(configuration);

        // Fictional patient and clinician records must never reach a public or automated host.
        if (environment.IsProduction() ||
            environment.IsEnvironment("E2E") ||
            environment.IsEnvironment("Testing"))
        {
            return false;
        }

        var explicitlyConfigured = configuration.GetValue<bool?>("DemoData:Enabled");
        if (explicitlyConfigured is not null)
        {
            return explicitlyConfigured.Value;
        }

        return environment.IsDevelopment();
    }

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await NewsDataSeed.SeedAsync(context);
        await AppointmentDataSeed.SeedAsync(context);
    }
}
