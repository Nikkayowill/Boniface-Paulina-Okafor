using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Okafor_.NET.Services;
using Okafor_.NET.ViewModels;

namespace Okafor_.NET.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class IntegrationsController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public IntegrationsController(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Index()
    {
        var paymentProvider = _configuration["Payments:Provider"] ?? "Mock";
        var notificationProvider = _configuration["Notifications:Provider"] ?? "Lean";
        var whatsAppMode = _configuration["Notifications:WhatsApp:Enabled"] ?? "Auto";

        var integrations = new[]
        {
            CreateItem(
                "Paystack",
                "Live online donations and bill payments",
                $"Payments provider: {paymentProvider}",
                true,
                "Set the provider to Paystack only after sandbox checkout, callback, webhook, and receipt verification pass.",
                "Payments:Paystack:PublicKey",
                "Payments:Paystack:SecretKey"),
            CreateItem(
                "SMTP email",
                "Account confirmation, receipts, and hospital notifications",
                "Used automatically when SMTP settings are complete",
                true,
                "Verify the sender address with the email provider before enabling confirmed-account registration in production.",
                "Email:SmtpHost",
                "Email:FromAddress",
                "Email:Username",
                "Email:Password"),
            CreateItem(
                "Meta WhatsApp Cloud API",
                "Teleconsultation updates and WhatsApp scheduling",
                $"WhatsApp mode: {whatsAppMode}",
                true,
                "Configure the public webhook after HTTPS hosting is available, then verify its challenge and signature.",
                "Notifications:WhatsApp:PhoneNumberId",
                "Notifications:WhatsApp:AccessToken",
                "Notifications:WhatsApp:AppSecret",
                "Notifications:WhatsApp:WebhookVerifyToken"),
            CreateItem(
                "Africa's Talking SMS",
                "SMS fallback for patients who cannot receive email or WhatsApp",
                $"Notifications provider: {notificationProvider}",
                false,
                "Confirm the approved sender ID and test delivery to a Nigerian number.",
                "Notifications:AfricasTalking:ApiKey",
                "Notifications:AfricasTalking:Username",
                "Notifications:AfricasTalking:SenderId"),
            CreateItem(
                "Browser push (VAPID)",
                "Patient portal browser notifications and reminders",
                "Enabled when all VAPID settings are present",
                false,
                "Use a mailto: or HTTPS contact subject and keep the private key in deployment secrets.",
                "VapidKeys:PublicKey",
                "VapidKeys:PrivateKey",
                "VapidKeys:Subject"),
            CreateItem(
                "Error monitoring",
                "Production exception reporting through Sentry",
                "Enabled when a Sentry DSN is present",
                false,
                "After configuration, trigger only a controlled staging error and confirm sensitive form values are not captured.",
                ResolveSentryConfigurationKey()),
            CreateItem(
                "Scheduling AI",
                "Optional interpretation of free-text WhatsApp appointment requests",
                "Falls back to local parsing when absent",
                false,
                "Keep the deterministic local parser available and verify that no unnecessary clinical detail is sent externally.",
                "SchedulingAi:Endpoint",
                "SchedulingAi:ApiKey")
        };

        return View(new IntegrationReadinessViewModel
        {
            EnvironmentName = _environment.EnvironmentName,
            Integrations = integrations
        });
    }

    private IntegrationReadinessItemViewModel CreateItem(
        string name,
        string purpose,
        string activationMode,
        bool isRequiredForLaunch,
        string setupHint,
        params string[] configurationKeys)
    {
        var missingKeys = configurationKeys
            .Where(key => !IntegrationConfiguration.HasRealValue(_configuration[key]))
            .ToArray();

        return new IntegrationReadinessItemViewModel
        {
            Name = name,
            Purpose = purpose,
            ActivationMode = activationMode,
            IsRequiredForLaunch = isRequiredForLaunch,
            IsConfigured = missingKeys.Length == 0,
            MissingKeys = missingKeys,
            SetupHint = setupHint
        };
    }

    private string ResolveSentryConfigurationKey()
    {
        return IntegrationConfiguration.HasRealValue(_configuration["SENTRY_DSN"])
            ? "SENTRY_DSN"
            : "Sentry:Dsn";
    }
}
