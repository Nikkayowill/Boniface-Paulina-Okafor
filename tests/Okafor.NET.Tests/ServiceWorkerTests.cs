using System.Text.RegularExpressions;
using Xunit;

namespace Okafor.NET.Tests;

/// <summary>
/// Tests for the real wwwroot/service-worker.js -- the piece of the app responsible for keeping
/// patient data (Admin/Patient/Portal/billing/document routes) out of the browser's page cache.
/// Every test here extracts the actual PRIVATE_ROUTE_PREFIXES / PUBLIC_ROUTES / STATIC_ASSETS
/// arrays from the shipped file and re-runs the service worker's own matching logic against them,
/// so a future edit that accidentally drops a sensitive prefix (e.g. removing "/Patient") makes
/// these tests fail. Previously every test here hardcoded its own frozen copy of these arrays and
/// asserted it against itself, which caught nothing if the real file drifted.
/// </summary>
public class ServiceWorkerTests
{
    private static readonly string ServiceWorkerScript = ReadRepoFile("wwwroot/service-worker.js");
    private static readonly IReadOnlyList<string> PrivateRoutePrefixes = ExtractStringArray(ServiceWorkerScript, "PRIVATE_ROUTE_PREFIXES");
    private static readonly IReadOnlyList<string> PublicRoutes = ExtractStringArray(ServiceWorkerScript, "PUBLIC_ROUTES");
    private static readonly IReadOnlyList<string> StaticAssets = ExtractStringArray(ServiceWorkerScript, "STATIC_ASSETS");

    [Fact]
    public void PrivateRoutePrefixes_AreActuallyDefinedInTheRealFile()
    {
        // Guards the extraction itself: if this ever comes back empty, every other test in this
        // class would pass vacuously, so fail loudly instead.
        Assert.NotEmpty(PrivateRoutePrefixes);
        Assert.NotEmpty(PublicRoutes);
        Assert.NotEmpty(StaticAssets);
    }

    [Theory]
    [InlineData("/Portal/Appointments", true)]
    [InlineData("/Admin/Dashboard", true)]
    [InlineData("/Account/Login", true)]
    [InlineData("/Identity/Account/Login", true)]
    [InlineData("/Patient/Documents", true)]
    [InlineData("/BillPayments", true)]
    [InlineData("/Donation/Receipt/42", true)]
    [InlineData("/uploads/patient-42.pdf", true)]
    [InlineData("/hubs/bookings", true)]
    [InlineData("/api/account/logout", true)]
    [InlineData("/api/patient/records", true)]
    [InlineData("/api/portal/appointments", true)]
    [InlineData("/about", false)]
    [InlineData("/doctors", false)]
    [InlineData("/contact", false)]
    [InlineData("/", false)]
    public void IsPrivateRoute_UsesTheRealPrefixList(string pathname, bool isSensitive)
    {
        var matches = IsPrivateRoute(pathname);

        Assert.Equal(isSensitive, matches);
    }

    [Theory]
    [InlineData("/", true)]
    [InlineData("/about", true)]
    [InlineData("/gallery", true)]
    [InlineData("/doctors", true)]
    [InlineData("/news", true)]
    [InlineData("/news/clinic-note", true)]
    [InlineData("/contact", false)]
    [InlineData("/Portal/Appointments", false)]
    [InlineData("/Admin/Dashboard", false)]
    public void ShouldCachePage_UsesTheRealPublicRouteList(string pathname, bool shouldCache)
    {
        var isPublic = PublicRoutes.Any(path =>
            pathname == path || (path != "/" && pathname.StartsWith($"{path}/")));

        Assert.Equal(shouldCache, isPublic);
    }

    [Fact]
    public void StaticAssets_IncludeCriticalOfflineShellFiles()
    {
        // APP_SHELL_URL is a named constant referenced inside STATIC_ASSETS rather than a repeated
        // string literal, so it's verified separately from the quoted-string array contents.
        var appShellMatch = Regex.Match(ServiceWorkerScript, "const APP_SHELL_URL = \"([^\"]+)\";");
        Assert.True(appShellMatch.Success, "Could not find the APP_SHELL_URL constant in service-worker.js.");
        Assert.Equal("/app-shell.html", appShellMatch.Groups[1].Value);
        Assert.Contains("APP_SHELL_URL", ServiceWorkerScript);

        Assert.Contains("/offline.html", StaticAssets);
        Assert.Contains("/offline-appointments.html", StaticAssets);
        Assert.Contains("/js/navigation.js", StaticAssets);
        Assert.Contains("/js/encrypted-offline-store.js", StaticAssets);
        Assert.Contains("/js/pwa-appointments.js", StaticAssets);
    }

    [Fact]
    public void FetchHandler_RejectsNonGetRequestsBeforeAnyCacheLogic()
    {
        // Antiforgery-bearing writes must go straight to the network; this must be the first
        // check in the fetch handler, not merely present somewhere in the file.
        Assert.Matches("self\\.addEventListener\\(\"fetch\"[\\s\\S]{0,400}method\\s*!==\\s*\"GET\"", ServiceWorkerScript);
    }

    [Fact]
    public void PrivateRoutes_AreServedNetworkOnly_NeverCacheFirst()
    {
        Assert.Matches("isPrivateRoute\\(url\\.pathname\\)[\\s\\S]{0,80}handleNetworkOnly", ServiceWorkerScript);
        Assert.Contains("cache: \"no-store\"", ServiceWorkerScript);
    }

    [Fact]
    public void HandleNavigation_SkipsCachingResponsesThatSetNoStore()
    {
        Assert.Contains("hasNoStore(response)", ServiceWorkerScript);
        Assert.Contains("Cache-Control", ServiceWorkerScript);
    }

    [Fact]
    public void ActivateHandler_DeletesCachesFromOlderVersionsOnly()
    {
        var versionMatch = Regex.Match(ServiceWorkerScript, "const VERSION = \"([^\"]+)\";");
        Assert.True(versionMatch.Success, "Could not find the VERSION constant in service-worker.js.");

        var version = versionMatch.Groups[1].Value;
        Assert.Matches("filter\\(\\(key\\) => !key\\.startsWith\\(VERSION\\)\\)", ServiceWorkerScript);

        // Sanity-check the extracted version reads like "okafor-pwa-vNN", not something malformed.
        Assert.Matches("^okafor-pwa-v\\d+$", version);
    }

    [Fact]
    public void NotificationClick_MatchesExistingClientsByPathname_NotFullUrl()
    {
        Assert.Contains("clientUrl.pathname === targetPathname", ServiceWorkerScript);
        Assert.Contains("self.clients.openWindow(targetUrl)", ServiceWorkerScript);
    }

    [Fact]
    public void PushHandler_FallsBackToDefaultsWhenPayloadFieldsAreMissing()
    {
        Assert.Contains("payload.title || defaults.title", ServiceWorkerScript);
        Assert.Contains("payload.body || defaults.body", ServiceWorkerScript);
        Assert.Contains("payload.url || defaults.url", ServiceWorkerScript);
    }

    private static bool IsPrivateRoute(string pathname)
    {
        var normalizedPath = pathname.ToLowerInvariant();
        return PrivateRoutePrefixes.Any(prefix =>
        {
            var normalizedPrefix = prefix.ToLowerInvariant();
            return normalizedPath == normalizedPrefix || normalizedPath.StartsWith($"{normalizedPrefix}/");
        });
    }

    private static IReadOnlyList<string> ExtractStringArray(string script, string constName)
    {
        var match = Regex.Match(script, $@"const\s+{constName}\s*=\s*\[(?<body>[\s\S]*?)\];");
        if (!match.Success)
        {
            throw new InvalidOperationException($"Could not find `const {constName} = [...]` in service-worker.js.");
        }

        return Regex.Matches(match.Groups["body"].Value, "\"([^\"]*)\"")
            .Select(m => m.Groups[1].Value)
            .ToList();
    }

    private static string ReadRepoFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find {relativePath} from {AppContext.BaseDirectory}.");
    }
}
