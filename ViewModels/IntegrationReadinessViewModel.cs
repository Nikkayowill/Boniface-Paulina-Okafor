namespace Okafor_.NET.ViewModels;

public sealed class IntegrationReadinessViewModel
{
    public string EnvironmentName { get; init; } = string.Empty;
    public IReadOnlyList<IntegrationReadinessItemViewModel> Integrations { get; init; } = [];
    public int ConfiguredCount => Integrations.Count(item => item.IsConfigured);
    public int RequiredCount => Integrations.Count(item => item.IsRequiredForLaunch);
    public int RequiredConfiguredCount => Integrations.Count(item => item.IsRequiredForLaunch && item.IsConfigured);
}

public sealed class IntegrationReadinessItemViewModel
{
    public string Name { get; init; } = string.Empty;
    public string Purpose { get; init; } = string.Empty;
    public string ActivationMode { get; init; } = string.Empty;
    public string SetupHint { get; init; } = string.Empty;
    public bool IsRequiredForLaunch { get; init; }
    public bool IsConfigured { get; init; }
    public IReadOnlyList<string> MissingKeys { get; init; } = [];
}
