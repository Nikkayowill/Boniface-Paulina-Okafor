using System.Text.RegularExpressions;
using Xunit;

namespace Okafor.NET.Tests;

/// <summary>
/// WCAG 2.1 contrast-ratio checks for the real text/background color pairs the public site
/// actually renders (extracted from wwwroot/css/site.css and wwwroot/css/tailwind.input.css,
/// not hand-picked or guessed). This closes a gap flagged during the 2026-07 launch review:
/// contrast had no automated check anywhere in the repo, only ad-hoc manual review.
/// </summary>
public class ColorContrastTests
{
    private const double NormalTextMinimum = 4.5; // WCAG 2.1 AA, text under 18.66px bold / 24px regular
    private static readonly string SiteCss = ReadRepoFile("wwwroot/css/site.css");
    private static readonly string TailwindInput = ReadRepoFile("wwwroot/css/tailwind.input.css");

    [Fact]
    public void ContrastCalculator_MatchesKnownWcagReferenceValues()
    {
        // Guards the calculator itself before trusting it against real site colors.
        Assert.Equal(21.0, ContrastRatio("#000000", "#ffffff"), 1);
        Assert.Equal(1.0, ContrastRatio("#155e75", "#155e75"), 1);
    }

    [Theory]
    [InlineData("--ok-ink", "#f8fafc", "Body text on the page background")]
    [InlineData("--ok-muted", "#ffffff", "Muted/caption text on white cards")]
    [InlineData("--ok-sky-700", "#ffffff", "Kicker/eyebrow text (.hospital-kicker, .ok-kicker)")]
    [InlineData("--ok-sky-950", "#ffffff", "site-button--secondary / --light label text")]
    public void SiteCssColor_OnWhiteOrPaper_MeetsAaForNormalText(string tokenName, string background, string usage)
    {
        var foreground = ExtractSiteCssColor(tokenName);

        var ratio = ContrastRatio(foreground, background);

        Assert.True(ratio >= NormalTextMinimum,
            $"{usage}: {tokenName} ({foreground}) on {background} is {ratio:F2}:1, below the {NormalTextMinimum}:1 AA minimum for normal text.");
    }

    [Fact]
    public void SiteButtonPrimary_WhiteLabelOnSkyEight_MeetsAaForNormalText()
    {
        var background = ExtractSiteCssColor("--ok-sky-800");

        var ratio = ContrastRatio("#ffffff", background);

        Assert.True(ratio >= NormalTextMinimum,
            $".site-button--primary label: white on --ok-sky-800 ({background}) is {ratio:F2}:1, below {NormalTextMinimum}:1.");
    }

    [Fact]
    public void TopUtilityBar_PrimaryOneHundredOnPrimaryNineFifty_MeetsAaForNormalText()
    {
        // Views/Shared/_Layout.cshtml renders the top utility bar as
        // bg-primary-950 with text-primary-100 links (Tailwind classes, not --ok-* tokens).
        var foreground = ExtractTailwindThemeColor("--color-primary-100");
        var background = ExtractTailwindThemeColor("--color-primary-950");

        var ratio = ContrastRatio(foreground, background);

        Assert.True(ratio >= NormalTextMinimum,
            $"Top utility bar: text-primary-100 ({foreground}) on bg-primary-950 ({background}) is {ratio:F2}:1, below {NormalTextMinimum}:1.");
    }

    private static double ContrastRatio(string hexA, string hexB)
    {
        var lumA = RelativeLuminance(hexA);
        var lumB = RelativeLuminance(hexB);
        var (lighter, darker) = lumA >= lumB ? (lumA, lumB) : (lumB, lumA);
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double RelativeLuminance(string hex)
    {
        hex = hex.TrimStart('#');
        var r = LinearizeChannel(Convert.ToInt32(hex[..2], 16) / 255.0);
        var g = LinearizeChannel(Convert.ToInt32(hex[2..4], 16) / 255.0);
        var b = LinearizeChannel(Convert.ToInt32(hex[4..6], 16) / 255.0);
        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    }

    private static double LinearizeChannel(double channel) =>
        channel <= 0.03928 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);

    private static string ExtractSiteCssColor(string tokenName)
    {
        var match = Regex.Match(SiteCss, $@"{Regex.Escape(tokenName)}:\s*(#[0-9a-fA-F]{{6}});");
        if (!match.Success)
        {
            throw new InvalidOperationException($"Could not find `{tokenName}: #......;` in site.css.");
        }

        return match.Groups[1].Value;
    }

    private static string ExtractTailwindThemeColor(string tokenName)
    {
        var match = Regex.Match(TailwindInput, $@"{Regex.Escape(tokenName)}:\s*(#[0-9a-fA-F]{{6}});");
        if (!match.Success)
        {
            throw new InvalidOperationException($"Could not find `{tokenName}: #......;` in tailwind.input.css.");
        }

        return match.Groups[1].Value;
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
