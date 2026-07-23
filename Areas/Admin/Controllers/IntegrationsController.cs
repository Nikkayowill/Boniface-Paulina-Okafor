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
    private readonly ILaunchFeatureAvailability _launchFeatures;

    public IntegrationsController(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILaunchFeatureAvailability launchFeatures)
    {
        _configuration = configuration;
        _environment = environment;
        _launchFeatures = launchFeatures;
    }

    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Index()
    {
        var paymentProvider = _configuration["Payments:Provider"] ?? "Mock";
        var notificationProvider = _configuration["Notifications:Provider"] ?? "Lean";
        var whatsAppMode = _configuration["Notifications:WhatsApp:Enabled"] ?? "Auto";
        var onlineDonationsEnabled = _launchFeatures.IsEnabled(LaunchFeature.OnlineDonations);
        var billPaymentsEnabled = _launchFeatures.IsEnabled(LaunchFeature.BillPayments);
        var patientDocumentsEnabled = _launchFeatures.IsEnabled(LaunchFeature.PatientDocuments);
        var confirmedAccountsRequired =
            _configuration.GetValue<bool?>("Authentication:RequireConfirmedAccount") ??
            !_environment.IsEnvironment("Testing");

        var integrations = new[]
        {
            CreateItem(
                "Hospital contact details",
                "Public contact, emergency, and donor follow-up information",
                "Displayed from hospital configuration",
                true,
                "Confirm these values with the hospital owner before the public demo.",
                "Hospital:Email",
                "Hospital:EmergencyNumbers"),
            CreateItem(
                "Paystack",
                "Hosted online donations and optional online bill payments",
                $"Payments provider: {paymentProvider}",
                onlineDonationsEnabled || billPaymentsEnabled,
                onlineDonationsEnabled
                    ? "Complete one low-value live donation and confirm checkout, callback, webhook, database status, settlement, and receipt before launch."
                    : "Online donations are disabled in this environment.",
                "Payments:Paystack:SecretKey"),
            CreateItem(
                "SMTP email",
                "Account confirmation, receipts, and hospital notifications",
                "Used automatically when SMTP settings are complete",
                confirmedAccountsRequired,
                "Verify the sender address with the email provider before enabling confirmed-account registration in production.",
                "Email:SmtpHost",
                "Email:FromAddress",
                "Email:Username",
                "Email:Password"),
            CreateItem(
                "Meta WhatsApp Cloud API",
                "Teleconsultation updates and WhatsApp scheduling",
                $"WhatsApp mode: {whatsAppMode}",
                false,
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
                "Patient document storage",
                "Persistent private storage for patient uploads",
                patientDocumentsEnabled ? "Enabled for this environment" : "Disabled for this launch",
                patientDocumentsEnabled,
                patientDocumentsEnabled
                    ? "Confirm the mounted volume survives a host revision and verify /health/ready."
                    : "Keep disabled until a persistent private volume has been mounted and verified.",
                "PatientDocuments:StorageRoot",
                "PatientDocuments:PersistentStorageConfirmed"),
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
