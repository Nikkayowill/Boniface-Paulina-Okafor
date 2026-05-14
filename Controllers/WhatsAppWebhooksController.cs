using System.Text.Json;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.Services;

namespace Okafor_.NET.Controllers;

[ApiController]
[AllowAnonymous]
[Route("webhooks/whatsapp")]
public sealed partial class WhatsAppWebhooksController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WhatsAppWebhooksController> _logger;

    public WhatsAppWebhooksController(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<WhatsAppWebhooksController> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Verify(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? token,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        var expectedToken = _configuration["Notifications:WhatsApp:WebhookVerifyToken"];
        if (mode == "subscribe" &&
            !string.IsNullOrWhiteSpace(expectedToken) &&
            string.Equals(token, expectedToken, StringComparison.Ordinal))
        {
            return Content(challenge ?? string.Empty, "text/plain");
        }

        return Forbid();
    }

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync(cancellationToken);

        if (!IsValidSignature(body))
        {
            _logger.LogWarning("Rejected WhatsApp webhook because the signature was invalid.");
            return Unauthorized();
        }

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        if (!root.TryGetProperty("entry", out var entries) || entries.ValueKind != JsonValueKind.Array)
            return Ok();

        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("changes", out var changes) || changes.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var change in changes.EnumerateArray())
            {
                if (!change.TryGetProperty("value", out var value))
                    continue;

                await ProcessStatusesAsync(value, cancellationToken);
                await ProcessInboundMessagesAsync(value, cancellationToken);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok();
    }

    private bool IsValidSignature(string body)
    {
        var appSecret = _configuration["Notifications:WhatsApp:AppSecret"];
        if (!IntegrationConfiguration.HasRealValue(appSecret))
        {
            return true;
        }

        var signature = Request.Headers["X-Hub-Signature-256"].ToString();
        const string prefix = "sha256=";
        if (string.IsNullOrWhiteSpace(signature) ||
            !signature.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret!));
        var hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
        var received = signature[prefix.Length..].ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(hash),
            Encoding.UTF8.GetBytes(received));
    }

    private async Task ProcessStatusesAsync(JsonElement value, CancellationToken cancellationToken)
    {
        if (!value.TryGetProperty("statuses", out var statuses) || statuses.ValueKind != JsonValueKind.Array)
            return;

        foreach (var status in statuses.EnumerateArray())
        {
            var messageId = GetString(status, "id");
            var statusName = GetString(status, "status") ?? "unknown";
            var recipient = GetString(status, "recipient_id") ?? string.Empty;
            var timestamp = GetTimestamp(status);
            var error = GetStatusError(status);

            var log = !string.IsNullOrWhiteSpace(messageId)
                ? await _context.NotificationLogs.FirstOrDefaultAsync(n => n.ExternalMessageId == messageId, cancellationToken)
                : null;

            if (log is null)
            {
                log = new NotificationLog
                {
                    Channel = "WhatsApp",
                    Recipient = recipient,
                    MessageBody = "WhatsApp delivery status",
                    SentAt = DateTime.UtcNow,
                    ExternalMessageId = messageId
                };
                _context.NotificationLogs.Add(log);
            }

            log.Success = error is null;
            log.DeliveryStatus = statusName;
            log.ErrorMessage = error;
            if (statusName == "delivered")
                log.DeliveredAt = timestamp ?? DateTime.UtcNow;
            if (statusName == "read")
                log.ReadAt = timestamp ?? DateTime.UtcNow;
        }
    }

    private async Task ProcessInboundMessagesAsync(JsonElement value, CancellationToken cancellationToken)
    {
        if (!value.TryGetProperty("messages", out var messages) || messages.ValueKind != JsonValueKind.Array)
            return;

        foreach (var message in messages.EnumerateArray())
        {
            var messageId = GetString(message, "id");
            if (!string.IsNullOrWhiteSpace(messageId) &&
                await _context.NotificationLogs.AnyAsync(n => n.ExternalMessageId == messageId, cancellationToken))
            {
                continue;
            }

            var from = GetString(message, "from") ?? string.Empty;
            var body = GetInboundBody(message);
            var teleconsultationId = await TryFindTeleconsultationIdAsync(body, cancellationToken);

            _context.NotificationLogs.Add(new NotificationLog
            {
                Channel = "WhatsAppInbound",
                Recipient = from,
                MessageBody = body,
                Success = true,
                DeliveryStatus = "received",
                ExternalMessageId = messageId,
                TeleconsultationRequestId = teleconsultationId,
                SentAt = GetTimestamp(message) ?? DateTime.UtcNow
            });
        }
    }

    private async Task<int?> TryFindTeleconsultationIdAsync(string body, CancellationToken cancellationToken)
    {
        var match = TeleconsultationReferenceRegex().Match(body);
        if (!match.Success || !int.TryParse(match.Groups["id"].Value, out var id))
            return null;

        return await _context.TeleconsultationRequests
            .AnyAsync(t => t.Id == id, cancellationToken)
                ? id
                : null;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static DateTime? GetTimestamp(JsonElement element)
    {
        var raw = GetString(element, "timestamp");
        if (long.TryParse(raw, out var seconds))
            return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;

        return null;
    }

    private static string GetInboundBody(JsonElement message)
    {
        if (message.TryGetProperty("text", out var text) &&
            text.TryGetProperty("body", out var body) &&
            body.ValueKind == JsonValueKind.String)
        {
            return body.GetString() ?? string.Empty;
        }

        var type = GetString(message, "type") ?? "message";
        return $"Inbound WhatsApp {type} received.";
    }

    private static string? GetStatusError(JsonElement status)
    {
        if (!status.TryGetProperty("errors", out var errors) || errors.ValueKind != JsonValueKind.Array)
            return null;

        var messages = errors
            .EnumerateArray()
            .Select(error => GetString(error, "message") ?? GetString(error, "title"))
            .Where(message => !string.IsNullOrWhiteSpace(message));

        return string.Join("; ", messages);
    }

    [GeneratedRegex(@"TC-(?<id>\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TeleconsultationReferenceRegex();
}
