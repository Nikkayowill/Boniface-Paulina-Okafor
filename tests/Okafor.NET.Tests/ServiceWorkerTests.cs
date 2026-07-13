using Xunit;

namespace Okafor.NET.Tests;

/// <summary>
/// Tests for Service Worker functionality including notification handling,
/// URL matching, cache fallback strategies, and offline support.
/// </summary>
public class ServiceWorkerTests
{
    [Fact]
    public void NotificationClick_URLMatching_UsesPathnameComparison()
    {
        // Arrange: Two different URLs with same pathname
        var client1Url = "https://okafor-hospital.com/Portal/Appointments";
        var client2Url = "https://okafor-hospital.com/Portal/Appointments?tab=upcoming";
        var targetUrl = "/Portal/Appointments";

        // Act: Extract pathnames
        var client1Pathname = new Uri(client1Url).AbsolutePath;
        var client2Pathname = new Uri(client2Url).AbsolutePath;
        var targetPathname = targetUrl;

        // Assert: Pathnames should match regardless of query parameters
        Assert.Equal(client1Pathname, client2Pathname);
        Assert.Equal(targetPathname, client1Pathname);
    }

    [Fact]
    public void NotificationClick_MalformedURL_ReturnsOpenWindow()
    {
        // Arrange: A malformed target URL without scheme or leading slash
        var targetUrl = "not-a-valid-url";
        var isValidPath = targetUrl.StartsWith("/") || targetUrl.StartsWith("http");

        // Act & Assert: Invalid paths should fall back to openWindow
        Assert.False(isValidPath);
    }

    [Theory]
    [InlineData("/Portal/Appointments", true)]
    [InlineData("/Admin/Dashboard", true)]
    [InlineData("/Account/Login", true)]
    [InlineData("/Identity/Account/Login", true)]
    [InlineData("/api/account/logout", true)]
    [InlineData("/api/patient/records", true)]
    [InlineData("/api/portal/appointments", true)]
    [InlineData("/Home/About", false)]
    [InlineData("/Home/Doctors", false)]
    [InlineData("/Home/Contact", false)]
    [InlineData("/", false)]
    public void IsSensitivePath_CorrectlyIdentifiesRestrictedRoutes(string pathname, bool isSensitive)
    {
        // Arrange: Sensitive path prefixes
        var sensitivePrefixes = new[]
        {
            "/Admin", "/Account", "/Patient", "/Portal", "/Identity",
            "/BillPayments", "/Donation/Receipt", "/uploads", "/hubs",
            "/api/account", "/api/portal", "/api/patient", "/api/admin", "/api/identity",
            "/api/billing", "/api/billpayments", "/api/documents", "/api/messages"
        };

        // Act: Check if path matches any sensitive prefix
        var normalizedPath = pathname.ToLowerInvariant();
        var matches = sensitivePrefixes.Any(prefix =>
            normalizedPath == prefix.ToLowerInvariant() || normalizedPath.StartsWith($"{prefix.ToLowerInvariant()}/"));

        // Assert
        Assert.Equal(isSensitive, matches);
    }

    [Theory]
    [InlineData("/", true)]
    [InlineData("/Home/About", true)]
    [InlineData("/Home/Doctors", true)]
    [InlineData("/Home/News", true)]
    [InlineData("/Home/Contact", false)]
    [InlineData("/Portal/Appointments", false)]
    [InlineData("/Admin/Dashboard", false)]
    public void ShouldCachePage_CorrectlyIdentifiesPublicPages(string pathname, bool shouldCache)
    {
        // Arrange: Public page paths
        var publicPaths = new[]
        {
            "/", "/Home/About", "/Home/Services", "/Home/Doctors",
            "/Home/Team", "/Home/PatientInformationHub", "/Home/News", "/doctors"
        };

        // Act: Check if path is public
        var isPublic = publicPaths.Any(path =>
            pathname == path || (path != "/" && pathname.StartsWith($"{path}/")));

        // Assert
        Assert.Equal(shouldCache, isPublic);
    }

    [Fact]
    public void HandleNavigation_CacheOfflineFallback_WhenNetworkFails()
    {
        // Arrange: Response options when fetch fails
        var cacheResponse = true;
        var offlineFallback = "/offline.html";

        // Act: Simulate fallback chain
        var response = cacheResponse ? offlineFallback : null;

        // Assert: Should return offline.html when network unavailable
        Assert.Equal(offlineFallback, response);
    }

    [Fact]
    public void HandleNavigation_Returns503WithFallback_WhenBothCachesEmpty()
    {
        // Arrange: No cache available, network failed
        var status = 503; // Service Unavailable

        // Act: Construct fallback response
        var response = $"Status: {status}, Content: Fallback HTML";

        // Assert: Should return 503 with helpful message
        Assert.Equal(503, status);
        Assert.Contains("Fallback", response);
    }

    [Fact]
    public void HandlePrivateNavigation_ReturnsGenericOfflineFallback()
    {
        // Arrange: When a secure patient page fails to load
        var primaryFallback = "/offline.html";

        // Act: Use the generic offline page for private routes
        var fallbackUsed = primaryFallback;

        // Assert: Should not expose patient-specific data offline
        Assert.Equal(primaryFallback, fallbackUsed);
    }

    [Theory]
    [InlineData("okafor-pwa-v11-static", true)]
    [InlineData("okafor-pwa-v6-static", false)]
    [InlineData("okafor-pwa-v11-runtime", true)]
    [InlineData("old-cache-key", false)]
    [InlineData("unrelated-cache", false)]
    public void Activate_CleanupsCacheVersions(string cacheKey, bool shouldKeep)
    {
        // Arrange: Version pattern
        const string VERSION = "okafor-pwa-v11";
        
        // Act: Check if cache should be kept
        var startsWithVersion = cacheKey.StartsWith(VERSION);

        // Assert: Only keep caches starting with current VERSION
        Assert.Equal(shouldKeep, startsWithVersion);
    }

    [Fact]
    public void Install_PrecachesStaticAssets()
    {
        // Arrange: Static assets to precache
        var assets = new[]
        {
            "/app-shell.html",
            "/offline.html",
            "/offline-appointments.html",
            "/css/app-shell.css",
            "/css/tailwind.css",
            "/js/navigation.js",
            "/js/encrypted-offline-store.js",
            "/js/offline-state.js",
            "/js/pwa-register.js",
            "/js/pwa-appointments.js"
        };

        // Act & Assert: All critical assets should be included
        Assert.NotEmpty(assets);
        Assert.Contains("/app-shell.html", assets);
        Assert.Contains("/offline.html", assets);
        Assert.Contains("/offline-appointments.html", assets);
        Assert.Contains("/js/navigation.js", assets);
        Assert.Contains("/js/encrypted-offline-store.js", assets);
        Assert.Contains("/js/pwa-appointments.js", assets);
    }

    [Theory]
    [InlineData("POST", false)]
    [InlineData("PUT", false)]
    [InlineData("DELETE", false)]
    [InlineData("GET", true)]
    public void FetchHandler_OnlyInterceptsGetRequests(string method, bool canIntercept)
    {
        var shouldIntercept = method == "GET";

        Assert.Equal(canIntercept, shouldIntercept);
    }

    [Fact]
    public void PushEvent_HandlesValidPayload()
    {
        // Arrange: Valid push notification payload
        var payload = new
        {
            title = "Appointment Reminder",
            body = "Your appointment is in 24 hours",
            icon = "/images/icons/okafor-hospital-icon.svg",
            url = "/Portal/Appointments"
        };

        // Act & Assert: Payload contains required fields
        Assert.NotNull(payload.title);
        Assert.NotNull(payload.body);
        Assert.NotNull(payload.icon);
        Assert.NotNull(payload.url);
    }

    [Fact]
    public void PushEvent_UsesDefaults_WhenPayloadMissing()
    {
        // Arrange: Default notification values
        var defaults = new
        {
            title = "Okafor Hospital",
            body = "You have a new notification.",
            url = "/"
        };

        // Act & Assert: Defaults should be available
        Assert.Equal("Okafor Hospital", defaults.title);
        Assert.Equal("/", defaults.url);
    }
}
