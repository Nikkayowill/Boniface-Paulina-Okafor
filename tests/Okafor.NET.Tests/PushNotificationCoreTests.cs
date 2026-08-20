using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;

namespace Okafor.NET.Tests;

public sealed class PushNotificationCoreTests
{
    [Fact]
    public void PushSubscription_Model_HasDurableEndpointContract()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);
        var entity = context.Model.FindEntityType(typeof(PushSubscription));

        Assert.NotNull(entity);
        Assert.Equal(2048, entity.FindProperty(nameof(PushSubscription.Endpoint))?.GetMaxLength());
        Assert.Equal(64, entity.FindProperty(nameof(PushSubscription.EndpointHash))?.GetMaxLength());
        Assert.Equal(256, entity.FindProperty(nameof(PushSubscription.P256DH))?.GetMaxLength());
        Assert.Equal(256, entity.FindProperty(nameof(PushSubscription.Auth))?.GetMaxLength());
        Assert.Equal(512, entity.FindProperty(nameof(PushSubscription.UserAgent))?.GetMaxLength());

        var endpointHashIndex = entity.GetIndexes()
            .Single(index => index.Properties.Any(property => property.Name == nameof(PushSubscription.EndpointHash)));

        Assert.True(endpointHashIndex.IsUnique);
    }

    [Fact]
    public void PatientDashboard_UsesPushNotificationComponent()
    {
        var dashboard = ReadRepoFile("Areas/Patient/Views/Dashboard/Index.cshtml");
        var component = ReadRepoFile("Views/Shared/Components/PushNotifications/Default.cshtml");

        Assert.Contains("Component.InvokeAsync(\"PushNotifications\")", dashboard);
        Assert.Contains("data-push-notifications", component);
        Assert.Contains("data-vapid-public-key", component);
        Assert.Contains("data-push-enable", component);
        Assert.Contains("data-push-unsubscribe", component);

        // "Send a test" was removed: a test notification is a thing the person
        // who built this wants, not a thing a patient came to do, and it stood
        // on the dashboard as a third button of equal weight. Every consumer of
        // the hook in push-notifications.js is null-checked, so its absence is
        // not a break — this asserts the removal stays deliberate.
        Assert.DoesNotContain("data-push-test", component);
    }

    [Fact]
    public void PushNotificationScript_AvoidsOptionalChaining_ForMobileCompatibility()
    {
        var script = ReadRepoFile("wwwroot/js/push-notifications.js");

        Assert.DoesNotContain("?.", script);
        Assert.DoesNotContain("??", script);
        Assert.Contains("refreshSubscriptionState", script);
        Assert.Contains("RequestVerificationToken", script);
        Assert.Contains("window.isSecureContext", script);
    }

    private static string ReadRepoFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find {relativePath} from {AppContext.BaseDirectory}.");
    }
}
