namespace Okafor_.NET.Services;

public static class DatabaseMigrationPolicy
{
    public static bool ShouldApplyOnStartup(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        return environment.IsDevelopment() ||
            configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup");
    }
}
