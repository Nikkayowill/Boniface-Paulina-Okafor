using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Okafor_.NET.Services;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireLaunchFeatureAttribute : TypeFilterAttribute
{
    public RequireLaunchFeatureAttribute(LaunchFeature feature)
        : base(typeof(LaunchFeatureFilter))
    {
        Arguments = [feature];
    }
}

public sealed class LaunchFeatureFilter(
    ILaunchFeatureAvailability availability,
    LaunchFeature feature) : IAsyncResourceFilter
{
    public async Task OnResourceExecutionAsync(
        ResourceExecutingContext context,
        ResourceExecutionDelegate next)
    {
        if (!availability.IsEnabled(feature))
        {
            context.Result = new NotFoundResult();
            return;
        }

        await next();
    }
}
