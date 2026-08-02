namespace Okafor_.NET.Tests;

public sealed class HostedSmokeFactAttribute : FactAttribute
{
    public HostedSmokeFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OKAFOR_BASE_URL")))
        {
            Skip = "Set OKAFOR_BASE_URL or run RUN_SMOKE=1 ./scripts/verify-backend.sh.";
        }
    }
}

public sealed class HostedSmokeTheoryAttribute : TheoryAttribute
{
    public HostedSmokeTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OKAFOR_BASE_URL")))
        {
            Skip = "Set OKAFOR_BASE_URL or run RUN_SMOKE=1 ./scripts/verify-backend.sh.";
        }
    }
}
