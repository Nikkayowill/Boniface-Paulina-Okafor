using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.Seed;
using Okafor_.NET.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseInMemoryDatabase("OkaforHospitalTests"));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
}

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
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
builder.Services.AddHealthChecks();
builder.Services.AddHttpClient();

builder.Services.AddScoped<IDonationReceiptEmailSender, DonationReceiptEmailSender>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IBillPaymentProvider, MockBillPaymentProvider>();
builder.Services.AddScoped<IBillPaymentReceiptEmailSender, BillPaymentReceiptEmailSender>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();

// Hybrid notification provider — switch via appsettings "Notifications:Provider"
var notifProvider = builder.Configuration["Notifications:Provider"];
if (notifProvider == "AfricasTalking")
    builder.Services.AddScoped<INotificationService, AfricasTalkingNotificationService>();
else
    builder.Services.AddScoped<INotificationService, LeanNotificationService>();

builder.Services.AddHostedService<AppointmentReminderService>();

// SignalR for real-time booking updates
builder.Services.AddSignalR();

var app = builder.Build();

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
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
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Serve patient document uploads
var uploadsPath = Path.Combine(builder.Environment.WebRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    await IdentitySeed.SeedAsync(scope.ServiceProvider);
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await ClinicalDataSeed.SeedAsync(db);
    await NewsDataSeed.SeedAsync(db);
    await AppointmentDataSeed.SeedAsync(db);
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

public partial class Program { }