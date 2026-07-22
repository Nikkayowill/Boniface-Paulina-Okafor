using Microsoft.Extensions.Hosting;
using Okafor_.NET.Data;

namespace Okafor_.NET.Seed;

public static class DemoDataSeed
{
    public static bool ShouldSeed(IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);

        return environment.IsDevelopment() || environment.IsStaging();
    }

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await ClinicalDataSeed.SeedAsync(context);
        await NewsDataSeed.SeedAsync(context);
        await AppointmentDataSeed.SeedAsync(context);
    }
}
