using Microsoft.AspNetCore.Mvc;
using Okafor_.NET.Controllers;

namespace Okafor_.NET.Tests;

public sealed class PushNotificationControllerTests
{
    [Fact]
    public async Task SaveSubscription_NullPayload_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.SaveSubscription(null);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SaveSubscription_NonHttpsEndpoint_ReturnsBadRequest()
    {
        var controller = CreateController();
        var request = ValidRequest();
        request.Endpoint = "http://push.example.test/subscription";

        var result = await controller.SaveSubscription(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SaveSubscription_MissingKeys_ReturnsBadRequest()
    {
        var controller = CreateController();
        var request = ValidRequest();
        request.Keys.Auth = string.Empty;

        var result = await controller.SaveSubscription(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SaveSubscription_OversizedPayload_ReturnsBadRequest()
    {
        var controller = CreateController();
        var request = ValidRequest();
        request.Keys.P256DH = new string('a', 257);

        var result = await controller.SaveSubscription(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Unsubscribe_MissingEndpoint_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.Unsubscribe(new PushUnsubscribeRequest());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    private static PushNotificationsController CreateController() =>
        new(null!, null!, null!);

    private static PushSubscriptionRequest ValidRequest() =>
        new()
        {
            Endpoint = "https://push.example.test/subscription",
            Keys = new PushSubscriptionKeys
            {
                P256DH = "test-p256dh-key",
                Auth = "test-auth-key"
            }
        };
}
