using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Okafor_.NET.Controllers;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.Services;

namespace Okafor_.NET.Tests;

public sealed class SeoControllerTests
{
    [Fact]
    public async Task Sitemap_ContainsPublicDatabaseUrlsAndExcludesDraftsAndDisabledBilling()
    {
        await using var context = CreateContext();
        context.Departments.Add(new Department { Id = 1, Name = "General Medicine" });
        context.Doctors.Add(new Doctor
        {
            FullName = "Dr Ada Example",
            Slug = "dr-ada-example",
            Specialty = "General Medicine",
            DepartmentId = 1
        });
        context.Posts.AddRange(
            new Post
            {
                Title = "Published note",
                Slug = "published-note",
                Summary = "A published note.",
                Content = "Confirmed information for patients.",
                Published = true,
                CreatedAt = new DateTime(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc)
            },
            new Post
            {
                Title = "Draft note",
                Slug = "draft-note",
                Content = "Not ready.",
                Published = false
            });
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = Assert.IsType<ContentResult>(await controller.Sitemap(CancellationToken.None));
        var document = XDocument.Parse(result.Content!);
        var locations = document.Descendants()
            .Where(element => element.Name.LocalName == "loc")
            .Select(element => element.Value)
            .ToArray();

        Assert.Contains("https://www.okaformemorial.org/news/published-note", locations);
        Assert.Contains("https://www.okaformemorial.org/doctors/dr-ada-example", locations);
        Assert.DoesNotContain(locations, location => location.Contains("draft-note"));
        Assert.DoesNotContain(locations, location => location.Contains("BillPayments"));
        Assert.All(locations, location => Assert.StartsWith("https://www.okaformemorial.org/", location));
    }

    [Fact]
    public async Task Feed_ContainsPublishedPostsOnly()
    {
        await using var context = CreateContext();
        context.Posts.AddRange(
            new Post
            {
                Title = "Public clinic note",
                Slug = "public-clinic-note",
                Summary = "Confirmed information for local families.",
                Content = "Confirmed information for local families.",
                Published = true
            },
            new Post
            {
                Title = "Private draft",
                Slug = "private-draft",
                Content = "Not published.",
                Published = false
            });
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = Assert.IsType<ContentResult>(await controller.Feed(CancellationToken.None));
        var document = XDocument.Parse(result.Content!);

        Assert.Contains("Public clinic note", document.ToString());
        Assert.DoesNotContain("Private draft", document.ToString());
        Assert.Equal("application/rss+xml", result.ContentType);
    }

    [Fact]
    public void Robots_UsesTheCanonicalSitemapAndBlocksPrivateWorkflows()
    {
        using var context = CreateContext();
        var controller = CreateController(context);

        var result = Assert.IsType<ContentResult>(controller.Robots());

        Assert.Contains(
            "Sitemap: https://www.okaformemorial.org/sitemap.xml",
            result.Content);
        Assert.Contains("Disallow: /Admin/", result.Content);
        Assert.Contains("Disallow: /Patient/", result.Content);
        Assert.Contains("Disallow: /Donation/Receipt", result.Content);
    }

    private static SeoController CreateController(ApplicationDbContext context)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Seo:CanonicalBaseUrl"] = "https://www.okaformemorial.org",
                ["Hospital:Name"] = "Boniface and Paulina Okafor Memorial Hospital",
                ["Editorial:FeedTitle"] = "Hospital Notes from Isuochi"
            })
            .Build();

        return new SeoController(context, new SeoUrlService(configuration), configuration);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new ApplicationDbContext(options);
    }
}
