using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.Seed;
using Okafor_.NET.Services;

LoadDotEnv();

var builder = WebApplication.CreateBuilder(args);
var isMigrationCommand = args.Any(argument =>
    string.Equals(argument, "--migrate-db", StringComparison.OrdinalIgnoreCase));
var isE2eEnvironment = builder.Environment.IsEnvironment("E2E");

var sentryDsn = builder.Configuration["SENTRY_DSN"] ?? builder.Configuration["Sentry:Dsn"];
if (!string.IsNullOrWhiteSpace(sentryDsn))
{
    builder.WebHost.UseSentry(options =>
    {
        options.Dsn = sentryDsn;
        options.Debug = builder.Configuration.GetValue<bool>("Sentry:Debug");
    });
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var requireConfirmedAccount =
    builder.Configuration.GetValue<bool?>("Authentication:RequireConfirmedAccount") ??
    !builder.Environment.IsEnvironment("Testing");

if (!builder.Environment.IsDevelopment() &&
    !builder.Environment.IsEnvironment("Testing") &&
    !isE2eEnvironment &&
    !isMigrationCommand &&
    requireConfirmedAccount &&
    !IntegrationConfiguration.HasSmtpSettings(builder.Configuration))
{
    throw new InvalidOperationException(
        "Email confirmation is required, but Email:SmtpHost and Email:FromAddress are not configured with production values.");
}

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseInMemoryDatabase("OkaforHospitalTests"));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString, sqlOptions =>
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null)));
}

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
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

builder.Services.ConfigureApplicationCookie(options =>
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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddRazorPages();
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<SqlServerHealthCheck>("sqlserver", tags: ["ready"]);
builder.Services.AddHttpClient();

var dataProtection = builder.Services
    .AddDataProtection()
    .SetApplicationName("OkaforMemorialHospital");
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    Directory.CreateDirectory(dataProtectionKeysPath);
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
}

builder.Services.AddScoped<IDonationReceiptEmailSender, DonationReceiptEmailSender>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
// Payment gateway registration (select provider via configuration Payments:Provider)
builder.Services.AddHttpClient<PaystackPaymentGateway>();
var paymentProviderMode = isMigrationCommand
    ? PaymentProviderMode.Mock
    : PaymentProviderSelection.Resolve(builder.Configuration, builder.Environment);
builder.Services.AddSingleton<ILaunchFeatureAvailability>(provider =>
    new LaunchFeatureAvailability(
        provider.GetRequiredService<IConfiguration>(),
        provider.GetRequiredService<IHostEnvironment>(),
        paymentProviderMode));
if (paymentProviderMode == PaymentProviderMode.Paystack)
{
    builder.Services.AddScoped<IPaymentGateway>(provider =>
        provider.GetRequiredService<PaystackPaymentGateway>());
}
else if (paymentProviderMode == PaymentProviderMode.Mock)
{
    builder.Services.AddScoped<IPaymentGateway, MockPaymentGateway>();
}
else
{
    builder.Services.AddScoped<IPaymentGateway, DisabledPaymentGateway>();
}
builder.Services.AddScoped<IBillPaymentReceiptEmailSender, BillPaymentReceiptEmailSender>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
builder.Services.AddScoped<IAppointmentRequestMaintenanceService, AppointmentRequestMaintenanceService>();
builder.Services.AddScoped<ITeleconsultationLifecycleService, TeleconsultationLifecycleService>();
builder.Services.Configure<PatientDocumentStorageOptions>(
    builder.Configuration.GetSection("PatientDocuments"));
builder.Services.AddScoped<IPatientDocumentStorageService, PatientDocumentStorageService>();
builder.Services.AddScoped<IWhatsAppNotificationService, MetaWhatsAppNotificationService>();

// Hybrid notification provider — switch via appsettings "Notifications:Provider"
var notificationProviderMode = NotificationProviderSelection.Resolve(builder.Configuration);
builder.Services.AddScoped<LeanNotificationService>();
builder.Services.AddScoped<AfricasTalkingNotificationService>();
if (notificationProviderMode == NotificationProviderMode.AfricasTalking)
{
    builder.Services.AddScoped<INotificationService, AfricasTalkingNotificationService>();
}
else if (notificationProviderMode == NotificationProviderMode.Composite)
{
    builder.Services.AddScoped<INotificationService, CompositeNotificationService>();
}
else
{
    builder.Services.AddScoped<INotificationService, LeanNotificationService>();
}

builder.Services.AddScoped<IAiSchedulingService, AiSchedulingService>();
builder.Services.AddScoped<IWhatsAppAppointmentSlotService, WhatsAppAppointmentSlotService>();
builder.Services.AddScoped<IWhatsAppAppointmentResponseService, WhatsAppAppointmentResponseService>();
builder.Services.AddScoped<IWhatsAppSchedulingSessionService, WhatsAppSchedulingSessionService>();
builder.Services.AddScoped<IWhatsAppSchedulingConversationService, WhatsAppSchedulingConversationService>();
builder.Services.AddScoped<IResilientPatientMessagingService, ResilientPatientMessagingService>();
builder.Services.AddScoped<IPushNotificationService, WebPushNotificationService>();

builder.Services.Configure<BackgroundTaskOptions>(
    builder.Configuration.GetSection(BackgroundTaskOptions.SectionName));
builder.Services.AddHostedService<AppointmentReminderService>();
builder.Services.AddHostedService<PushSubscriptionCleanupService>();

// SignalR for real-time booking updates
builder.Services.AddSignalR();

var app = builder.Build();

if (isMigrationCommand)
{
    await using var migrationScope = app.Services.CreateAsyncScope();
    var migrationDb = migrationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await migrationDb.Database.MigrateAsync();
    app.Logger.LogInformation("Database migrations completed successfully.");
    return;
}

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    var isPrivateRoute = context.Request.Path.StartsWithSegments("/Admin") ||
        context.Request.Path.StartsWithSegments("/Patient") ||
        context.Request.Path.StartsWithSegments("/Portal") ||
        context.Request.Path.StartsWithSegments("/Identity");

    if (isPrivateRoute)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.CacheControl = "no-store, no-cache, max-age=0";
            context.Response.Headers.Pragma = "no-cache";
            context.Response.Headers.Expires = "0";
            return Task.CompletedTask;
        });
    }

    headers.TryAdd("X-Content-Type-Options", "nosniff");
    headers.TryAdd("X-Frame-Options", "SAMEORIGIN");
    headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
    headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=(self)");
    headers.TryAdd(
        "Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://cdn.tailwindcss.com https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        "img-src 'self' data: https:; " +
        "frame-src 'self' https://www.google.com https://maps.google.com; " +
        "connect-src 'self' ws: wss:; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'self';");

    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseWhen(
        context => HttpMethods.IsGet(context.Request.Method) &&
            context.Request.Headers.Accept.Any(value =>
                value?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true),
        branch => branch.UseStatusCodePagesWithReExecute("/Home/HttpStatus", "?code={0}"));
    app.UseHsts();
}

if (!isE2eEnvironment)
{
    app.UseHttpsRedirection();
}

// Legacy patient files may exist under wwwroot. They must only be read through
// an authorized controller, never served as public static files.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/uploads/patient-documents"))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    await next();
});

app.UseStaticFiles();

// Public upload root is retained for CMS post images.
var uploadsPath = Path.Combine(builder.Environment.WebRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live")
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});

if (!app.Environment.IsEnvironment("Testing") && !isE2eEnvironment)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (app.Environment.IsDevelopment())
    {
        await db.Database.MigrateAsync();
    }

    await IdentitySeed.SeedAsync(scope.ServiceProvider);
    if (DemoDataSeed.ShouldSeed(app.Environment))
    {
        await DemoDataSeed.SeedAsync(db);
    }
}

app.MapControllerRoute(
    name: "news_slug",
    pattern: "news/{slug}",
    defaults: new { controller = "Home", action = "NewsDetail" });

app.MapControllerRoute(
    name: "doctor_department_lookup",
    pattern: "Doctors/GetByDepartment",
    defaults: new { controller = "Doctors", action = "GetByDepartment" });

app.MapControllerRoute(
    name: "doctor_slug",
    pattern: "doctors/{slug}",
    defaults: new { controller = "Home", action = "DoctorProfile" });

// SignalR hub for real-time booking updates
app.MapHub<Okafor_.NET.Hubs.BookingHub>("/hubs/bookings");

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapAreaControllerRoute(
    name: "admin",
    areaName: "Admin",
    pattern: "Admin/{controller=Dashboard}/{action=Index}/{id?}");

app.MapAreaControllerRoute(
    name: "patient",
    areaName: "Patient",
    pattern: "Portal/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "admin_doctors",
    pattern: "Admin/Doctors/{action=Index}/{id?}",
    defaults: new { controller = "Doctors" });

app.MapControllerRoute(
    name: "admin_departments",
    pattern: "Admin/Departments/{action=Index}/{id?}",
    defaults: new { controller = "Departments" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();

static void LoadDotEnv()
{
    var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
    if (!File.Exists(envPath))
        return;

    foreach (var rawLine in File.ReadAllLines(envPath))
    {
        var line = rawLine.Trim();
        if (line.Length == 0 || line.StartsWith('#'))
            continue;

        if (line.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
            line = line["export ".Length..].Trim();

        var separator = line.IndexOf('=');
        if (separator <= 0)
            continue;

        var key = line[..separator].Trim();
        var value = line[(separator + 1)..].Trim().Trim('"', '\'');

        if (string.IsNullOrWhiteSpace(key) || Environment.GetEnvironmentVariable(key) is not null)
            continue;

        Environment.SetEnvironmentVariable(key, value);
    }
}

public partial class Program { }
