namespace Okafor_.NET.Services;

public enum PaymentProviderMode
{
    Mock,
    Paystack
}

public static class PaymentProviderSelection
{
    public static PaymentProviderMode Resolve(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var provider = configuration["Payments:Provider"];
        var isAuto = IntegrationConfiguration.IsAutoProvider(configuration, "Payments:Provider");
        var isMock = string.Equals(provider, "Mock", StringComparison.OrdinalIgnoreCase);
        var isPaystack = string.Equals(provider, "Paystack", StringComparison.OrdinalIgnoreCase);
        var hasTestKey = IntegrationConfiguration.HasPaystackTestSecretKey(configuration);
        var hasLiveKey = IntegrationConfiguration.HasPaystackLiveSecretKey(configuration);

        if (!isAuto && !isMock && !isPaystack)
        {
            throw new InvalidOperationException(
                $"Unsupported Payments:Provider '{provider}'. Use Auto, Mock, or Paystack.");
        }

        if (environment.IsProduction())
        {
            if (isMock)
            {
                throw new InvalidOperationException(
                    "Mock payments are not allowed in Production. Configure Paystack with a live secret key.");
            }

            if (!hasLiveKey)
            {
                throw new InvalidOperationException(
                    "Production payments require a valid Paystack live secret key (sk_live_...).");
            }

            return PaymentProviderMode.Paystack;
        }

        if (hasLiveKey)
        {
            throw new InvalidOperationException(
                "A Paystack live secret key cannot be used outside Production. Configure a test key instead.");
        }

        if (isPaystack && !hasTestKey)
        {
            throw new InvalidOperationException(
                "Paystack payments outside Production require a valid test secret key (sk_test_...).");
        }

        return isPaystack || isAuto && hasTestKey
            ? PaymentProviderMode.Paystack
            : PaymentProviderMode.Mock;
    }
}
