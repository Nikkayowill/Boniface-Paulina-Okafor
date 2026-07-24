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
