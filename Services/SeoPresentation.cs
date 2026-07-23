using System.Net;
using System.Text.RegularExpressions;
using Okafor_.NET.Models;

namespace Okafor_.NET.Services;

public sealed class SeoUrlService
{
    private readonly Uri _canonicalBaseUri;

    public SeoUrlService(IConfiguration configuration)
    {
        var configuredBaseUrl = configuration["Seo:CanonicalBaseUrl"];
        if (!Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var baseUri) ||
            !string.Equals(baseUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrEmpty(baseUri.UserInfo) ||
            !string.IsNullOrEmpty(baseUri.Query) ||
            !string.IsNullOrEmpty(baseUri.Fragment))
        {
            throw new InvalidOperationException(
                "Seo:CanonicalBaseUrl must be an absolute HTTPS URL without credentials, query, or fragment.");
        }

        _canonicalBaseUri = new Uri($"{baseUri.GetLeftPart(UriPartial.Authority).TrimEnd('/')}/");
    }

    public string BaseUrl => _canonicalBaseUri.GetLeftPart(UriPartial.Authority);

    public string Absolute(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return $"{BaseUrl}/";
        }

        if (Uri.TryCreate(path, UriKind.Absolute, out var absoluteUri))
        {
            return string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                ? absoluteUri.AbsoluteUri
                : $"{BaseUrl}/";
        }

        return new Uri(_canonicalBaseUri, path.TrimStart('/')).AbsoluteUri;
    }
}

public enum PostContentBlockType
{
    Paragraph,
    Heading,
    UnorderedList,
    OrderedList
}

public sealed record PostContentBlock(
    PostContentBlockType Type,
    IReadOnlyList<string> Lines);

public static partial class PostPresentation
{
    private const int WordsPerMinute = 220;
    private const int ShortPostWordLimit = 300;

    [GeneratedRegex("<\\s*br\\s*/?\\s*>|</\\s*(p|div|blockquote)\\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex BlockEndingTagPattern();

    [GeneratedRegex("<\\s*h[1-6](?:\\s[^>]*)?>", RegexOptions.IgnoreCase)]
    private static partial Regex HeadingStartTagPattern();

    [GeneratedRegex("</\\s*h[1-6]\\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex HeadingEndTagPattern();

    [GeneratedRegex("<\\s*li(?:\\s[^>]*)?>", RegexOptions.IgnoreCase)]
    private static partial Regex ListItemTagPattern();

    [GeneratedRegex("<\\s*(script|style)(?:\\s[^>]*)?>.*?</\\s*\\1\\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex UnsafeBlockPattern();

    [GeneratedRegex("<[^>]+>", RegexOptions.Singleline)]
    private static partial Regex HtmlTagPattern();

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespacePattern();

    [GeneratedRegex("^\\d+[.)]\\s+")]
    private static partial Regex OrderedListPrefixPattern();

    public static string PlainText(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var withLineBreaks = UnsafeBlockPattern().Replace(content, string.Empty);
        withLineBreaks = HeadingStartTagPattern().Replace(withLineBreaks, "\n");
        withLineBreaks = HeadingEndTagPattern().Replace(withLineBreaks, ":\n");
        withLineBreaks = BlockEndingTagPattern().Replace(withLineBreaks, "\n");
        withLineBreaks = ListItemTagPattern().Replace(withLineBreaks, "\n- ");
        var withoutTags = HtmlTagPattern().Replace(withLineBreaks, " ");
        return WebUtility.HtmlDecode(withoutTags).Trim();
    }

    public static string Description(Post post, int maximumLength = 180)
    {
        var source = string.IsNullOrWhiteSpace(post.Summary)
            ? PlainText(post.Content)
            : PlainText(post.Summary);
        var normalized = WhitespacePattern().Replace(source, " ").Trim();

        if (normalized.Length <= maximumLength)
        {
            return normalized;
        }

        var breakAt = normalized.LastIndexOf(' ', maximumLength);
        return $"{normalized[..(breakAt > 0 ? breakAt : maximumLength)].TrimEnd(' ', '.', ',', ';', ':')}…";
    }

    public static int ReadingMinutes(Post post)
    {
        var wordCount = WhitespacePattern()
            .Split(PlainText(post.Content))
            .Count(word => !string.IsNullOrWhiteSpace(word));
        return Math.Max(1, (int)Math.Ceiling(wordCount / (double)WordsPerMinute));
    }

    public static string FormatLabel(Post post)
    {
        var wordCount = WhitespacePattern()
            .Split(PlainText(post.Content))
            .Count(word => !string.IsNullOrWhiteSpace(word));
        return wordCount <= ShortPostWordLimit ? "Hospital note" : "Health guide";
    }

    public static DateTime AsUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();
    }

    public static IReadOnlyList<PostContentBlock> ContentBlocks(string? content)
    {
        var plainText = PlainText(content).Replace("\r\n", "\n", StringComparison.Ordinal);
        if (string.IsNullOrWhiteSpace(plainText))
        {
            return [];
        }

        var blocks = new List<PostContentBlock>();
        var paragraph = new List<string>();
        var listItems = new List<string>();
        PostContentBlockType? listType = null;

        void FlushParagraph()
        {
            if (paragraph.Count == 0)
            {
                return;
            }

            blocks.Add(new PostContentBlock(
                PostContentBlockType.Paragraph,
                [string.Join(" ", paragraph)]));
            paragraph.Clear();
        }

        void FlushList()
        {
            if (listItems.Count == 0 || listType is null)
            {
                return;
            }

            blocks.Add(new PostContentBlock(listType.Value, listItems.ToArray()));
            listItems.Clear();
            listType = null;
        }

        foreach (var rawLine in plainText.Split('\n'))
        {
            var line = WhitespacePattern().Replace(rawLine, " ").Trim();
            if (string.IsNullOrEmpty(line))
            {
                FlushParagraph();
                FlushList();
                continue;
            }

            var isUnorderedItem = line.StartsWith("- ", StringComparison.Ordinal) ||
                line.StartsWith("• ", StringComparison.Ordinal);
            var isOrderedItem = OrderedListPrefixPattern().IsMatch(line);
            if (isUnorderedItem || isOrderedItem)
            {
                FlushParagraph();
                var nextListType = isOrderedItem
                    ? PostContentBlockType.OrderedList
                    : PostContentBlockType.UnorderedList;
                if (listType is not null && listType != nextListType)
                {
                    FlushList();
                }

                listType = nextListType;
                listItems.Add(isOrderedItem
                    ? OrderedListPrefixPattern().Replace(line, string.Empty)
                    : line[2..].Trim());
                continue;
            }

            FlushList();
            if (line.EndsWith(':') && line.Length <= 100)
            {
                FlushParagraph();
                blocks.Add(new PostContentBlock(
                    PostContentBlockType.Heading,
                    [line.TrimEnd(':')]));
                continue;
            }

            paragraph.Add(line);
        }

        FlushParagraph();
        FlushList();
        return blocks;
    }
}
