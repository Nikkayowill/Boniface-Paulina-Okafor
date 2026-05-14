using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.Services;
using System.Security.Cryptography;
using System.Text;

namespace Okafor_.NET.Controllers;

[Authorize]
public class PushNotificationsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPushNotificationService _pushNotifications;

    public PushNotificationsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IPushNotificationService pushNotifications)
    {
        _context = context;
        _userManager = userManager;
        _pushNotifications = pushNotifications;
    }

    [HttpPost]
    public async Task<IActionResult> SaveSubscription([FromBody] PushSubscriptionRequest? request)
    {
        if (!TryNormalize(request, out var normalized, out var validationMessage))
            return BadRequest(new { success = false, message = validationMessage });

        var userId = _userManager.GetUserId(User);
        if (userId is null)
            return Unauthorized(new { success = false, message = "Login required." });

        var endpointHash = HashEndpoint(normalized.Endpoint);
        var subscription = await _context.PushSubscriptions
            .FirstOrDefaultAsync(s => s.EndpointHash == endpointHash);

        if (subscription is null)
        {
            subscription = new PushSubscription
            {
                UserId = userId,
                Endpoint = normalized.Endpoint,
                EndpointHash = endpointHash,
                CreatedAt = DateTime.UtcNow
            };
            _context.PushSubscriptions.Add(subscription);
        }

        subscription.UserId = userId;
        subscription.Endpoint = normalized.Endpoint;
        subscription.P256DH = normalized.Keys.P256DH;
        subscription.Auth = normalized.Keys.Auth;
        subscription.LastUsedAt = DateTime.UtcNow;
        subscription.LastFailureAt = null;
        subscription.FailureCount = 0;
        subscription.UserAgent = GetUserAgent();

        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "Push subscription saved." });
    }

    [HttpPost]
    public async Task<IActionResult> Unsubscribe([FromBody] PushUnsubscribeRequest? request)
    {
        if (string.IsNullOrWhiteSpace(request?.Endpoint))
            return BadRequest(new { success = false, message = "Endpoint is required." });

        var userId = _userManager.GetUserId(User);
        if (userId is null)
            return Unauthorized(new { success = false, message = "Login required." });

        var endpointHash = HashEndpoint(request.Endpoint);
        var subscriptions = await _context.PushSubscriptions
            .Where(s => s.UserId == userId && s.EndpointHash == endpointHash)
            .ToListAsync();

        if (subscriptions.Count > 0)
        {
            _context.PushSubscriptions.RemoveRange(subscriptions);
            await _context.SaveChangesAsync();
        }

        return Json(new { success = true, message = "Push subscription removed." });
    }

    [HttpPost]
    public async Task<IActionResult> SendTestNotification()
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null)
            return Unauthorized(new { success = false, message = "Login required." });

        var sent = await _pushNotifications.SendToUserAsync(userId, new PushNotificationPayload
        {
            Title = "Test notification",
            Body = "Push notifications are working.",
            Url = Url.Action("Index", "Dashboard", new { area = "Patient" }) ?? "/"
        });

        return Json(new
        {
            success = sent.Sent > 0,
            sent = sent.Sent,
            failed = sent.Failed,
            removed = sent.Removed,
            message = sent.Sent > 0
                ? "Test notification sent."
                : sent.Attempted == 0
                    ? "No active push subscriptions were found for this account."
                    : "Push notification delivery failed. Check VAPID settings and the browser subscription."
        });
    }

    private static bool TryNormalize(
        PushSubscriptionRequest? request,
        out PushSubscriptionRequest normalized,
        out string message)
    {
        normalized = new PushSubscriptionRequest();
        message = "Invalid push subscription.";

        if (request?.Keys is null)
            return false;

        var endpointValue = request.Endpoint?.Trim() ?? string.Empty;
        var p256dh = request.Keys.P256DH?.Trim() ?? string.Empty;
        var auth = request.Keys.Auth?.Trim() ?? string.Empty;

        if (!Uri.TryCreate(endpointValue, UriKind.Absolute, out var endpoint) ||
            endpoint.Scheme != Uri.UriSchemeHttps)
        {
            message = "Push subscription endpoint must be a secure URL.";
            return false;
        }

        if (endpointValue.Length > 2048 || p256dh.Length > 256 || auth.Length > 256)
        {
            message = "Push subscription payload is too large.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(p256dh) || string.IsNullOrWhiteSpace(auth))
        {
            message = "Push subscription keys are required.";
            return false;
        }

        normalized = new PushSubscriptionRequest
        {
            Endpoint = endpointValue,
            Keys = new PushSubscriptionKeys
            {
                P256DH = p256dh,
                Auth = auth
            }
        };
        return true;
    }

    private static string HashEndpoint(string endpoint)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(endpoint.Trim()));
        return Convert.ToHexString(bytes);
    }

    private string? GetUserAgent()
    {
        var userAgent = Request.Headers.UserAgent.ToString();
        return string.IsNullOrWhiteSpace(userAgent)
            ? null
            : userAgent[..Math.Min(userAgent.Length, 512)];
    }
}

public sealed class PushSubscriptionRequest
{
    public string Endpoint { get; set; } = string.Empty;

    public PushSubscriptionKeys Keys { get; set; } = new();
}

public sealed class PushSubscriptionKeys
{
    public string P256DH { get; set; } = string.Empty;

    public string Auth { get; set; } = string.Empty;
}

public sealed class PushUnsubscribeRequest
{
    public string Endpoint { get; set; } = string.Empty;
}
