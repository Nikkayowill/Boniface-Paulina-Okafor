using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Okafor_.NET.Models;

namespace Okafor_.NET.Tests;

/// <summary>
/// The hosted preview launches without SMTP by setting
/// Authentication:RequireConfirmedAccount to false. Registration must therefore
/// survive a failing confirmation email instead of discarding the new account.
/// </summary>
public sealed class RegistrationEmailResilienceTests
{
    private sealed class FailingEmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage) =>
            throw new InvalidOperationException("SMTP email delivery is not configured.");
    }

    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IEmailSender>();
                services.AddScoped<IEmailSender, FailingEmailSender>();
            });
        });

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"(?<token>[^\"]+)\"");
        Assert.True(match.Success, "The register page did not render an antiforgery token.");
        return match.Groups["token"].Value;
    }

    [Fact]
    public async Task Register_KeepsTheAccount_WhenConfirmationEmailFailsAndConfirmationIsNotRequired()
    {
        const string email = "resilient-patient@okaformemorial.test";
        const string password = "Str0ng!Passw0rd";

        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var getResponse = await client.GetAsync("/Identity/Account/Register");
        getResponse.EnsureSuccessStatusCode();
        var token = ExtractAntiforgeryToken(await getResponse.Content.ReadAsStringAsync());

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = email,
            ["Input.Password"] = password,
            ["Input.ConfirmPassword"] = password,
            ["__RequestVerificationToken"] = token
        });

        using var postResponse = await client.PostAsync("/Identity/Account/Register", content);

        // A failed confirmation email must not surface as a rejected registration.
        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var created = await userManager.FindByEmailAsync(email);

        Assert.NotNull(created);
        Assert.True(
            await userManager.IsInRoleAsync(created!, "Patient"),
            "A registered account should still receive the Patient role.");
    }
}
