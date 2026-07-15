namespace Okafor_.NET.Services;

public enum NotificationProviderMode
{
    Lean,
    AfricasTalking,
    Composite
}

public static class NotificationProviderSelection
{
    public static NotificationProviderMode Resolve(IConfiguration configuration)
    {
        var provider = configuration["Notifications:Provider"];

        if (string.Equals(provider, "AfricasTalking", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(provider, "Sms", StringComparison.OrdinalIgnoreCase))
        {
            return NotificationProviderMode.AfricasTalking;
        }

        if (IntegrationConfiguration.IsAutoProvider(configuration, "Notifications:Provider") ||
            string.Equals(provider, "Composite", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(provider, "All", StringComparison.OrdinalIgnoreCase))
        {
            return NotificationProviderMode.Composite;
        }

        return NotificationProviderMode.Lean;
    }
}
