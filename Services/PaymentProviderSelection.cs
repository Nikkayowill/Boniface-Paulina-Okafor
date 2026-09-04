namespace Okafor_.NET.Services;

public enum PaymentProviderMode
{
    Disabled,
    Mock
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
        var isDisabled = string.Equals(provider, "Disabled", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(provider, "None", StringComparison.OrdinalIgnoreCase);
        var isMock = string.Equals(provider, "Mock", StringComparison.OrdinalIgnoreCase);

        if (!isAuto && !isDisabled && !isMock)
        {
            throw new InvalidOperationException(
                $"Unsupported Payments:Provider '{provider}'. Use Auto, Disabled, or Mock.");
        }

        if (isMock && environment.IsProduction())
        {
            throw new InvalidOperationException(
                "Payments:Provider=Mock is not allowed in Production. MockPaymentGateway always " +
                "reports success without collecting real money. Use Disabled until a real payment " +
                "provider is configured.");
        }

        // No live payment gateway is wired up yet, so Auto (and an explicit Disabled)
        // both resolve to Disabled. Mock is available outside Production for
        // demoing the checkout flow without a real provider configured.
        return isMock ? PaymentProviderMode.Mock : PaymentProviderMode.Disabled;
    }
}
