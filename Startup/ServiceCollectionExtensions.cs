using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.Services;
using Npgsql;

namespace Okafor_.NET.Startup;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOkaforData(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        if (environment.IsEnvironment("Testing") || environment.IsEnvironment("Demo"))
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("OkaforHospitalTests")
                    .ConfigureWarnings(warnings =>
                        warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        }
        else if (TryGetPostgresConnectionString(configuration, out var postgresConnectionString))
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(postgresConnectionString, postgresOptions =>
                    postgresOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null)));
        }
        else
        {
            throw new InvalidOperationException(
                "DATABASE_URL must be configured for hosted environments.");
        }

        services.AddDatabaseDeveloperPageExceptionFilter();

        var dataProtection = services
            .AddDataProtection()
            .SetApplicationName("OkaforMemorialHospital");
        if (configuration.GetValue<bool>("DataProtection:PersistKeysToDatabase"))
        {
            dataProtection.PersistKeysToDbContext<ApplicationDbContext>();
        }
        else
        {
            var dataProtectionKeysPath = configuration["DataProtection:KeysPath"];
            if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
            {
                Directory.CreateDirectory(dataProtectionKeysPath);
                dataProtection.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
            }
        }

        return services;
    }

    private static bool TryGetPostgresConnectionString(
        IConfiguration configuration,
        out string connectionString)
    {
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL") ?? configuration["DATABASE_URL"];
        if (string.IsNullOrWhiteSpace(databaseUrl))
        {
            connectionString = string.Empty;
            return false;
        }

        connectionString = NormalizeDatabaseUrl(databaseUrl);
        return true;
    }

    private static string NormalizeDatabaseUrl(string databaseUrl)
    {
        if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri) ||
            !uri.Scheme.StartsWith("postgres", StringComparison.OrdinalIgnoreCase))
        {
            return databaseUrl;
        }

        var userInfo = uri.UserInfo.Split(':', 2);
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = uri.AbsolutePath.Trim('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
            SslMode = SslMode.Require
        };

        var query = uri.Query.TrimStart('?');
        if (query.Length > 0)
        {
            foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var keyValue = pair.Split('=', 2);
                if (keyValue.Length != 2)
                {
                    continue;
                }

                var key = Uri.UnescapeDataString(keyValue[0]);
                var value = Uri.UnescapeDataString(keyValue[1]);
                if (key.Equals("sslmode", StringComparison.OrdinalIgnoreCase) &&
                    Enum.TryParse<SslMode>(value, ignoreCase: true, out var sslMode))
                {
                    connectionStringBuilder.SslMode = sslMode;
                    continue;
                }

                connectionStringBuilder[key] = value;
            }
        }

        return connectionStringBuilder.ConnectionString;
    }

    public static IServiceCollection AddOkaforIdentityAndAuthorization(
        this IServiceCollection services,
        bool requireConfirmedAccount)
    {
        services
            .AddDefaultIdentity<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = requireConfirmedAccount;
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.LoginPath = "/Identity/Account/Login";
            options.AccessDeniedPath = "/Identity/Account/AccessDenied";
            options.ReturnUrlParameter = "returnUrl";
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        });

        return services;
    }

    public static IServiceCollection AddOkaforMvc(this IServiceCollection services)
    {
        services.AddControllersWithViews(options =>
        {
            options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
        });

        services.AddRazorPages();
        services.AddHealthChecks()
            .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live"])
            .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"])
            .AddCheck<AdminAccountHealthCheck>("admin-account", tags: ["ready"])
            .AddCheck<PatientDocumentStorageHealthCheck>("patient-document-storage", tags: ["ready"]);
        services.AddHttpClient();

        return services;
    }

    public static IServiceCollection AddOkaforSupportServices(this IServiceCollection services)
    {
        services.AddScoped<IDonationReceiptEmailSender, DonationReceiptEmailSender>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<SeoUrlService>();
        services.AddSingleton<ProgramInfoService>();
        services.AddScoped<IImageService, ImageService>();

        return services;
    }

    public static IServiceCollection AddOkaforPayments(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        bool isMigrationCommand)
    {
        // Payment gateway registration (select provider via configuration Payments:Provider)
        services.AddHttpClient<PaystackPaymentGateway>();
        var paymentProviderMode = isMigrationCommand
            ? PaymentProviderMode.Mock
            : PaymentProviderSelection.Resolve(configuration, environment);
        services.AddSingleton<ILaunchFeatureAvailability>(provider =>
            new LaunchFeatureAvailability(
                provider.GetRequiredService<IConfiguration>(),
                provider.GetRequiredService<IHostEnvironment>(),
                paymentProviderMode));
        if (paymentProviderMode == PaymentProviderMode.Paystack)
        {
            services.AddScoped<IPaymentGateway>(provider =>
                provider.GetRequiredService<PaystackPaymentGateway>());
        }
        else if (paymentProviderMode == PaymentProviderMode.Mock)
        {
            services.AddScoped<IPaymentGateway, MockPaymentGateway>();
        }
        else
        {
            services.AddScoped<IPaymentGateway, DisabledPaymentGateway>();
        }

        services.AddScoped<IBillPaymentReceiptEmailSender, BillPaymentReceiptEmailSender>();

        return services;
    }

    public static IServiceCollection AddOkaforNotifications(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IWhatsAppNotificationService, MetaWhatsAppNotificationService>();

        // Hybrid notification provider — switch via appsettings "Notifications:Provider"
        var notificationProviderMode = NotificationProviderSelection.Resolve(configuration);
        services.AddScoped<LeanNotificationService>();
        services.AddScoped<AfricasTalkingNotificationService>();
        if (notificationProviderMode == NotificationProviderMode.AfricasTalking)
        {
            services.AddScoped<INotificationService, AfricasTalkingNotificationService>();
        }
        else if (notificationProviderMode == NotificationProviderMode.Composite)
        {
            services.AddScoped<INotificationService, CompositeNotificationService>();
        }
        else
        {
            services.AddScoped<INotificationService, LeanNotificationService>();
        }

        return services;
    }

    public static IServiceCollection AddOkaforScheduling(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IAvailabilityService, AvailabilityService>();
        services.AddScoped<IAppointmentRequestMaintenanceService, AppointmentRequestMaintenanceService>();
        services.AddScoped<ITeleconsultationLifecycleService, TeleconsultationLifecycleService>();
        services.Configure<PatientDocumentStorageOptions>(
            configuration.GetSection(PatientDocumentStorageOptions.SectionName));
        services.AddScoped<IPatientDocumentStorageService, PatientDocumentStorageService>();

        services.AddScoped<IAiSchedulingService, AiSchedulingService>();
        services.AddScoped<IWhatsAppAppointmentSlotService, WhatsAppAppointmentSlotService>();
        services.AddScoped<IWhatsAppAppointmentResponseService, WhatsAppAppointmentResponseService>();
        services.AddScoped<IWhatsAppSchedulingSessionService, WhatsAppSchedulingSessionService>();
        services.AddScoped<IWhatsAppSchedulingConversationService, WhatsAppSchedulingConversationService>();
        services.AddScoped<IResilientPatientMessagingService, ResilientPatientMessagingService>();
        services.AddScoped<IPushNotificationService, WebPushNotificationService>();

        services.Configure<BackgroundTaskOptions>(
            configuration.GetSection(BackgroundTaskOptions.SectionName));
        services.AddHostedService<AppointmentReminderService>();
        services.AddHostedService<PushSubscriptionCleanupService>();

        return services;
    }
}
