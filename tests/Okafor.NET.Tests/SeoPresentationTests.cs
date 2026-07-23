using Microsoft.Extensions.Configuration;
using Okafor_.NET.Models;
using Okafor_.NET.Services;

namespace Okafor_.NET.Tests;

public sealed class SeoPresentationTests
{
    [Fact]
    public void SeoUrlService_BuildsCanonicalUrlsFromOneTrustedOrigin()
    {
        var urls = CreateUrlService("https://www.okaformemorial.org/");

        Assert.Equal("https://www.okaformemorial.org", urls.BaseUrl);
        Assert.Equal(
            "https://www.okaformemorial.org/news/community-note",
            urls.Absolute("/news/community-note"));
    }

    [Theory]
    [InlineData("http://www.okaformemorial.org")]
    [InlineData("https://user:secret@www.okaformemorial.org")]
    [InlineData("https://www.okaformemorial.org?source=test")]
    public void SeoUrlService_RejectsUnsafeCanonicalOrigins(string value)
    {
        Assert.Throws<InvalidOperationException>(() => CreateUrlService(value));
    }

    [Fact]
    public void Description_UsesPlainTextAndTruncatesAtAWordBoundary()
    {
        var post = new Post
        {
            Summary = "<p>Clear guidance for families choosing the right next step at the hospital.</p>",
            Content = "Unused"
        };

        var result = PostPresentation.Description(post, 42);

        Assert.Equal("Clear guidance for families choosing the…", result);
        Assert.DoesNotContain("<p>", result);
    }

    [Fact]
    public void ContentBlocks_ConvertLegacyHtmlAndPlainTextWithoutReturningMarkup()
    {
        const string content = """
            <script>alert('unsafe')</script>
            <h2>Before your visit</h2>
            <p>Bring your current medicines.</p>
            <ul><li>Your appointment reference</li><li>A contact number</li></ul>
            """;

        var blocks = PostPresentation.ContentBlocks(content);

        Assert.Collection(
            blocks,
            block =>
            {
                Assert.Equal(PostContentBlockType.Heading, block.Type);
                Assert.Equal("Before your visit", block.Lines.Single());
            },
            block =>
            {
                Assert.Equal(PostContentBlockType.Paragraph, block.Type);
                Assert.Equal("Bring your current medicines.", block.Lines.Single());
            },
            block =>
            {
                Assert.Equal(PostContentBlockType.UnorderedList, block.Type);
                Assert.Equal(
                    ["Your appointment reference", "A contact number"],
                    block.Lines);
            });
        Assert.DoesNotContain(blocks, block => block.Lines.Any(line => line.Contains("unsafe")));
    }

    [Fact]
    public void ShortUsefulContent_IsLabelledAsAHospitalNote()
    {
        var post = new Post
        {
            Content = "Clinic hours have changed for Friday. Please call reception before travelling."
        };

        Assert.Equal("Hospital note", PostPresentation.FormatLabel(post));
        Assert.Equal(1, PostPresentation.ReadingMinutes(post));
    }

    private static SeoUrlService CreateUrlService(string baseUrl)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Seo:CanonicalBaseUrl"] = baseUrl
            })
            .Build();

        return new SeoUrlService(configuration);
    }
}
