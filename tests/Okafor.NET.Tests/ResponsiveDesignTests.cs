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
    public void HeroPhoto_ServesADedicatedMobileCrop()
    {
        // The landing page lives in the React tree (client/landing) that
        // Views/Home/Index.cshtml hydrates -- see
        // docs/decisions/0006-react-landing-page-non-headless.md.
        var hero = ReadRepoFile("client/landing/components/Hero.jsx");

        // Phones get their own pre-cropped photo rather than a scaled-down desktop
        // band, which matters on the slow connections this hospital's patients often have.
        Assert.Contains("media=\"(max-width: 719px)\"", hero);
        Assert.Contains("hero-photo-mobile-804.webp", hero);
        Assert.Contains("hero-photo-1920.webp", hero);
    }

    [Fact]
    public void HomeHero_UsesTheApprovedFigmaCopy()
    {
        var hero = ReadRepoFile("client/landing/components/Hero.jsx");
        var whyDifferent = ReadRepoFile("client/landing/components/WhyDifferent.jsx");

        Assert.Contains("<span className=\"home-hero__title-thin\">Health is</span> <strong>Wealth.</strong>", hero);
        Assert.Contains("Here, <strong>Your Life</strong> Matters More Than<strong> A Budget.</strong>", hero);
        Assert.Contains("What Makes Us Different?", whyDifferent);
        Assert.Contains("Medicine for Your Body, Support for Your Mind, Hope for Your Spirit.", whyDifferent);
    }

    [Fact]
    public void HomeBooking_AlwaysOffersBothAVisitAndATeleconsultation()
    {
        var chooser = ReadRepoFile("client/landing/components/BookingChooser.jsx");
        var hero = ReadRepoFile("client/landing/components/Hero.jsx");
        var services = ReadRepoFile("client/landing/components/ServiceCards.jsx");

        Assert.Contains("urls.appointmentCreate", chooser);
        Assert.Contains("urls.teleconsultationCreate", chooser);
        Assert.Contains("<BookingChooser", hero);
        Assert.Contains("<BookingChooser", services);
        Assert.DoesNotContain("urls.appointmentCreate", hero);
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
    public void UtilityNavigation_SeparatesPatientToolsFromPrimaryNavigation()
    {
        var layout = ReadRepoFile("Views/Shared/_Layout.cshtml");
        var utilityStart = layout.IndexOf("<!-- TOP UTILITY BAR -->", StringComparison.Ordinal);
        var utilityEnd = layout.IndexOf("<!-- MAIN NAVIGATION -->", StringComparison.Ordinal);
        var utilityMarkup = layout[utilityStart..utilityEnd];
        var primaryStart = layout.IndexOf("<!-- Desktop nav -->", StringComparison.Ordinal);
        var primaryEnd = layout.IndexOf("<!-- Mobile menu toggle -->", StringComparison.Ordinal);
        var primaryMarkup = layout[primaryStart..primaryEnd];
        var mobileMenuStart = layout.IndexOf("<!-- Mobile menu -->", StringComparison.Ordinal);
        var mobileMenuMarkup = layout[mobileMenuStart..layout.IndexOf("</header>", StringComparison.Ordinal)];

        Assert.Contains("Emergency Line", utilityMarkup);
        Assert.Contains("Newsletter", utilityMarkup);
        Assert.Contains("Donate", utilityMarkup);
        Assert.Contains("Log in", utilityMarkup);
        Assert.Contains("My Portal", utilityMarkup);

        foreach (var label in new[] { ">Home<", ">About Us<", ">Services<", ">Gallery<", ">Contact Us<" })
        {
            Assert.Contains(label, primaryMarkup);
        }
        Assert.DoesNotContain("asp-action=\"Search\"", primaryMarkup);

        // Team and Search stay reachable from the menu; News, Patient Information and
        // bill payment are deliberately left out of the menu for now.
        Assert.Contains("aria-label=\"Search hospital information\"", mobileMenuMarkup);
        Assert.Contains("asp-action=\"Team\"", mobileMenuMarkup);
        Assert.DoesNotContain("asp-action=\"PatientInformationHub\"", mobileMenuMarkup);
        Assert.DoesNotContain("asp-action=\"News\"", mobileMenuMarkup);
        Assert.DoesNotContain("BillPayments", mobileMenuMarkup);

        var css = ReadRepoFile("wwwroot/css/site.css");
        Assert.Matches("@media \\(max-width: 1023\\.98px\\)[\\s\\S]{0,100}\\.site-utility__actions[\\s\\S]{0,50}display:\\s*none", css);
        Assert.Matches("@media \\(max-width: 1023\\.98px\\)[\\s\\S]{0,400}\\.site-header__menu-button[\\s\\S]{0,50}display:\\s*inline-flex", css);
        Assert.Matches("\\.site-header__menu-button \\{[\\s\\S]{0,100}display:\\s*none", css);
        Assert.Contains(":where(svg:not([class]):not([width]):not([height]))", css);
    }

    [Fact]
    public void PublicSite_HasDedicatedMobileBreakpoint()
    {
        var css = ReadRepoFile("wwwroot/css/public-site.css");

        Assert.Contains("@media (max-width: 719.98px) {", css);
    }

    [Fact]
    public void SiteChrome_DefinesTheFigmaPaletteAndTypefaces()
    {
        var css = ReadRepoFile("wwwroot/css/site.css");
        var layout = ReadRepoFile("Views/Shared/_Layout.cshtml");

        Assert.Contains("--bp-green: #047760;", css);
        Assert.Contains("--bp-cream: #f5f0e1;", css);
        Assert.Contains("--bp-night: #022720;", css);
        Assert.Contains("--bp-mint: #76fadf;", css);
        Assert.Contains("family=Roboto+Serif", layout);
        Assert.Contains("family=Roboto+Condensed", layout);
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
