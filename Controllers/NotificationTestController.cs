using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Okafor_.NET.Services;

namespace Okafor_.NET.Controllers;

[ApiController]
[Route("dev/notifications")]
[IgnoreAntiforgeryToken]
public sealed class NotificationTestController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly IEmailSender _emailSender;
    private readonly IWhatsAppNotificationService _whatsApp;
    private readonly IPushNotificationService _pushNotifications;
    private readonly ILogger<NotificationTestController> _logger;

    public NotificationTestController(
        IWebHostEnvironment environment,
        IEmailSender emailSender,
        IWhatsAppNotificationService whatsApp,
        IPushNotificationService pushNotifications,
        ILogger<NotificationTestController> logger)
    {
        _environment = environment;
        _emailSender = emailSender;
        _whatsApp = whatsApp;
        _pushNotifications = pushNotifications;
        _logger = logger;
    }

    [HttpPost("email")]
    public async Task<IActionResult> SendEmailAsync([FromBody] EmailTestRequest request)
    {
        if (!IsDevelopment())
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.To))
            return BadRequest(new { success = false, message = "Recipient email is required." });

        try
        {
            await _emailSender.SendEmailAsync(
                request.To.Trim(),
                string.IsNullOrWhiteSpace(request.Subject) ? "Okafor Hospital email test" : request.Subject.Trim(),
                string.IsNullOrWhiteSpace(request.HtmlBody)
                    ? "<p>Brevo SMTP email test from Okafor Memorial Hospital.</p>"
                    : request.HtmlBody);

            return Ok(new { success = true, message = "Email send attempted. Check SMTP logs/provider delivery status." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Development email notification test failed for {Recipient}.", request.To);
            return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("whatsapp")]
    public async Task<IActionResult> SendWhatsAppAsync([FromBody] WhatsAppTestRequest request, CancellationToken cancellationToken)
    {
        if (!IsDevelopment())
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.To))
            return BadRequest(new { success = false, message = "Recipient phone number is required." });

        try
        {
            var sent = await _whatsApp.SendTextMessageAsync(
                request.To,
                string.IsNullOrWhiteSpace(request.Message)
                    ? "Okafor Memorial Hospital WhatsApp test message."
                    : request.Message,
                cancellationToken);

            return Ok(new
            {
                success = sent,
                message = sent
                    ? "WhatsApp send succeeded."
                    : "WhatsApp send did not succeed. Check credentials, opt-in rules, and notification logs."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Development WhatsApp notification test failed for {Recipient}.", request.To);
            return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("push")]
    public async Task<IActionResult> SendPushAsync([FromBody] PushTestRequest request, CancellationToken cancellationToken)
    {
        if (!IsDevelopment())
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.UserId))
            return BadRequest(new { success = false, message = "UserId is required." });

        try
        {
            var summary = await _pushNotifications.SendToUserAsync(
                request.UserId,
                new PushNotificationPayload
                {
                    Title = string.IsNullOrWhiteSpace(request.Title) ? "Okafor Hospital test" : request.Title,
                    Body = string.IsNullOrWhiteSpace(request.Body) ? "Push notifications are configured." : request.Body,
                    Url = string.IsNullOrWhiteSpace(request.Url) ? "/" : request.Url
                },
                cancellationToken);

            return Ok(new
            {
                success = summary.Sent > 0,
                summary.Attempted,
                summary.Sent,
                summary.Failed,
                summary.Removed
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Development push notification test failed for user {UserId}.", request.UserId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = ex.Message });
        }
    }

    private bool IsDevelopment()
    {
        return _environment.IsDevelopment();
    }

    public sealed class EmailTestRequest
    {
        public string To { get; set; } = string.Empty;

        public string? Subject { get; set; }

        public string? HtmlBody { get; set; }
    }

    public sealed class WhatsAppTestRequest
    {
        public string To { get; set; } = string.Empty;

        public string? Message { get; set; }
    }

    public sealed class PushTestRequest
    {
        public string UserId { get; set; } = string.Empty;

        public string? Title { get; set; }

        public string? Body { get; set; }

        public string? Url { get; set; }
    }
}
