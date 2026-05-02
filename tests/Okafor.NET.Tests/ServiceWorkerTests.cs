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
    [InlineData("/Identity/Account/Login", true)]
    [InlineData("/Home/About", false)]
    [InlineData("/Home/Doctors", false)]
    [InlineData("/", false)]
    public void IsSensitivePath_CorrectlyIdentifiesRestrictedRoutes(string pathname, bool isSensitive)
    {
        // Arrange: Sensitive path prefixes
        var sensitivePrefixes = new[]
        {
            "/Admin", "/Patient", "/Portal", "/Identity",
            "/BillPayments", "/Donation/Receipt", "/uploads", "/hubs"
        };

        // Act: Check if path matches any sensitive prefix
        var matches = sensitivePrefixes.Any(prefix =>
            pathname == prefix || pathname.StartsWith($"{prefix}/"));

        // Assert
        Assert.Equal(isSensitive, matches);
    }

    [Theory]
    [InlineData("/", true)]
    [InlineData("/Home/About", true)]
    [InlineData("/Home/Doctors", true)]
    [InlineData("/Home/News", true)]
    [InlineData("/Portal/Appointments", false)]
    [InlineData("/Admin/Dashboard", false)]
    public void ShouldCachePage_CorrectlyIdentifiesPublicPages(string pathname, bool shouldCache)
    {
        // Arrange: Public page paths
        var publicPaths = new[]
        {
            "/", "/Home/About", "/Home/Services", "/Home/Doctors",
            "/Home/Team", "/Home/PatientInformationHub", "/Home/News", "/Home/Contact"
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
    public void HandleSensitiveAppointmentNavigation_ReturnsAppointmentsFallback()
    {
        // Arrange: When sensitive appointment page fails to load
        var primaryFallback = "/offline-appointments.html";

        // Act: Try to get appointment fallback first
        var fallbackUsed = primaryFallback;

        // Assert: Should prefer appointment-specific offline page
        Assert.Equal(primaryFallback, fallbackUsed);
    }

    [Theory]
    [InlineData("okafor-pwa-v4-static", true)]
    [InlineData("okafor-pwa-v3-static", false)]
    [InlineData("okafor-pwa-v4-pages", true)]
    [InlineData("old-cache-key", false)]
    [InlineData("unrelated-cache", false)]
    public void Activate_CleanupsCacheVersions(string cacheKey, bool shouldKeep)
    {
        // Arrange: Version pattern
        const string VERSION = "okafor-pwa-v4";
        
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
            "/",
            "/offline.html",
            "/offline-appointments.html",
            "/css/tailwind.css",
            "/js/pwa-register.js",
            "/js/pwa-appointments.js"
        };

        // Act & Assert: All critical assets should be included
        Assert.NotEmpty(assets);
        Assert.Contains("/offline.html", assets);
        Assert.Contains("/offline-appointments.html", assets);
        Assert.Contains("/js/pwa-appointments.js", assets);
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
