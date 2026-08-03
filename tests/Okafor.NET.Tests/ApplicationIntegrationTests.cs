using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Okafor_.NET.Tests;

public sealed class ApplicationIntegrationTests
{
    [Fact]
    public async Task HealthEndpoint_ReturnsOk()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task HomePage_ReturnsOk()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/");

        response.EnsureSuccessStatusCode();
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HomePage_KeepsCareTeamOnItsDedicatedPage()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("team-preview-title", html);
        Assert.DoesNotContain("Meet the full care team", html);
    }

    [Fact]
    public async Task HomePage_SecurityHeaders_HaveExpectedValues()
    {
        // The pre-existing smoke test (SmokeTests.ResponseHeaders_Include_Security_Basics) only
        // asserted a Date header and a 200 status -- it would not catch a CSP, X-Frame-Options, or
        // X-Content-Type-Options regression. This runs in the default fast test pass (no live
        // server required) and pins the actual header values.
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/");
        response.EnsureSuccessStatusCode();

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("SAMEORIGIN", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("strict-origin-when-cross-origin", response.Headers.GetValues("Referrer-Policy").Single());

        var csp = response.Headers.GetValues("Content-Security-Policy").Single();
        Assert.Contains("default-src 'self'", csp);
        Assert.Contains("frame-ancestors 'self'", csp);
        // Alpine.js and the SignalR client are vendored under wwwroot/lib and served same-origin,
        // so script-src should not need to allow any third-party CDN origin.
        Assert.DoesNotContain("cdn.jsdelivr.net", csp);
        Assert.DoesNotContain("cdn.tailwindcss.com", csp);
    }

    [Fact]
    public async Task DonationDemo_ShowsSupportedInternationalCurrencies()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Donation");
        var html = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Contains("Demo mode is active", html, StringComparison.Ordinal);
        Assert.Contains("Canadian dollar (CAD)", html, StringComparison.Ordinal);
        Assert.Contains("US dollar (USD)", html, StringComparison.Ordinal);
        Assert.Contains("Euro (EUR)", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TeleconsultationCreatePage_ReturnsOk()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Teleconsultations/Create");

        response.EnsureSuccessStatusCode();
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/Teleconsultations/Submitted")]
    [InlineData("/Teleconsultations/Submitted?reference=123")]
    [InlineData("/Teleconsultations/Submitted?id=1")]
    public async Task TeleconsultationSubmittedPage_RejectsMissingOrGuessedReference(string url)
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));

        using var client = factory.CreateClient();
        using var response = await client.GetAsync(url);

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
}
