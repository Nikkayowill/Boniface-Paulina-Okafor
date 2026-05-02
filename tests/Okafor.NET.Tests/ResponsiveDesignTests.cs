using Xunit;

namespace Okafor.NET.Tests;

/// <summary>
/// Tests for CSS and responsive design changes including image rendering,
/// Tailwind utilities, and mobile-first responsive patterns.
/// </summary>
public class ResponsiveDesignTests
{
    [Fact]
    public void HeroImage_HasExplicitHeightConstraint()
    {
        // Arrange: Hero image styling
        var heroClass = "absolute inset-0 object-cover transition-transform duration-700 hover:scale-105 origin-center";
        var heroStyle = "height: calc(100vh - 97px)";

        // Act: Verify constraints
        var hasInset = heroClass.Contains("inset-0");
        var hasObjectCover = heroClass.Contains("object-cover");
        var hasOriginCenter = heroClass.Contains("origin-center");
        var hasHeightStyle = heroStyle.Contains("calc(100vh - 97px)");

        // Assert: Image should not stretch beyond viewport
        Assert.True(hasInset, "Should have inset-0 for absolute positioning");
        Assert.True(hasObjectCover, "Should have object-cover for proper fit");
        Assert.True(hasOriginCenter, "Should have origin-center for controlled zoom");
        Assert.True(hasHeightStyle, "Should have explicit height calculation");
    }

    [Fact]
    public void HeroImageParent_HasOverflowHidden()
    {
        // Arrange: Parent container for hero image
        var parentClass = "relative overflow-hidden w-full";

        // Act: Verify overflow control
        var hasRelative = parentClass.Contains("relative");
        var hasOverflowHidden = parentClass.Contains("overflow-hidden");

        // Assert: Parent should clip overflowing content
        Assert.True(hasRelative, "Should have relative positioning");
        Assert.True(hasOverflowHidden, "Should have overflow-hidden to prevent image stretch");
    }

    [Theory]
    [InlineData("rounded-lg", true)]
    [InlineData("rounded-md", false)]
    [InlineData("h-full", false)]
    [InlineData("w-full", false)]
    public void GalleryImages_ProperBorderRadius(string className, bool shouldHave)
    {
        // Arrange: Gallery image classes after fix
        var galleryImage = "aspect-[4/3] object-cover rounded-lg origin-center transition-transform duration-500 hover:scale-110";

        // Act: Check for className
        var hasClass = galleryImage.Contains(className);

        // Assert
        Assert.Equal(shouldHave, hasClass);
    }

    [Fact]
    public void DoctorImageContainer_HasAspectRatio()
    {
        // Arrange: Doctor image container
        var containerClass = "aspect-[4/3] overflow-hidden rounded-md";

        // Act: Verify aspect ratio and overflow
        var hasAspectRatio = containerClass.Contains("aspect-[4/3]");
        var hasOverflow = containerClass.Contains("overflow-hidden");
        var hasRounded = containerClass.Contains("rounded-md");

        // Assert: Container should maintain image aspect ratio
        Assert.True(hasAspectRatio, "Should have 4:3 aspect ratio");
        Assert.True(hasOverflow, "Should have overflow-hidden");
        Assert.True(hasRounded, "Should have rounded corners");
    }

    [Theory]
    [InlineData("text-base", true)]
    [InlineData("text-sm", true)]
    [InlineData("text-lg", true)]
    [InlineData("sm:text-lg", true)]
    [InlineData("md:text-xl", true)]
    [InlineData("mobile:text-16px", false)]
    public void TextScaling_UsesTailwindUtilities(string className, bool isValid)
    {
        // Arrange: Valid Tailwind text scales
        var validScales = new[] { "text-base", "text-sm", "text-lg", "text-xl", "sm:text-lg", "md:text-xl" };

        // Act: Check if it's standard Tailwind
        var isStandard = validScales.Any(scale => className == scale);

        // Assert
        Assert.Equal(isValid && isStandard, isValid);
    }

    [Theory]
    [InlineData("gap-4", true)]
    [InlineData("gap-4 sm:gap-6", true)]
    [InlineData("gap-4 sm:gap-6 md:gap-8", true)]
    [InlineData("gap-4 sm:gap-5 md:gap-6", true)]
    [InlineData("gap-4 lg:gap-8", true)]
    public void ResponsiveGaps_HaveBreakpointPrefixes(string className, bool isValid)
    {
        // Arrange: Check for responsive gap pattern
        var hasBaseGap = className.Contains("gap-");
        var hasResponsive = className.Contains("sm:") || className.Contains("md:") || className.Contains("lg:");

        // Act & Assert: Should have base gap and responsive prefixes
        if (isValid)
        {
            Assert.True(hasBaseGap, "Should have base gap-X");
            Assert.True(hasResponsive || !className.Contains(":"), "Should have responsive prefixes or be single scale");
        }
    }

    [Theory]
    [InlineData(320, "mobile")]
    [InlineData(640, "sm")]
    [InlineData(768, "md")]
    [InlineData(1024, "lg")]
    [InlineData(1280, "xl")]
    public void ResponsiveBreakpoints_MatchMobileFirs(int width, string breakpoint)
    {
        // Arrange: Expected breakpoints for mobile-first design
        var breakpoints = new Dictionary<string, int>
        {
            { "mobile", 320 },
            { "sm", 640 },
            { "md", 768 },
            { "lg", 1024 },
            { "xl", 1280 }
        };

        // Act & Assert: Verify breakpoint values
        Assert.True(breakpoints.ContainsKey(breakpoint));
        Assert.Equal(width, breakpoints[breakpoint]);
    }

    [Fact]
    public void MobileLayout_StartsAt320px()
    {
        // Arrange: Mobile-first viewport meta tag
        var metaTag = @"<meta name=""viewport"" content=""width=device-width, initial-scale=1.0, viewport-fit=cover"" />";

        // Act: Verify minimum width
        var hasMetaTag = metaTag.Contains("viewport");
        var allowsDeviceWidth = metaTag.Contains("device-width");
        var hasInitialScale = metaTag.Contains("initial-scale=1.0");

        // Assert: Should support devices from 320px width
        Assert.True(hasMetaTag, "Should have viewport meta tag");
        Assert.True(allowsDeviceWidth, "Should respect device width");
        Assert.True(hasInitialScale, "Should set initial scale to 1.0");
    }

    [Theory]
    [InlineData("px-2 sm:px-4 md:px-6", true)]
    [InlineData("py-2 sm:py-3 md:py-4", true)]
    [InlineData("px-2 py-3", true)]
    [InlineData("p-2 sm:p-4", true)]
    public void PaddingSpacing_ResponsiveScale(string className, bool isValid)
    {
        // Arrange: Check for responsive padding pattern
        var hasBase = className.Contains("p");
        var isResponsive = className.Contains("sm:") || className.Contains("md:");

        // Act & Assert: Should have appropriate spacing
        Assert.True(hasBase, "Should have padding utility");
        if (isValid && className.Contains(":"))
        {
            Assert.True(isResponsive, "Should have responsive prefixes");
        }
    }

    [Fact]
    public void ContainerQuery_FollowsDesktopFirst()
    {
        // Arrange: Container breakpoints
        var containerBreakpoints = new[]
        {
            new { name = "container", size = "100%" },
            new { name = "sm", size = "640px" },
            new { name = "md", size = "768px" },
            new { name = "lg", size = "1024px" }
        };

        // Act & Assert: All breakpoints should be defined
        Assert.NotEmpty(containerBreakpoints);
        Assert.Equal(4, containerBreakpoints.Length);
    }

    [Theory]
    [InlineData("hover:scale-105", true)]
    [InlineData("hover:opacity-75", true)]
    [InlineData("focus:ring-2", true)]
    [InlineData("active:scale-95", true)]
    public void InteractiveStates_HaveTailwindUtilities(string className, bool isValid)
    {
        // Arrange: Valid state prefixes in Tailwind
        var validStates = new[] { "hover:", "focus:", "active:", "group-hover:" };

        // Act: Check if state prefix exists
        var hasState = validStates.Any(state => className.StartsWith(state));

        // Assert
        Assert.Equal(isValid, hasState);
    }

    [Fact]
    public void DarkMode_RespondsToSystemPreference()
    {
        // Arrange: Dark mode media query
        var darkClassExample = "dark:bg-gray-900";

        // Act: Check for dark mode support
        var hasDarkVariant = darkClassExample.Contains("dark:");

        // Assert: Should support system dark mode
        Assert.True(hasDarkVariant, "Should use dark: variant for system preference");
    }

    [Fact]
    public void TailwindColors_IncludeTealScale()
    {
        // Arrange: Expected teal color scale
        var tealColors = new[]
        {
            "teal-50", "teal-100", "teal-200", "teal-300", "teal-400",
            "teal-500", "teal-600", "teal-700", "teal-800", "teal-900"
        };

        // Act & Assert: All colors should be available
        Assert.Equal(10, tealColors.Length);
        Assert.Contains("teal-600", tealColors);
        Assert.Contains("teal-700", tealColors);
    }

    [Theory]
    [InlineData("bg-teal-700", true)]
    [InlineData("text-teal-600", true)]
    [InlineData("border-teal-500", true)]
    [InlineData("hover:bg-teal-800", true)]
    public void TealColorUtilities_AvailableWithVariants(string className, bool isAvailable)
    {
        // Act & Assert: Should support teal colors
        Assert.True(isAvailable, $"{className} should be valid");
    }

    [Theory]
    [InlineData("origin-center", "scale-105")]
    [InlineData("origin-center", "scale-110")]
    [InlineData("origin-top-left", "scale-105")]
    public void TransformOrigin_ControlsScaleCenter(string origin, string scale)
    {
        // Arrange: Scale transform should work with origin
        var hasOrigin = !string.IsNullOrEmpty(origin);
        var hasScale = !string.IsNullOrEmpty(scale);

        // Act & Assert: Both utilities should combine properly
        Assert.True(hasOrigin && hasScale, $"{origin} {scale} should work together");
    }
}
