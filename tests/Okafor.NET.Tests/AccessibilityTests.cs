using Xunit;

namespace Okafor.NET.Tests;

/// <summary>
/// Tests for accessibility behavior in views and components. Every assertion here reads the real
/// shipped file (view, CSS, or JS) from disk and checks its actual content -- previously these tests
/// built a hardcoded string literal describing the intended markup and asserted it against itself,
/// which passed regardless of what the real files contained and caught nothing when they drifted.
/// </summary>
public class AccessibilityTests
{
    [Fact]
    public void OfflineAppointmentsView_EmptyState_HasAriaLiveRegion()
    {
        var html = ReadRepoFile("wwwroot/offline-appointments.html");

        Assert.Contains("data-offline-appointments-empty", html);
        Assert.Contains("aria-live=\"polite\"", html);
        Assert.Contains("role=\"status\"", html);
    }

    [Fact]
    public void OfflineAppointmentsList_Container_HasAriaAttributes()
    {
        var html = ReadRepoFile("wwwroot/offline-appointments.html");

        Assert.Contains("data-offline-appointments-list", html);
        Assert.Contains("aria-live=\"polite\"", html);
        Assert.Contains("aria-label=\"Appointment list\"", html);
    }

    [Fact]
    public void PatientInformationHub_Sidebar_HasKeyboardFocusManagement()
    {
        var view = ReadRepoFile("Views/Home/PatientInformationHub.cshtml");

        Assert.Contains("closeButton.focus()", view);
        Assert.Contains("openButton.focus()", view);
        Assert.Contains("event.key === 'Escape'", view);
    }

    [Fact]
    public void PatientInformationHub_Backdrop_ExistsAndStartsHidden()
    {
        var view = ReadRepoFile("Views/Home/PatientInformationHub.cshtml");

        Assert.Contains("id=\"hub-sidebar-backdrop\"", view);
        Assert.Contains("hub-drawer-backdrop", view);
        // Tailwind's "hidden" utility class keeps the backdrop out of the accessibility tree and
        // click path until the sidebar JS opens it.
        Assert.Matches("id=\"hub-sidebar-backdrop\"[^>]*\\bhidden\\b", view);
    }

    [Fact]
    public void Layout_ManifestLink_UsesUrlContentForPathResolution()
    {
        var layout = ReadRepoFile("Views/Shared/_Layout.cshtml");

        Assert.Contains("<link rel=\"manifest\" href=\"@Url.Content(\"~/site.webmanifest\")\" />", layout);
    }

    [Fact]
    public void Layout_AppleTouchIcon_UsesPngNotFavicon()
    {
        var layout = ReadRepoFile("Views/Shared/_Layout.cshtml");

        Assert.Contains("rel=\"apple-touch-icon\"", layout);
        Assert.Contains("apple-touch-icon.png", layout);
    }

    [Fact]
    public void PortalCss_TableResponsiveScrollHint_IsLocalizableViaDataAttribute()
    {
        var css = ReadRepoFile("wwwroot/css/portal.css");

        Assert.Contains(".table-responsive::before", css);
        Assert.Contains("content: attr(data-scroll-hint);", css);
        // The hint text itself must come from markup (a translatable data-scroll-hint attribute),
        // not be baked into the stylesheet as hardcoded English.
        Assert.DoesNotContain("content: \"Scroll", css);
    }

    [Fact]
    public void TeleconsultationForm_ValidationMessages_AreVisuallyDistinctAndDescriptive()
    {
        var view = ReadRepoFile("Views/Teleconsultations/Create.cshtml");

        Assert.Contains("asp-validation-for=\"PatientName\"", view);
        // asp-validation-for spans render the real per-field error text at runtime; here we only
        // confirm they carry a visible error color so messages aren't invisible to sighted users.
        Assert.Matches("asp-validation-for=\"PatientName\"[^>]*text-red-600", view);
    }

    [Fact]
    public void MinTouchTarget_Utility_MeetsFortyFourPixelMinimum()
    {
        var css = ReadRepoFile("wwwroot/css/portal.css");

        Assert.Contains(".min-touch-target", css);
        Assert.Matches("\\.min-touch-target[\\s\\S]{0,200}min-height:\\s*44px", css);
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
