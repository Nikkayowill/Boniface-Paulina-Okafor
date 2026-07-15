using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Okafor_.NET.Tests;

public sealed class AuthorizationBoundaryIntegrationTests
{
    private const string TestAuthenticationScheme = "AuthorizationBoundaryTest";

    [Theory]
    [InlineData("/Portal/Dashboard")]
    [InlineData("/Admin/Dashboard")]
    [InlineData("/Admin/AppointmentRequests")]
    public async Task AnonymousUser_IsRedirectedToLogin(string url)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var loginPath = response.Headers.Location.IsAbsoluteUri
            ? response.Headers.Location.AbsolutePath
            : response.Headers.Location.OriginalString.Split('?', 2)[0];
        Assert.Equal("/Identity/Account/Login", loginPath);
    }

    [Theory]
    [InlineData("Patient", "/Portal/Dashboard")]
    [InlineData("Admin", "/Admin/Dashboard")]
    [InlineData("Admin", "/Admin/AppointmentRequests")]
    [InlineData("Staff", "/Admin/AppointmentRequests")]
    [InlineData("Staff", "/Admin/Teleconsultations")]
    [InlineData("Staff", "/Admin/BillPayments")]
    [InlineData("Staff", "/Admin/Donations")]
    public async Task AuthorizedRole_CanOpenAssignedRoute(string role, string url)
    {
        using var factory = CreateFactory(useTestAuthentication: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add(TestRoleAuthenticationHandler.RoleHeader, role);

        using var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("Patient", "/Admin/Dashboard")]
    [InlineData("Patient", "/Admin/AppointmentRequests")]
    [InlineData("Staff", "/Admin/Dashboard")]
    [InlineData("Staff", "/Admin/Integrations")]
    [InlineData("Staff", "/Portal/Dashboard")]
    [InlineData("Admin", "/Portal/Dashboard")]
    public async Task AuthenticatedRole_IsForbiddenOutsideAssignedBoundary(string role, string url)
    {
        using var factory = CreateFactory(useTestAuthentication: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add(TestRoleAuthenticationHandler.RoleHeader, role);

        using var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory(bool useTestAuthentication = false)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                if (!useTestAuthentication)
                {
                    return;
                }

                builder.ConfigureServices(services =>
                {
                    services.AddAuthentication(options =>
                        {
                            options.DefaultAuthenticateScheme = TestAuthenticationScheme;
                            options.DefaultChallengeScheme = TestAuthenticationScheme;
                            options.DefaultForbidScheme = TestAuthenticationScheme;
                        })
                        .AddScheme<AuthenticationSchemeOptions, TestRoleAuthenticationHandler>(
                            TestAuthenticationScheme,
                            _ => { });
                });
            });
    }

    private sealed class TestRoleAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string RoleHeader = "X-Test-Role";

        public TestRoleAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(RoleHeader, out var role) || string.IsNullOrWhiteSpace(role))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "authorization-test-user"),
                new Claim(ClaimTypes.Name, "authorization-test@example.invalid"),
                new Claim(ClaimTypes.Role, role.ToString())
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, TestAuthenticationScheme));
            var ticket = new AuthenticationTicket(principal, TestAuthenticationScheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
