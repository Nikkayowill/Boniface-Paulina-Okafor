using System.Text.RegularExpressions;

namespace Okafor_.NET.Tests;

/// <summary>
/// WCAG 2.1 contrast checks for the portal palette.
///
/// ColorContrastTests covers the public site (site.css and tailwind.input.css).
/// portal.css had no equivalent, which mattered once the portals moved off the
/// public navy/bronze scheme onto green and sky blue: every text pair was
/// re-picked, and "it looks fine" is not a check. Each pair below is read out
/// of the stylesheet by token name, so changing a token re-runs the check
/// rather than silently invalidating it.
///
/// The portals are read by patients who are often older and often looking for a
/// single fact, so the floor here is AA for normal text everywhere it applies.
/// </summary>
public class PortalContrastTests
{
    private const double NormalTextMinimum = 4.5;   // WCAG 2.1 AA, normal-size text
    private const double NonTextMinimum = 3.0;      // WCAG 2.1 AA, UI components and graphics

    private static readonly string PortalCss = ReadRepoFile("wwwroot/css/portal.css");

    [Theory]
    // On the page ground.
    [InlineData("--portal-ink", "--portal-paper", "Body text on the page")]
    [InlineData("--portal-muted", "--portal-paper", "Secondary text: ledes, row meta, leader labels")]
    [InlineData("--portal-faint", "--portal-paper", "Recessive text: register captions, placeholders, a zero count")]
    [InlineData("--portal-green-700", "--portal-paper", "The accent carrying text: eyebrows, notice keys, filter keys")]
    [InlineData("--portal-green-800", "--portal-paper", "\"Confirmed\" on a standing mark")]
    [InlineData("--portal-amber-700", "--portal-paper", "The outline-warning label")]
    [InlineData("--portal-amber-800", "--portal-paper", "\"Awaiting\" on a standing mark")]
    [InlineData("--portal-ox", "--portal-paper", "Danger text and validation errors")]
    [InlineData("--portal-sky-800", "--portal-paper", "Links and the info mark's ink")]
    // On a white panel — the plate, the docket, the empty state.
    [InlineData("--portal-ink", "--portal-panel", "Body text on a panel")]
    [InlineData("--portal-muted", "--portal-panel", "Secondary text on a panel")]
    [InlineData("--portal-faint", "--portal-panel", "Recessive text on a panel")]
    [InlineData("--portal-green-700", "--portal-panel", "Notice keys on a panel")]
    [InlineData("--portal-sky-800", "--portal-panel", "The plate's hour, and links on a panel")]
    // On the green tint the plate's preparation note uses.
    [InlineData("--portal-green-800", "--portal-green-100", "The preparation note inside a visit plate")]
    public void PortalText_MeetsAaForNormalText(string foregroundToken, string backgroundToken, string usage)
    {
        var foreground = Token(foregroundToken);
        var background = Token(backgroundToken);

        var ratio = ContrastRatio(foreground, background);

        Assert.True(ratio >= NormalTextMinimum,
            $"{usage}: {foregroundToken} ({foreground}) on {backgroundToken} ({background}) is {ratio:F2}:1, below the {NormalTextMinimum}:1 AA minimum for normal text.");
    }

    [Theory]
    [InlineData("#ffffff", "--portal-sky-800", "White label on a primary button")]
    [InlineData("#ffffff", "--portal-sky-950", "White label on a primary button, hovered, and the rail's name")]
    [InlineData("#ffffff", "--portal-green-700", "White label on a success button")]
    [InlineData("#ffffff", "--portal-ox", "White label on a danger button")]
    public void ButtonLabels_MeetAaForNormalText(string foreground, string backgroundToken, string usage)
    {
        var background = Token(backgroundToken);

        var ratio = ContrastRatio(foreground, background);

        Assert.True(ratio >= NormalTextMinimum,
            $"{usage}: {foreground} on {backgroundToken} ({background}) is {ratio:F2}:1, below {NormalTextMinimum}:1.");
    }

    [Theory]
    [InlineData("--portal-rail-ink", "The rail's navigation links")]
    [InlineData("--portal-rail-quiet", "The rail's group headings and the signed-in name")]
    [InlineData("--portal-green-300", "The rail's \"which portal\" line and its section counts")]
    public void RailText_MeetsAaAgainstTheRail(string foregroundToken, string usage)
    {
        var foreground = Token(foregroundToken);
        var rail = Token("--portal-sky-950");

        var ratio = ContrastRatio(foreground, rail);

        Assert.True(ratio >= NormalTextMinimum,
            $"{usage}: {foregroundToken} ({foreground}) on the rail ({rail}) is {ratio:F2}:1, below {NormalTextMinimum}:1.");
    }

    [Theory]
    [InlineData("--portal-green-600", "--portal-paper", "The focus ring, title rules, act underlines, the plate's top border")]
    [InlineData("--portal-green-600", "--portal-panel", "The same accent drawn on a panel")]
    [InlineData("--portal-amber-600", "--portal-paper", "The diamond on a \"waiting\" standing mark")]
    public void PortalOrnament_MeetsAaForNonTextContrast(string foregroundToken, string backgroundToken, string usage)
    {
        var foreground = Token(foregroundToken);
        var background = Token(backgroundToken);

        var ratio = ContrastRatio(foreground, background);

        Assert.True(ratio >= NonTextMinimum,
            $"{usage}: {foregroundToken} ({foreground}) on {backgroundToken} ({background}) is {ratio:F2}:1, below the {NonTextMinimum}:1 AA minimum for non-text.");
    }

    [Fact]
    public void TheStandingMarks_AreDistinguishableFromOneAnother()
    {
        // The mark prints its word as well as its diamond, so colour is never
        // load-bearing. It should still be possible to tell the diamonds apart
        // without relying on hue — two swatches of the same lightness are one
        // swatch to a reader with a colour deficiency. Green means confirmed and
        // amber means waiting, and those two are the pair a member of staff
        // scans a register for. These are the tokens the ::before rules draw
        // with, not the accent the ornament uses.
        var confirmed = Token("--portal-green-700");
        var waiting = Token("--portal-amber-600");

        var ratio = ContrastRatio(confirmed, waiting);

        Assert.True(ratio >= 1.5,
            $"The confirmed ({confirmed}) and waiting ({waiting}) diamonds are only {ratio:F2}:1 apart.");
    }

    [Fact]
    public void EveryColourTheRulesDrawWith_IsANamedToken()
    {
        // A hex literal buried in a rule is a colour no contrast check can
        // reach: the test would keep passing against a value nobody uses. The
        // survivors are white, the muted rose on an outline-danger border, and
        // the neutral diamond — none of which is a palette decision.
        var body = PortalCss[PortalCss.IndexOf("--portal-control-radius", StringComparison.Ordinal)..];
        var literals = Regex.Matches(body, "#[0-9a-fA-F]{6}")
            .Select(match => match.Value.ToLowerInvariant())
            .Distinct()
            .Except(["#ffffff", "#cfa8a4", "#8b9794", "#0d6efd"])
            .ToList();

        Assert.True(literals.Count == 0,
            $"portal.css draws with un-named colours: {string.Join(", ", literals)}.");
    }

    [Fact]
    public void ThePaletteKeepsNoBronzeOrCoral()
    {
        // The portals moved to green and sky blue. A stray --portal-gold-* or
        // --portal-coral-* reference resolves to nothing and renders as the
        // browser's default, which is how a colour change half-lands.
        Assert.DoesNotContain("--portal-gold-", PortalCss);
        Assert.DoesNotContain("--portal-coral-", PortalCss);
    }

    private static string Token(string tokenName)
    {
        if (tokenName.StartsWith('#'))
        {
            return tokenName;
        }

        var match = Regex.Match(PortalCss, $@"{Regex.Escape(tokenName)}:\s*(#[0-9a-fA-F]{{6}});");
        if (!match.Success)
        {
            throw new InvalidOperationException($"Could not find `{tokenName}: #......;` in portal.css.");
        }

        return match.Groups[1].Value;
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
