using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Okafor_.NET.Controllers;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.Services;

namespace Okafor.NET.Tests;

public sealed class WhatsAppIntegrationTests
{
    [Theory]
    [InlineData("08012345678", "2348012345678")]
    [InlineData("+234 801 234 5678", "2348012345678")]
    [InlineData("2348012345678", "2348012345678")]
    [InlineData("8012345678", "2348012345678")]
    [InlineData("+44 7700 900123", "447700900123")]
    [InlineData("", "")]
    [InlineData("not a phone", "")]
    public void NigerianPhoneNumber_NormalizesCommonWhatsAppFormats(string input, string expected)
    {
        Assert.Equal(expected, NigerianPhoneNumber.NormalizeForWhatsApp(input));
    }

    [Fact]
    public async Task SendTeleconsultationReceivedAsync_WhenOptedOut_DoesNotSendOrLog()
    {
        await using var context = CreateContext();
        var handler = new CapturingHttpMessageHandler(HttpStatusCode.OK, MetaSuccessBody("wamid.1"));
        var service = CreateService(context, handler, enabled: true);
        var request = CreateTeleconsultation(whatsAppOptIn: false);

        var sent = await service.SendTeleconsultationReceivedAsync(request);

        Assert.False(sent);
        Assert.Empty(handler.Requests);
        Assert.Empty(await context.NotificationLogs.ToListAsync());
    }

    [Fact]
    public async Task SendTeleconsultationReceivedAsync_WhenCredentialsMissing_LogsFailureWithoutHttpCall()
    {
        await using var context = CreateContext();
        var handler = new CapturingHttpMessageHandler(HttpStatusCode.OK, MetaSuccessBody("wamid.1"));
        var service = CreateService(context, handler, enabled: true, phoneNumberId: "REPLACE_WITH_PHONE", accessToken: "REPLACE_WITH_TOKEN");
        var request = CreateTeleconsultation();

        var sent = await service.SendTeleconsultationReceivedAsync(request);

        var log = await context.NotificationLogs.SingleAsync();
        Assert.False(sent);
        Assert.Empty(handler.Requests);
        Assert.False(log.Success);
        Assert.Equal("WhatsApp", log.Channel);
        Assert.Equal("2348012345678", log.Recipient);
        Assert.Contains("credentials are missing", log.ErrorMessage);
        Assert.Equal("failed", log.DeliveryStatus);
    }

    [Fact]
    public async Task SendTeleconsultationReceivedAsync_SendsExpectedMetaTemplatePayloadAndLogsMessageId()
    {
        await using var context = CreateContext();
        var handler = new CapturingHttpMessageHandler(HttpStatusCode.OK, MetaSuccessBody("wamid.sent.123"));
        var service = CreateService(context, handler, enabled: true);
        var request = CreateTeleconsultation();

        var sent = await service.SendTeleconsultationReceivedAsync(request);

        var httpRequest = Assert.Single(handler.Requests);
        var payload = JsonDocument.Parse(httpRequest.Body).RootElement;
        var log = await context.NotificationLogs.SingleAsync();

        Assert.True(sent);
        Assert.Equal(HttpMethod.Post, httpRequest.Method);
        Assert.Equal("https://graph.facebook.com/v23.0/123456/messages", httpRequest.RequestUri);
        Assert.Equal("Bearer test-token", httpRequest.Authorization);
        Assert.Equal("whatsapp", payload.GetProperty("messaging_product").GetString());
        Assert.Equal("2348012345678", payload.GetProperty("to").GetString());
        Assert.Equal("template", payload.GetProperty("type").GetString());
        Assert.Equal("teleconsultation_received", payload.GetProperty("template").GetProperty("name").GetString());
        Assert.Equal("en", payload.GetProperty("template").GetProperty("language").GetProperty("code").GetString());
        Assert.Equal("wamid.sent.123", log.ExternalMessageId);
        Assert.True(log.Success);
        Assert.Equal("sent", log.DeliveryStatus);
    }

    [Fact]
    public async Task SendTeleconsultationStatusAsync_WhenMetaRejectsRequest_LogsFailureBody()
    {
        await using var context = CreateContext();
        var handler = new CapturingHttpMessageHandler(HttpStatusCode.BadRequest, @"{""error"":{""message"":""Template paused""}}");
        var service = CreateService(context, handler, enabled: true);
        var request = CreateTeleconsultation(status: TeleconsultationStatus.Confirmed);

        var sent = await service.SendTeleconsultationStatusAsync(request);

        var log = await context.NotificationLogs.SingleAsync();
        Assert.False(sent);
        Assert.False(log.Success);
        Assert.Equal("failed", log.DeliveryStatus);
        Assert.Contains("HTTP 400", log.ErrorMessage);
        Assert.Contains("Template paused", log.ErrorMessage);
    }

    [Fact]
    public async Task WebhookVerify_WithMatchingToken_ReturnsChallenge()
    {
        await using var context = CreateContext();
        var controller = CreateWebhookController(context);

        var result = controller.Verify("subscribe", "verify-token", "challenge-123");

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal("challenge-123", content.Content);
        Assert.Equal("text/plain", content.ContentType);
    }

    [Fact]
    public async Task WebhookVerify_WithInvalidToken_ReturnsForbidden()
    {
        await using var context = CreateContext();
        var controller = CreateWebhookController(context);

        var result = controller.Verify("subscribe", "wrong-token", "challenge-123");

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task WebhookReceive_UpdatesExistingDeliveryStatus()
    {
        await using var context = CreateContext();
        context.NotificationLogs.Add(new NotificationLog
        {
            Channel = "WhatsApp",
            Recipient = "2348012345678",
            MessageBody = "Teleconsultation received: TC-000001",
            Success = true,
            DeliveryStatus = "sent",
            ExternalMessageId = "wamid.sent.123",
            SentAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateWebhookController(context, WebhookPayloadWithStatus("wamid.sent.123", "delivered"));

        var result = await controller.Receive(CancellationToken.None);

        var log = await context.NotificationLogs.SingleAsync();
        Assert.IsType<OkResult>(result);
        Assert.True(log.Success);
        Assert.Equal("delivered", log.DeliveryStatus);
        Assert.NotNull(log.DeliveredAt);
    }

    [Fact]
    public async Task WebhookReceive_WhenDeliveryStatusArrivesBeforeLocalLog_CreatesAuditLog()
    {
        await using var context = CreateContext();
        var controller = CreateWebhookController(context, WebhookPayloadWithStatus("wamid.unknown", "failed", "User phone number is invalid"));

        await controller.Receive(CancellationToken.None);

        var log = await context.NotificationLogs.SingleAsync();
        Assert.Equal("WhatsApp", log.Channel);
        Assert.Equal("wamid.unknown", log.ExternalMessageId);
        Assert.Equal("failed", log.DeliveryStatus);
        Assert.False(log.Success);
        Assert.Contains("invalid", log.ErrorMessage);
    }

    [Fact]
    public async Task WebhookReceive_InboundReplyLinksToTeleconsultationReferenceAndIgnoresDuplicate()
    {
        await using var context = CreateContext();
        var request = CreateTeleconsultation();
        context.TeleconsultationRequests.Add(request);
        await context.SaveChangesAsync();

        var body = $"Hello, I need to reschedule TC-{request.Id:D6}";
        var payload = WebhookPayloadWithInboundMessage("wamid.reply.1", body);
        var controller = CreateWebhookController(context, payload);

        await controller.Receive(CancellationToken.None);
        controller = CreateWebhookController(context, payload);
        await controller.Receive(CancellationToken.None);

        var log = await context.NotificationLogs.SingleAsync();
        Assert.Equal("WhatsAppInbound", log.Channel);
        Assert.Equal("received", log.DeliveryStatus);
        Assert.Equal(request.Id, log.TeleconsultationRequestId);
        Assert.Equal(body, log.MessageBody);
    }

    [Fact]
    public async Task WebhookReceive_WithConfiguredAppSecret_RejectsInvalidSignature()
    {
        await using var context = CreateContext();
        var controller = CreateWebhookController(
            context,
            WebhookPayloadWithStatus("wamid.sent.123", "delivered"),
            appSecret: "meta-app-secret");

        var result = await controller.Receive(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
        Assert.Empty(await context.NotificationLogs.ToListAsync());
    }

    [Fact]
    public async Task WebhookReceive_WithConfiguredAppSecret_AcceptsValidSignature()
    {
        await using var context = CreateContext();
        var payload = WebhookPayloadWithStatus("wamid.sent.123", "delivered");
        var controller = CreateWebhookController(context, payload, appSecret: "meta-app-secret");
        controller.Request.Headers["X-Hub-Signature-256"] = BuildMetaSignature(payload, "meta-app-secret");

        var result = await controller.Receive(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        Assert.Equal("delivered", (await context.NotificationLogs.SingleAsync()).DeliveryStatus);
    }

    private static MetaWhatsAppNotificationService CreateService(
        ApplicationDbContext context,
        CapturingHttpMessageHandler handler,
        bool enabled,
        string phoneNumberId = "123456",
        string accessToken = "test-token")
    {
        return new MetaWhatsAppNotificationService(
            CreateConfiguration(new Dictionary<string, string?>
            {
                ["Hospital:Name"] = "Okafor Hospital",
                ["Notifications:WhatsApp:Enabled"] = enabled ? "true" : "false",
                ["Notifications:WhatsApp:PhoneNumberId"] = phoneNumberId,
                ["Notifications:WhatsApp:AccessToken"] = accessToken,
                ["Notifications:WhatsApp:ApiVersion"] = "v23.0",
                ["Notifications:WhatsApp:LanguageCode"] = "en",
                ["Notifications:WhatsApp:ReceivedTemplate"] = "teleconsultation_received",
                ["Notifications:WhatsApp:StatusTemplate"] = "teleconsultation_update"
            }),
            context,
            new TestHttpClientFactory(handler),
            NullLogger<MetaWhatsAppNotificationService>.Instance);
    }

    private static WhatsAppWebhooksController CreateWebhookController(
        ApplicationDbContext context,
        string? body = null,
        string? appSecret = null)
    {
        var controller = new WhatsAppWebhooksController(
            context,
            CreateConfiguration(new Dictionary<string, string?>
            {
                ["Notifications:WhatsApp:WebhookVerifyToken"] = "verify-token",
                ["Notifications:WhatsApp:AppSecret"] = appSecret
            }),
            new NoOpWhatsAppSchedulingConversationService(),
            CreateSmsFallback(context),
            NullLogger<WhatsAppWebhooksController>.Instance);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        if (body is not null)
        {
            controller.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
            controller.Request.ContentType = "application/json";
        }

        return controller;
    }

    private sealed class NoOpWhatsAppSchedulingConversationService : IWhatsAppSchedulingConversationService
    {
        public Task HandleInboundTextAsync(
            string patientPhoneNumber,
            string message,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private static AfricasTalkingNotificationService CreateSmsFallback(ApplicationDbContext context)
    {
        return new AfricasTalkingNotificationService(
            CreateConfiguration(new Dictionary<string, string?>
            {
                ["Notifications:AfricasTalking:ApiKey"] = "REPLACE_WITH_API_KEY",
                ["Notifications:AfricasTalking:Username"] = "sandbox"
            }),
            context,
            new TestHttpClientFactory(new CapturingHttpMessageHandler(HttpStatusCode.OK, "{}")),
            NullLogger<AfricasTalkingNotificationService>.Instance);
    }

    private static string BuildMetaSignature(string body, string appSecret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
        var hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
        return $"sha256={hash}";
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static TeleconsultationRequest CreateTeleconsultation(
        bool whatsAppOptIn = true,
        TeleconsultationStatus status = TeleconsultationStatus.Pending)
    {
        return new TeleconsultationRequest
        {
            Id = 1,
            PatientName = "Ada Okafor",
            Email = "ada@example.com",
            Phone = "08012345678",
            WhatsAppOptIn = whatsAppOptIn,
            DepartmentId = 1,
            Department = new Department { Id = 1, Name = "Family Medicine" },
            PreferredDate = new DateTime(2026, 06, 15),
            PreferredTime = "10:30 AM",
            Reason = "Follow-up consultation",
            ConsentAccepted = true,
            Status = status,
            MeetingLink = status == TeleconsultationStatus.Confirmed ? "https://meet.example.com/abc" : null
        };
    }

    private static string MetaSuccessBody(string messageId)
    {
        return $$"""
            {
              "messaging_product": "whatsapp",
              "contacts": [{ "input": "2348012345678", "wa_id": "2348012345678" }],
              "messages": [{ "id": "{{messageId}}" }]
            }
            """;
    }

    private static string WebhookPayloadWithStatus(string messageId, string status, string? errorMessage = null)
    {
        var errors = errorMessage is null
            ? string.Empty
            : $$""","errors":[{"message":"{{errorMessage}}"}]""";

        return $$"""
            {
              "entry": [{
                "changes": [{
                  "value": {
                    "statuses": [{
                      "id": "{{messageId}}",
                      "status": "{{status}}",
                      "recipient_id": "2348012345678",
                      "timestamp": "1778889600"{{errors}}
                    }]
                  }
                }]
              }]
            }
            """;
    }

    private static string WebhookPayloadWithInboundMessage(string messageId, string body)
    {
        return $$"""
            {
              "entry": [{
                "changes": [{
                  "value": {
                    "messages": [{
                      "from": "2348012345678",
                      "id": "{{messageId}}",
                      "timestamp": "1778889600",
                      "type": "text",
                      "text": { "body": {{JsonSerializer.Serialize(body)}} }
                    }]
                  }
                }]
              }]
            }
            """;
    }

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public TestHttpClientFactory(HttpMessageHandler handler)
        {
            _handler = handler;
        }

        public HttpClient CreateClient(string name)
        {
            return new HttpClient(_handler, disposeHandler: false);
        }
    }

    private sealed class CapturingHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseBody;

        public CapturingHttpMessageHandler(HttpStatusCode statusCode, string responseBody)
        {
            _statusCode = statusCode;
            _responseBody = responseBody;
        }

        public List<CapturedHttpRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(new CapturedHttpRequest(
                request.Method,
                request.RequestUri?.ToString() ?? string.Empty,
                request.Headers.Authorization?.ToString() ?? string.Empty,
                request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken)));

            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed record CapturedHttpRequest(
        HttpMethod Method,
        string RequestUri,
        string Authorization,
        string Body);
}
