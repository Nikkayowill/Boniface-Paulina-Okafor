namespace Okafor_.NET.Services;

public static class IntegrationConfiguration
{
    public static bool HasPaystackSecretKey(IConfiguration configuration)
    {
        return HasRealValue(configuration["Payments:Paystack:SecretKey"]);
    }

    public static bool HasSmtpSettings(IConfiguration configuration)
    {
        return HasRealValue(configuration["Email:SmtpHost"]) &&
            HasRealValue(configuration["Email:FromAddress"]);
    }

    public static bool HasAfricasTalkingCredentials(IConfiguration configuration)
    {
        return HasRealValue(configuration["Notifications:AfricasTalking:ApiKey"]) &&
            HasRealValue(configuration["Notifications:AfricasTalking:Username"]);
    }

    public static bool HasWhatsAppCredentials(IConfiguration configuration)
    {
        return HasRealValue(configuration["Notifications:WhatsApp:PhoneNumberId"]) &&
            HasRealValue(configuration["Notifications:WhatsApp:AccessToken"]);
    }

    public static bool HasWhatsAppAppSecret(IConfiguration configuration)
    {
        return HasRealValue(configuration["Notifications:WhatsApp:AppSecret"]);
    }

    public static bool HasVapidSettings(IConfiguration configuration)
    {
        return HasRealValue(configuration["VapidKeys:PublicKey"]) &&
            HasRealValue(configuration["VapidKeys:PrivateKey"]) &&
            HasRealValue(configuration["VapidKeys:Subject"]);
    }

    public static bool IsEnabledOrAuto(IConfiguration configuration, string key, bool autoValue)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value) ||
            string.Equals(value, "Auto", StringComparison.OrdinalIgnoreCase))
        {
            return autoValue;
        }

        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsProvider(IConfiguration configuration, string key, string provider)
    {
        return string.Equals(configuration[key], provider, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsAutoProvider(IConfiguration configuration, string key)
    {
        var provider = configuration[key];
        return string.IsNullOrWhiteSpace(provider) ||
            string.Equals(provider, "Auto", StringComparison.OrdinalIgnoreCase);
    }

    public static bool HasRealValue(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
            !value.StartsWith("REPLACE_WITH_", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase);
    }
}
