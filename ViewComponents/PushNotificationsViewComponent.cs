using Microsoft.AspNetCore.Mvc;
using Okafor_.NET.ViewModels;

namespace Okafor_.NET.ViewComponents;

public sealed class PushNotificationsViewComponent : ViewComponent
{
    private readonly IConfiguration _configuration;

    public PushNotificationsViewComponent(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IViewComponentResult Invoke()
    {
        return View(new PushNotificationsViewModel
        {
            PublicKey = _configuration["VapidKeys:PublicKey"] ?? string.Empty,
            SaveUrl = Url.Action("SaveSubscription", "PushNotifications", new { area = "" }) ?? "/PushNotifications/SaveSubscription",
            UnsubscribeUrl = Url.Action("Unsubscribe", "PushNotifications", new { area = "" }) ?? "/PushNotifications/Unsubscribe",
            TestUrl = Url.Action("SendTestNotification", "PushNotifications", new { area = "" }) ?? "/PushNotifications/SendTestNotification"
        });
    }
}
