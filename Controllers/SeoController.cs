using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Services;

namespace Okafor_.NET.Controllers;

public sealed class SeoController(
    ApplicationDbContext context,
    SeoUrlService urls,
    IConfiguration configuration) : Controller
{
    private static readonly string[] PublicPaths =
    [
        "/",
        "/about",
        "/services",
        "/doctors",
        "/news",
        "/patient-information",
        "/contact",
        "/privacy",
        "/AppointmentRequests/Create",
        "/Teleconsultations/Create",
        "/Donation"
    ];

    [HttpGet("/sitemap.xml")]
    [ResponseCache(Duration = 900, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Sitemap(CancellationToken cancellationToken)
    {
        var posts = await context.Posts
            .AsNoTracking()
            .Where(post => post.Published && post.Slug != string.Empty)
            .OrderByDescending(post => post.UpdatedAt ?? post.CreatedAt)
            .Select(post => new { post.Slug, post.CreatedAt, post.UpdatedAt })
            .ToListAsync(cancellationToken);
        var doctors = await context.Doctors
            .AsNoTracking()
            .Where(doctor => doctor.Slug != null && doctor.Slug != string.Empty)
            .OrderBy(doctor => doctor.Slug)
            .Select(doctor => doctor.Slug!)
            .ToListAsync(cancellationToken);

        var output = new StringBuilder();
        using (var writer = XmlWriter.Create(output, new XmlWriterSettings
        {
            OmitXmlDeclaration = true,
            Indent = true,
            Async = false
        }))
        {
            writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

            foreach (var path in PublicPaths)
            {
                WriteSitemapUrl(writer, urls.Absolute(path));
            }

            foreach (var doctorSlug in doctors)
            {
                WriteSitemapUrl(writer, urls.Absolute($"/doctors/{doctorSlug}"));
            }

            foreach (var post in posts)
            {
                WriteSitemapUrl(
                    writer,
                    urls.Absolute($"/news/{post.Slug}"),
                    post.UpdatedAt ?? post.CreatedAt);
            }

            writer.WriteEndElement();
        }

        return Content(output.ToString(), "application/xml", Encoding.UTF8);
    }

    [HttpGet("/feed.xml")]
    [ResponseCache(Duration = 900, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Feed(CancellationToken cancellationToken)
    {
        var posts = await context.Posts
            .AsNoTracking()
            .Where(post => post.Published && post.Slug != string.Empty)
            .OrderByDescending(post => post.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        var hospitalName = configuration["Hospital:Name"] ??
            "Boniface and Paulina Okafor Memorial Hospital";
        var feedTitle = configuration["Editorial:FeedTitle"] ??
            "Hospital Notes from Isuochi";
        var output = new StringBuilder();

        using (var writer = XmlWriter.Create(output, new XmlWriterSettings
        {
            OmitXmlDeclaration = true,
            Indent = true,
            Async = false
        }))
        {
            writer.WriteStartElement("rss");
            writer.WriteAttributeString("version", "2.0");
            writer.WriteStartElement("channel");
            writer.WriteElementString("title", feedTitle);
            writer.WriteElementString("link", urls.Absolute("/news"));
            writer.WriteElementString(
                "description",
                $"Short hospital updates and practical health guidance from {hospitalName}.");
            writer.WriteElementString("language", "en-NG");
            var lastBuildDate = posts.Count > 0
                ? PostPresentation.AsUtc(posts.Max(post => post.UpdatedAt ?? post.CreatedAt))
                : DateTime.UtcNow;
            writer.WriteElementString("lastBuildDate", new DateTimeOffset(lastBuildDate).ToString("R"));

            foreach (var post in posts)
            {
                var postUrl = urls.Absolute($"/news/{post.Slug}");
                writer.WriteStartElement("item");
                writer.WriteElementString("title", post.Title);
                writer.WriteElementString("link", postUrl);
                writer.WriteElementString("guid", postUrl);
                writer.WriteElementString(
                    "pubDate",
                    new DateTimeOffset(PostPresentation.AsUtc(post.CreatedAt)).ToString("R"));
                writer.WriteElementString("description", PostPresentation.Description(post));
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        return Content(output.ToString(), "application/rss+xml", Encoding.UTF8);
    }

    [HttpGet("/robots.txt")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public IActionResult Robots()
    {
        var content = $"""
            User-agent: *
            Allow: /
            Disallow: /Admin/
            Disallow: /Account/
            Disallow: /Patient/
            Disallow: /Portal/
            Disallow: /Identity/
            Disallow: /Donation/Receipt
            Disallow: /AppointmentRequests/Submitted
            Disallow: /Teleconsultations/Submitted
            Disallow: /Home/Search

            Sitemap: {urls.Absolute("/sitemap.xml")}
            """;

        return Content(content, "text/plain", Encoding.UTF8);
    }

    private static void WriteSitemapUrl(XmlWriter writer, string location, DateTime? lastModified = null)
    {
        writer.WriteStartElement("url");
        writer.WriteElementString("loc", location);
        if (lastModified.HasValue)
        {
            writer.WriteElementString(
                "lastmod",
                PostPresentation.AsUtc(lastModified.Value).ToString("yyyy-MM-ddTHH:mm:ssK"));
        }

        writer.WriteEndElement();
    }
}
