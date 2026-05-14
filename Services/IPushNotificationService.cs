using Okafor_.NET.Models;

namespace Okafor_.NET.Services;

public interface IPushNotificationService
{
    Task<PushSendResult> SendAsync(PushSubscription subscription, PushNotificationPayload payload, CancellationToken cancellationToken = default);

    Task<PushSendSummary> SendToUserAsync(string userId, PushNotificationPayload payload, CancellationToken cancellationToken = default);
}

public sealed class PushNotificationPayload
{
    public string Title { get; set; } = "Okafor Hospital";

    public string Body { get; set; } = "You have a new notification.";

    public string Url { get; set; } = "/";

    public string? Icon { get; set; }

    public string? Badge { get; set; }
}

public sealed class PushSendResult
{
    public bool Sent { get; init; }

    public bool Removed { get; init; }

    public string? Error { get; init; }

    public static PushSendResult Success() => new() { Sent = true };

    public static PushSendResult Expired() => new() { Removed = true, Error = "Subscription expired." };

    public static PushSendResult Failed(string error) => new() { Error = error };
}

public sealed class PushSendSummary
{
    public int Attempted { get; set; }

    public int Sent { get; set; }

    public int Failed { get; set; }

    public int Removed { get; set; }
}
