using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using WebPush;
using BrowserPushSubscription = WebPush.PushSubscription;

namespace Okafor_.NET.Services;

public sealed class WebPushNotificationService : IPushNotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebPushNotificationService> _logger;

    public WebPushNotificationService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<WebPushNotificationService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<PushSendResult> SendAsync(Okafor_.NET.Models.PushSubscription subscription, PushNotificationPayload payload, CancellationToken cancellationToken = default)
    {
        var vapid = GetVapidDetails();
        if (vapid is null)
        {
            _logger.LogWarning("Push notification skipped because VAPID settings are not configured.");
            return PushSendResult.Failed("VAPID settings are not configured.");
        }

        var browserSubscription = new BrowserPushSubscription(subscription.Endpoint, subscription.P256DH, subscription.Auth);
        var jsonPayload = JsonSerializer.Serialize(new
        {
            title = string.IsNullOrWhiteSpace(payload.Title) ? "Okafor Hospital" : payload.Title,
            body = string.IsNullOrWhiteSpace(payload.Body) ? "You have a new notification." : payload.Body,
            icon = string.IsNullOrWhiteSpace(payload.Icon) ? "/images/icons/okafor-hospital-icon.svg" : payload.Icon,
            badge = string.IsNullOrWhiteSpace(payload.Badge) ? "/images/icons/okafor-hospital-icon.svg" : payload.Badge,
            url = string.IsNullOrWhiteSpace(payload.Url) ? "/" : payload.Url
        });

        try
        {
            using var client = new WebPushClient();
            await client.SendNotificationAsync(browserSubscription, jsonPayload, vapid, cancellationToken);
            subscription.LastUsedAt = DateTime.UtcNow;
            subscription.LastSuccessAt = DateTime.UtcNow;
            subscription.LastFailureAt = null;
            subscription.FailureCount = 0;
            await _context.SaveChangesAsync(cancellationToken);
            return PushSendResult.Success();
        }
        catch (WebPushException ex) when (ex.StatusCode is HttpStatusCode.Gone or HttpStatusCode.NotFound)
        {
            _context.PushSubscriptions.Remove(subscription);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Removed expired push subscription {SubscriptionId}.", subscription.Id);
            return PushSendResult.Expired();
        }
        catch (Exception ex)
        {
            subscription.LastFailureAt = DateTime.UtcNow;
            subscription.FailureCount += 1;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogError(ex, "Failed to send push notification to subscription {SubscriptionId}.", subscription.Id);
            return PushSendResult.Failed("Push provider rejected the notification.");
        }
    }

    public async Task<PushSendSummary> SendToUserAsync(string userId, PushNotificationPayload payload, CancellationToken cancellationToken = default)
    {
        var subscriptions = await _context.PushSubscriptions
            .Where(s => s.UserId == userId)
            .ToListAsync(cancellationToken);

        var summary = new PushSendSummary { Attempted = subscriptions.Count };
        foreach (var subscription in subscriptions)
        {
            var result = await SendAsync(subscription, payload, cancellationToken);
            if (result.Sent)
            {
                summary.Sent++;
            }
            else if (result.Removed)
            {
                summary.Removed++;
            }
            else
            {
                summary.Failed++;
            }
        }

        return summary;
    }

    private VapidDetails? GetVapidDetails()
    {
        var publicKey = _configuration["VapidKeys:PublicKey"];
        var privateKey = _configuration["VapidKeys:PrivateKey"];
        var subject = _configuration["VapidKeys:Subject"];

        if (string.IsNullOrWhiteSpace(publicKey) ||
            string.IsNullOrWhiteSpace(privateKey) ||
            string.IsNullOrWhiteSpace(subject) ||
            publicKey.StartsWith("REPLACE_WITH_", StringComparison.OrdinalIgnoreCase) ||
            privateKey.StartsWith("REPLACE_WITH_", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new VapidDetails(subject, publicKey, privateKey);
    }
}
