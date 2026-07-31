using Xunit;

namespace Okafor.NET.Tests;

/// <summary>
/// Tests for CSS and responsive design on the public landing page. Every assertion reads the real
/// shipped Razor view or stylesheet from disk. Previously these tests built a hardcoded string
/// literal (some describing a hero implementation -- an absolutely-positioned background image --
/// that no longer exists now that the hero is a carousel) and asserted it against itself, which
/// passed no matter what the real files contained.
/// </summary>
public class ResponsiveDesignTests
{
    [Fact]
    public void Layout_ViewportMeta_SupportsMobileFirstFrom320px()
    {
        var layout = ReadRepoFile("Views/Shared/_Layout.cshtml");

        Assert.Contains("name=\"viewport\"", layout);
        Assert.Contains("width=device-width", layout);
        Assert.Contains("initial-scale=1.0", layout);
    }

    [Fact]
    public void HeroCarousel_Slides_HaveResponsiveMobileSource()
    {
        var view = ReadRepoFile("Views/Home/Index.cshtml");

        // Each hero slide serves a dedicated mobile image via <picture><source>, not just a
        // scaled-down desktop asset, which matters on the slow connections this hospital's
        // patients commonly have.
        Assert.Contains("<source media=\"(max-width: 719px)\"", view);
        Assert.Contains("data-carousel-track", view);
        Assert.Contains("data-carousel-viewport", view);
    }

    [Fact]
    public void HeroCarousel_ControlButtons_MeetFortyFourPixelTouchTarget()
    {
        var css = ReadRepoFile("wwwroot/css/public-site.css");

        Assert.Matches("\\.hospital-hero__controls button[\\s\\S]{0,400}min-height:\\s*44px", css);
        Assert.Matches("\\.hospital-hero__controls button[\\s\\S]{0,400}min-width:\\s*44px", css);
    }

    [Fact]
    public void InteractiveElements_HaveVisibleFocusIndicator()
    {
        var css = ReadRepoFile("wwwroot/css/public-site.css");

        Assert.Contains(".hospital-home a:focus-visible,", css);
        Assert.Contains(".hospital-home button:focus-visible {", css);
    }

    [Fact]
    public void PublicSite_RespectsPrefersReducedMotion()
    {
        var css = ReadRepoFile("wwwroot/css/public-site.css");

        Assert.Contains("@media (prefers-reduced-motion: reduce)", css);
    }

    [Fact]
    public void HeroCarousel_PausesAutoplay_ForReducedMotionUsers()
    {
        var script = ReadRepoFile("wwwroot/js/hero-carousel.js");

        Assert.Contains("prefers-reduced-motion", script);
    }

    [Fact]
    public void PublicSite_HasDedicatedMobileBreakpoint()
    {
        var css = ReadRepoFile("wwwroot/css/public-site.css");

        Assert.Contains("@media (max-width: 719.98px) {", css);
    }

    [Fact]
    public void PublicSite_DefinesHospitalColorTokens()
    {
        var css = ReadRepoFile("wwwroot/css/public-site.css");

        // The palette is a multi-hue token system (teal/green/clay/gold/coral), not a single
        // Tailwind teal-* scale -- confirming the tokens exist keeps this test honest about what
        // the design system actually is.
        Assert.Contains("--hospital-teal:", css);
        Assert.Contains("--hospital-green:", css);
        Assert.Contains("--hospital-gold:", css);
        Assert.Contains("--hospital-coral:", css);
        Assert.Contains("--hospital-radius:", css);
    }

    [Fact]
    public void GalleryImages_CoverTheirFrameWithoutDistortion()
    {
        var css = ReadRepoFile("wwwroot/css/public-site.css");

        Assert.Matches("\\.hospital-gallery img[\\s\\S]{0,120}object-fit:\\s*cover", css);
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
