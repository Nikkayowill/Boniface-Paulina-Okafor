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
    public void HomeHero_IncludesApprovedWelcomeBeliefAndFacilityPhotography()
    {
        var view = ReadRepoFile("Views/Home/Index.cshtml");

        Assert.Contains("Welcome to <strong>B&amp;P Hospital</strong>", view);
        Assert.Contains("Here we provide holistic healthcare.", view);
        Assert.Contains("Our core belief:", view);
        Assert.Contains("Health is Wealth!", view);
        Assert.Contains("IWFI7760.webp", view);
        Assert.Contains("TBMP5109.webp", view);
        Assert.Contains("FXGP9714.webp", view);
        Assert.True(
            view.IndexOf("IWFI7760.webp", StringComparison.Ordinal) <
            view.IndexOf("WVHK5210.webp", StringComparison.Ordinal));
        Assert.Contains("hospital-overview", view);
        Assert.DoesNotContain("hospital-gallery", view);
    }

    [Fact]
    public void TeamPage_UsesCompactMedicalOfficerCardAndDedicatedProfileRoute()
    {
        var view = ReadRepoFile("Views/Home/Team.cshtml");
        var profile = ReadRepoFile("Views/Home/DoctorProfile.cshtml");
        var controller = ReadRepoFile("Controllers/HomeController.cs");

        Assert.Contains("Dr. Opie Thomas N.", view);
        Assert.Contains("Medical Officer", view);
        Assert.Contains("General Practitioner", view);
        Assert.Contains("site-card team-feature-profile mb-12 grid", view);
        Assert.Contains("Dr. Opie Thomas N. is a Nigerian medical officer", view);
        Assert.Contains("team-feature-profile__bio", view);
        Assert.Contains("asp-route-slug=\"dr-opie-thomas-n\"", view);
        Assert.DoesNotContain("Benue State University, Makurdi", view);
        Assert.Contains("Benue State University, Makurdi", controller);
        Assert.Contains("Clinical care &amp; emergency", profile);
        Assert.DoesNotContain("JSSCE", view);
        Assert.DoesNotContain("FSLC", view);
    }

    [Fact]
    public void WhatsAppButton_UsesAdministratorNumberMessageAndRequiredStackingOrder()
    {
        var settings = ReadRepoFile("appsettings.json");
        var layout = ReadRepoFile("Views/Shared/_Layout.cshtml");
        var css = ReadRepoFile("wwwroot/css/site.css");

        Assert.Contains("+2349042929406", settings);
        Assert.Contains("Hello B&P Hospital, I have an inquiry.", layout);
        Assert.Matches("\\.whatsapp-float[\\s\\S]{0,900}z-index:\\s*1000", css);
    }

    [Fact]
    public void HeroCarousel_UsesMinimalIndicatorsWithoutArrowOrPauseControls()
    {
        var view = ReadRepoFile("Views/Home/Index.cshtml");

        Assert.Contains("data-carousel-go", view);
        Assert.DoesNotContain("data-carousel-prev", view);
        Assert.DoesNotContain("data-carousel-next", view);
        Assert.DoesNotContain("data-carousel-toggle", view);
    }

    [Fact]
    public void LandingPage_KeepsNewsAndHealthEducationOnTheirDedicatedPages()
    {
        var view = ReadRepoFile("Views/Home/Index.cshtml");
        var layout = ReadRepoFile("Views/Shared/_Layout.cshtml");

        Assert.DoesNotContain("Stories and updates", view);
        Assert.DoesNotContain("Health education", view);
        Assert.Contains("asp-action=\"News\"", layout);
    }

    [Fact]
    public void FatherToochukwuProfile_HasAStableVerifiedFallback()
    {
        var controller = ReadRepoFile("Controllers/HomeController.cs");
        var teamView = ReadRepoFile("Views/Home/Team.cshtml");

        Assert.Contains("CreateFatherToochukwuProfile", controller);
        Assert.Contains("Fr. Toochukwu Okafor was born in Isuochi", controller);
        Assert.Contains("@fatherToochukwu.Bio", teamView);
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
    public void HeroCarousel_SupportsHorizontalTouchSwiping()
    {
        var script = ReadRepoFile("wwwroot/js/hero-carousel.js");
        var css = ReadRepoFile("wwwroot/css/public-site.css");

        Assert.Contains("touchstart", script);
        Assert.Contains("touchend", script);
        Assert.Contains("touch-action: pan-y", css);
    }

    [Fact]
    public void UtilityNavigation_SeparatesPatientToolsFromPrimaryNavigation()
    {
        var layout = ReadRepoFile("Views/Shared/_Layout.cshtml");
        var utilityStart = layout.IndexOf("<!-- TOP UTILITY BAR -->", StringComparison.Ordinal);
        var utilityEnd = layout.IndexOf("<!-- MAIN NAVIGATION -->", StringComparison.Ordinal);
        var utilityMarkup = layout[utilityStart..utilityEnd];
        var primaryStart = layout.IndexOf("<!-- Desktop nav -->", StringComparison.Ordinal);
        var primaryEnd = layout.IndexOf("<!-- Book Appointment CTA -->", StringComparison.Ordinal);
        var primaryMarkup = layout[primaryStart..primaryEnd];

        Assert.Contains("tel:@hospitalPhone", utilityMarkup);
        Assert.Contains("My Portal", utilityMarkup);
        Assert.Contains("Newsletter", utilityMarkup);
        Assert.Contains("Search", utilityMarkup);
        Assert.Contains("Donate", utilityMarkup);
        Assert.DoesNotContain("asp-action=\"Search\"", primaryMarkup);
        Assert.Contains("aria-label=\"Search hospital information\"", layout);
        Assert.Contains("mobile-header-actions", layout);
        Assert.Contains("Donate monthly", layout);

        var css = ReadRepoFile("wwwroot/css/site.css");
        Assert.Matches("@media \\(max-width: 1023\\.98px\\)[\\s\\S]{0,100}\\.top-utility-bar[\\s\\S]{0,50}display:\\s*none", css);
        Assert.Matches("@media \\(max-width: 1023\\.98px\\)[\\s\\S]{0,180}\\.mobile-header-actions[\\s\\S]{0,50}display:\\s*flex", css);
        Assert.Matches("\\.mobile-header-actions[\\s\\S]{0,100}display:\\s*none", css);
        Assert.Contains(":where(svg:not([class]):not([width]):not([height]))", css);
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
