using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Okafor_.NET.Startup;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapOkaforHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health");
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("live")
        });
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("ready")
        });

        return endpoints;
    }

    public static IEndpointRouteBuilder MapOkaforRoutes(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllerRoute(
            name: "about",
            pattern: "about",
            defaults: new { controller = "Home", action = "About" });

        endpoints.MapControllerRoute(
            name: "services",
            pattern: "services",
            defaults: new { controller = "Home", action = "Services" });

        endpoints.MapControllerRoute(
            name: "doctors_index",
            pattern: "doctors",
            defaults: new { controller = "Home", action = "Team" });

        endpoints.MapControllerRoute(
            name: "news_index",
            pattern: "news",
            defaults: new { controller = "Home", action = "News" });

        endpoints.MapControllerRoute(
            name: "patient_information",
            pattern: "patient-information",
            defaults: new { controller = "Home", action = "PatientInformationHub" });

        endpoints.MapControllerRoute(
            name: "contact",
            pattern: "contact",
            defaults: new { controller = "Home", action = "Contact" });

        endpoints.MapControllerRoute(
            name: "support",
            pattern: "support",
            defaults: new { controller = "Home", action = "Support" });

        endpoints.MapControllerRoute(
            name: "privacy",
            pattern: "privacy",
            defaults: new { controller = "Home", action = "Privacy" });

        endpoints.MapControllerRoute(
            name: "news_slug",
            pattern: "news/{slug}",
            defaults: new { controller = "Home", action = "NewsDetail" });

        endpoints.MapControllerRoute(
            name: "doctor_department_lookup",
            pattern: "Doctors/GetByDepartment",
            defaults: new { controller = "Doctors", action = "GetByDepartment" });

        endpoints.MapControllerRoute(
            name: "doctor_slug",
            pattern: "doctors/{slug}",
            defaults: new { controller = "Home", action = "DoctorProfile" });

        // SignalR hub for real-time booking updates
        endpoints.MapHub<Okafor_.NET.Hubs.BookingHub>("/hubs/bookings");

        endpoints.MapControllerRoute(
            name: "areas",
            pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

        endpoints.MapAreaControllerRoute(
            name: "admin",
            areaName: "Admin",
            pattern: "Admin/{controller=Dashboard}/{action=Index}/{id?}");

        endpoints.MapAreaControllerRoute(
            name: "patient",
            areaName: "Patient",
            pattern: "Portal/{controller=Dashboard}/{action=Index}/{id?}");

        endpoints.MapControllerRoute(
            name: "admin_doctors",
            pattern: "Admin/Doctors/{action=Index}/{id?}",
            defaults: new { controller = "Doctors" });

        endpoints.MapControllerRoute(
            name: "admin_departments",
            pattern: "Admin/Departments/{action=Index}/{id?}",
            defaults: new { controller = "Departments" });

        endpoints.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        endpoints.MapRazorPages();

        return endpoints;
    }
}
