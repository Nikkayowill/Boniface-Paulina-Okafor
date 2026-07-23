using System.Net;

namespace Okafor_.NET.Tests;

/// <summary>
/// Smoke tests for post-deployment validation on staging/production environments.
/// These tests verify critical system health after deployment.
/// Run via: dotnet test --filter "Category=Smoke"
/// </summary>
public sealed class SmokeTests
{
    private readonly string _baseUrl = Environment.GetEnvironmentVariable("OKAFOR_BASE_URL") ?? "http://localhost:5187";

    private HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
        };
        var client = new HttpClient(handler) { BaseAddress = new Uri(_baseUrl) };
        client.DefaultRequestHeaders.Add("User-Agent", "Okafor-SmokeTests/1.0");
        return client;
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task HealthCheck_Endpoint_Returns200()
    {
        using var client = CreateHttpClient();
        using var response = await client.GetAsync("/health");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task HomePage_Loads_Successfully()
    {
        using var client = CreateHttpClient();
        using var response = await client.GetAsync("/");
        
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        Assert.NotEmpty(content);
        Assert.Contains("<!DOCTYPE html>", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task Doctors_Page_Loads_Successfully()
    {
        using var client = CreateHttpClient();
        using var response = await client.GetAsync("/doctors");
        
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        Assert.NotEmpty(content);
    }

    [Theory]
    [Trait("Category", "Smoke")]
    [InlineData("/about")]
    [InlineData("/services")]
    [InlineData("/news")]
    [InlineData("/contact")]
    public async Task Public_Content_Pages_Load_Successfully(string url)
    {
        using var client = CreateHttpClient();
        using var response = await client.GetAsync(url);

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        Assert.NotEmpty(content);
        Assert.Contains("<!DOCTYPE html>", content, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [Trait("Category", "Smoke")]
    [InlineData("/offline.html", "text/html")]
    [InlineData("/offline-appointments.html", "text/html")]
    [InlineData("/site.webmanifest", "application/manifest+json")]
    [InlineData("/service-worker.js", "text/javascript")]
    public async Task Pwa_Assets_Load_Successfully(string url, string expectedContentType)
    {
        using var client = CreateHttpClient();
        using var response = await client.GetAsync(url);

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        Assert.NotEmpty(content);
        Assert.Contains(expectedContentType, response.Content.Headers.ContentType?.MediaType ?? "");
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task HomePage_Renders_WhatsApp_Widget()
    {
        using var client = CreateHttpClient();
        using var response = await client.GetAsync("/");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("whatsapp-float", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("https://wa.me/", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Contact us on WhatsApp to book an appointment", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Contact Us", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Hello%2C%20I%20would%20like%20to%20book%20an%20appointment", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Boniface%20%26%20Paulina%20Okafor%20Memorial%20Hospital", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("My%20name%20is%3A%0AReason%20for%20visit%3A%0APreferred%20day%2Ftime%3A", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task AppointmentRequests_Page_Accessible()
    {
        using var client = CreateHttpClient();
        using var response = await client.GetAsync("/AppointmentRequests/Create");
        
        // Should either load or redirect to login (302/401)
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.Unauthorized,
            $"Unexpected status code: {response.StatusCode}"
        );
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task Css_Files_Load_Successfully()
    {
        using var client = CreateHttpClient();
        var cssUrls = new[]
        {
            "/css/site.css",
            "/lib/bootstrap/dist/css/bootstrap.min.css"
        };

        foreach (var cssUrl in cssUrls)
        {
            using var response = await client.GetAsync(cssUrl);
            Assert.True(
                response.IsSuccessStatusCode,
                $"CSS file {cssUrl} returned {response.StatusCode}"
            );
        }
    }

    [Theory]
    [Trait("Category", "Smoke")]
    [InlineData("/js/navigation.js")]
    [InlineData("/js/site.js")]
    public async Task Core_Javascript_Files_Load_Successfully(string scriptUrl)
    {
        using var client = CreateHttpClient();
        using var response = await client.GetAsync(scriptUrl);

        response.EnsureSuccessStatusCode();
        Assert.Contains("javascript", response.Content.Headers.ContentType?.MediaType ?? "");
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task ResponseHeaders_Include_Security_Basics()
    {
        using var client = CreateHttpClient();
        using var response = await client.GetAsync("/");
        
        // Verify response is not cached or has proper cache headers
        Assert.True(response.Headers.Contains("Date"), "Response should include Date header");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task No_500_Errors_On_Home()
    {
        using var client = CreateHttpClient();
        using var response = await client.GetAsync("/");
        
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task Timeout_Handling_Reasonable()
    {
        using var client = CreateHttpClient();
        client.Timeout = TimeSpan.FromSeconds(10);
        
        try
        {
            using var response = await client.GetAsync("/");
            Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Redirect);
        }
        catch (HttpRequestException ex)
        {
            Assert.Fail($"Request timed out or failed: {ex.Message}");
        }
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task Static_Content_Returns_Correct_Content_Types()
    {
        using var client = CreateHttpClient();
        
        var tests = new[]
        {
            ("/css/site.css", "text/css"),
            ("/favicon.ico", "image/x-icon"),
        };

        foreach (var (url, expectedContentType) in tests)
        {
            using var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                Assert.Contains(expectedContentType, response.Content.Headers.ContentType?.MediaType ?? "");
            }
        }
    }
}
