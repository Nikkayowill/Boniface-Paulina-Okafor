using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Seed;
using Okafor_.NET.Services;
using Okafor_.NET.Startup;

LoadDotEnv();

var builder = WebApplication.CreateBuilder(args);
var renderPortValue = Environment.GetEnvironmentVariable("PORT");
if (int.TryParse(renderPortValue, out var renderPort) && renderPort > 0)
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{renderPort}");
}
var isMigrationCommand = args.Any(argument =>
    string.Equals(argument, "--migrate-db", StringComparison.OrdinalIgnoreCase));
var isE2eEnvironment = builder.Environment.IsEnvironment("E2E");
var applyMigrationsOnStartup = DatabaseMigrationPolicy.ShouldApplyOnStartup(
    builder.Configuration,
    builder.Environment);

var sentryDsn = builder.Configuration["SENTRY_DSN"] ?? builder.Configuration["Sentry:Dsn"];
if (!string.IsNullOrWhiteSpace(sentryDsn))
{
    builder.WebHost.UseSentry(options =>
    {
        options.Dsn = sentryDsn;
        options.Debug = builder.Configuration.GetValue<bool>("Sentry:Debug");
    });
}

var smtpSettingsConfigured = IntegrationConfiguration.HasSmtpSettings(builder.Configuration);
var requireConfirmedAccount =
    (builder.Configuration.GetValue<bool?>("Authentication:RequireConfirmedAccount") ??
        !builder.Environment.IsEnvironment("Testing")) &&
    smtpSettingsConfigured;

builder.Services.AddOkaforData(builder.Configuration, builder.Environment);
builder.Services.AddOkaforIdentityAndAuthorization(requireConfirmedAccount);
builder.Services.AddOkaforMvc();
builder.Services.AddOkaforSupportServices();
builder.Services.AddOkaforPayments(builder.Configuration, builder.Environment, isMigrationCommand);
builder.Services.AddOkaforNotifications(builder.Configuration);
builder.Services.AddOkaforScheduling(builder.Configuration);

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

app.UseOkaforSecurityHeaders();

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

app.UseOkaforPatientDocumentGuard();

app.UseStaticFiles();

// Public upload root is retained for CMS post images.
var uploadsPath = Path.Combine(builder.Environment.WebRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);

app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapOkaforHealthChecks();

if (!app.Environment.IsEnvironment("Testing") && !isE2eEnvironment)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (app.Environment.IsDevelopment() || applyMigrationsOnStartup)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.Logger.LogWarning(
                "Applying database migrations during startup because Database:ApplyMigrationsOnStartup is enabled.");
        }

        await db.Database.MigrateAsync();
    }

    await IdentitySeed.SeedAsync(scope.ServiceProvider);
    // Keep the public clinical directory aligned with the confirmed real clinicians
    // on every host, including an existing production database.
    await ClinicalDataSeed.SeedAsync(db);
    if (DemoDataSeed.ShouldSeed(app.Environment, app.Configuration))
    {
        await DemoDataSeed.SeedAsync(db);
    }
}

app.MapOkaforRoutes();

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
