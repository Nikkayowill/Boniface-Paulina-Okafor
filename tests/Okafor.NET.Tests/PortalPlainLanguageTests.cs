
namespace Okafor_.NET.Tests;

/// <summary>
/// The rules the portals were simplified to, kept honest.
///
/// Both portals are read by people in their eighties — patients checking a date
/// and a time, and the hospital office working through a queue. The rebuild
/// that made them legible (Atkinson Hyperlegible, 48px targets, AA contrast)
/// did nothing about the two things that actually stop an older reader: a
/// figure printed without saying what it counts, and a clock they have to do
/// arithmetic on. These are source-level checks, in the manner of
/// PortalContrastTests, because a rule that only holds on the pages someone
/// remembered to look at is not a rule.
/// </summary>
public class PortalPlainLanguageTests
{
    /// <summary>
    /// Every view in both portal areas.
    /// </summary>
    public static TheoryData<string> PortalViews()
    {
        var data = new TheoryData<string>();
        foreach (var path in FindPortalViews())
        {
            data.Add(path);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(PortalViews))]
    public void NoPortalView_PrintsA24HourClock(string relativePath)
    {
        var markup = File.ReadAllText(RepoPath(relativePath));

        // "14:30" is half past two to anyone who counts the hours past twelve
        // and a number to everyone else. The portals print "2:30 PM".
        Assert.False(
            markup.Contains("\"HH:mm", StringComparison.Ordinal) ||
            markup.Contains("'HH:mm", StringComparison.Ordinal) ||
            markup.Contains(" HH:mm\"", StringComparison.Ordinal),
            $"{relativePath} formats a time as HH:mm. Portal times are written as h:mm tt so an " +
            "afternoon appointment reads as 2:30 PM rather than 14:30.");
    }

    [Theory]
    [MemberData(nameof(PortalViews))]
    public void NoPortalView_LabelsAStatusFieldStanding(string relativePath)
    {
        var markup = File.ReadAllText(RepoPath(relativePath));

        Assert.DoesNotContain(">Standing<", markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// The index is the list of doors out of a dashboard. Each row used to be a
    /// name and a bare numeral — "Teleconsultations  3" — which says neither
    /// what the row is nor whether the 3 wants anything from the reader. Every
    /// row now carries a sentence saying what it is, and a chevron saying it is
    /// a door.
    /// </summary>
    [Theory]
    [InlineData("Areas/Patient/Views/Dashboard/Index.cshtml")]
    [InlineData("Areas/Admin/Views/Dashboard/Index.cshtml")]
    public void EveryIndexRow_SaysWhatItIsAndThatItOpens(string relativePath)
    {
        var markup = File.ReadAllText(RepoPath(relativePath));

        var rows = Count(markup, "portal-index__row");
        var says = Count(markup, "portal-index__say");
        var chevrons = Count(markup, "portal-index__go");

        Assert.True(rows > 0, $"{relativePath} renders no index at all.");
        Assert.True(says == rows,
            $"{relativePath} has {rows} index rows but {says} descriptions. Every row says what it is.");
        Assert.True(chevrons == rows,
            $"{relativePath} has {rows} index rows but {chevrons} chevrons. Every row is a link and says so.");
    }

    /// <summary>
    /// A screen asks for one thing. Two large buttons of equal weight is a
    /// decision, and the reader this portal is drawn for is the one least
    /// served by being handed one.
    /// </summary>
    [Theory]
    [InlineData("Areas/Patient/Views/Dashboard/Index.cshtml")]
    [InlineData("Areas/Admin/Views/Dashboard/Index.cshtml")]
    public void ADashboard_NeverOffersMoreThanOneLeadActionPerBlock(string relativePath)
    {
        var markup = File.ReadAllText(RepoPath(relativePath));

        // The title row carries one, and each mutually exclusive state of the
        // opening block carries at most one more. On the patient dashboard the
        // three states are: no profile, a next visit, nothing booked.
        var leads = Count(markup, "btn-lead");

        Assert.True(leads > 0, $"{relativePath} names no lead action at all.");
        Assert.True(leads <= 4,
            $"{relativePath} carries {leads} lead actions. Only one can be reachable at a time.");
    }

    private static int Count(string haystack, string needle)
    {
        var total = 0;
        var index = haystack.IndexOf(needle, StringComparison.Ordinal);
        while (index >= 0)
        {
            total++;
            index = haystack.IndexOf(needle, index + needle.Length, StringComparison.Ordinal);
        }

        return total;
    }

    private static IEnumerable<string> FindPortalViews()
    {
        var root = RepoRoot();

        foreach (var area in new[] { "Areas/Patient/Views", "Areas/Admin/Views" })
        {
            var directory = Path.Combine(root, area);
            foreach (var file in Directory.EnumerateFiles(directory, "*.cshtml", SearchOption.AllDirectories))
            {
                yield return Path.GetRelativePath(root, file).Replace('\\', '/');
            }
        }
    }

    private static string RepoPath(string relativePath) => Path.Combine(RepoRoot(), relativePath);

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "Areas", "Patient", "Views")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException($"Could not find the repository root from {AppContext.BaseDirectory}.");
    }
}
