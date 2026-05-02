using Okafor_.NET.Models;

namespace Okafor_.NET.Services;

public interface IPushNotificationService
{
    Task SendAsync(PushSubscription subscription, PushNotificationPayload payload, CancellationToken cancellationToken = default);

    Task<int> SendToUserAsync(string userId, PushNotificationPayload payload, CancellationToken cancellationToken = default);
}

public sealed class PushNotificationPayload
{
    public string Title { get; set; } = "Okafor Hospital";

    public string Body { get; set; } = "You have a new notification.";

    public string Url { get; set; } = "/";

    public string? Icon { get; set; }

    public string? Badge { get; set; }
}
