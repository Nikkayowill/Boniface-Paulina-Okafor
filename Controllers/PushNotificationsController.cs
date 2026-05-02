using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.Services;

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
    public async Task<IActionResult> SaveSubscription([FromBody] PushSubscriptionRequest request)
    {
        if (!IsValid(request))
            return BadRequest(new { success = false, message = "Invalid push subscription." });

        var userId = _userManager.GetUserId(User);
        if (userId is null)
            return Unauthorized(new { success = false, message = "Login required." });

        var subscription = await _context.PushSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == request.Endpoint);

        if (subscription is null)
        {
            subscription = new PushSubscription
            {
                UserId = userId,
                Endpoint = request.Endpoint,
                CreatedAt = DateTime.UtcNow
            };
            _context.PushSubscriptions.Add(subscription);
        }

        subscription.P256DH = request.Keys.P256DH;
        subscription.Auth = request.Keys.Auth;
        subscription.LastUsedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "Push subscription saved." });
    }

    [HttpPost]
    public async Task<IActionResult> Unsubscribe([FromBody] PushUnsubscribeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Endpoint))
            return BadRequest(new { success = false, message = "Endpoint is required." });

        var userId = _userManager.GetUserId(User);
        if (userId is null)
            return Unauthorized(new { success = false, message = "Login required." });

        var subscriptions = await _context.PushSubscriptions
            .Where(s => s.UserId == userId && s.Endpoint == request.Endpoint)
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
            success = sent > 0,
            sent,
            message = sent > 0 ? "Test notification sent." : "No saved push subscriptions found."
        });
    }

    private static bool IsValid(PushSubscriptionRequest request)
    {
        return !string.IsNullOrWhiteSpace(request.Endpoint) &&
            request.Keys is not null &&
            !string.IsNullOrWhiteSpace(request.Keys.P256DH) &&
            !string.IsNullOrWhiteSpace(request.Keys.Auth);
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
