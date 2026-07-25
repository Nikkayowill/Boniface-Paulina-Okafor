namespace Okafor_.NET.Startup;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseOkaforSecurityHeaders(this IApplicationBuilder app)
    {
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
                "script-src 'self' 'unsafe-inline'; " +
                "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
                "font-src 'self' https://fonts.gstatic.com; " +
                "img-src 'self' data: https:; " +
                "frame-src 'self' https://www.google.com https://maps.google.com; " +
                "connect-src 'self'; " +
                "base-uri 'self'; " +
                "form-action 'self'; " +
                "frame-ancestors 'self';");

            await next();
        });

        return app;
    }

    public static IApplicationBuilder UseOkaforPatientDocumentGuard(this IApplicationBuilder app)
    {
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

        return app;
    }
}
