using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Okafor_.NET.Data;
using Okafor_.NET.Models;

namespace Okafor_.NET.Tests;

/// <summary>
/// Proves every patient portal page renders with real records in it.
///
/// The portal was rebuilt onto one component set — the register, the visit
/// plate, the index and the standing mark — and two of these views previously
/// carried their list twice, once as mobile cards and once as a desktop table.
/// A page that throws in Razor, or silently loses the component it is built
/// from, is the failure this guards against. Status assertions matter doubly:
/// booking-realtime.js keys off the same bg-* classes the mark is styled from,
/// so a status that stops rendering them stops updating live.
/// </summary>
public sealed class PatientPortalRenderTests
{
    private const string TestAuthenticationScheme = "PatientRenderTest";
    private const string PatientUserId = "patient-render-test-user";
    private const string PatientEmail = "adaeze.render@example.invalid";

    public static TheoryData<string> PatientPages() => new()
    {
        "/Portal/Dashboard",
        "/Portal/Appointments",
        "/Portal/Teleconsultations",
        "/Portal/Documents",
        "/Portal/Messages",
        "/Portal/Profile",
        "/Portal/Profile/Edit",
        "/Portal/Messages/Send",
        "/Portal/Documents/Upload"
    };

    [Theory]
    [MemberData(nameof(PatientPages))]
    public async Task EveryPatientPage_RendersForASignedInPatient(string url)
    {
        using var factory = CreateSeededFactory();
        using var client = CreateClient(factory);

        using var response = await client.GetAsync(url);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // The layout's own chrome, so a page that rendered an error view or an
        // empty body cannot pass.
        Assert.Contains("portal-crown", html);
        Assert.Contains("portal-page-title", html);
    }

    [Fact]
    public async Task Dashboard_LeadsWithTheNextVisit_NotACountTile()
    {
        using var factory = CreateSeededFactory();
        using var client = CreateClient(factory);

        var html = await client.GetStringAsync("/Portal/Dashboard");

        Assert.Contains("portal-plate", html);
        Assert.Contains("Your next visit", html);
        Assert.Contains("General Medicine", html);
        Assert.Contains("Dr. Render Test", html);
        // The counts are one ruled index, not a grid of KPI cards.
        Assert.Contains("portal-index__row", html);
        Assert.DoesNotContain("dashboard-kpi", html);
    }

    [Fact]
    public async Task Appointments_RenderOneRegister_NotACardStackAndATable()
    {
        using var factory = CreateSeededFactory();
        using var client = CreateClient(factory);

        var html = await client.GetStringAsync("/Portal/Appointments");

        Assert.Contains("portal-register__row", html);
        Assert.Contains("General Medicine", html);

        // The paired mobile-card / desktop-table rendering is gone: there is no
        // second copy of the list hidden behind a display utility.
        Assert.DoesNotContain("d-md-none", html);
        Assert.DoesNotContain("<table", html);
    }

    [Fact]
    public async Task Appointments_KeepTheHooksTheOfflineAndRealtimeScriptsRead()
    {
        using var factory = CreateSeededFactory();
        using var client = CreateClient(factory);

        var html = await client.GetStringAsync("/Portal/Appointments");

        // pwa-appointments.js reads every one of these off the row.
        Assert.Contains("data-appointment-offline-item", html);
        Assert.Contains("data-appointment-id=", html);
        Assert.Contains("data-subject=", html);
        Assert.Contains("data-date=", html);
        Assert.Contains("data-department=", html);
        Assert.Contains("data-doctor=", html);
        Assert.Contains("data-status=", html);
        Assert.Contains("data-type=", html);
        Assert.Contains("data-save-offline-appointments", html);
        Assert.Contains("data-appointment-pwa-status", html);

        // booking-realtime.js finds the status by this attribute and rewrites
        // its textContent, so the mark must stay a text-only element.
        Assert.Contains("data-booking-status=\"appointment:", html);
    }

    [Theory]
    [InlineData("/Portal/Appointments")]
    [InlineData("/Portal/Teleconsultations")]
    public async Task StatusMarks_CarryBothAWordAndTheClassTheRealtimeScriptWrites(string url)
    {
        using var factory = CreateSeededFactory();
        using var client = CreateClient(factory);

        var html = await client.GetStringAsync(url);

        Assert.Contains("portal-mark", html);
        // Colour never carries the state on its own — the word is in the markup.
        Assert.Contains("Confirmed", html);
        // ...and the class booking-realtime.js swaps is the one portal.css styles.
        Assert.Contains("bg-success", html);
    }

    [Fact]
    public async Task Teleconsultations_RenderOneRegister_NotACardStackAndATable()
    {
        using var factory = CreateSeededFactory();
        using var client = CreateClient(factory);

        var html = await client.GetStringAsync("/Portal/Teleconsultations");

        Assert.Contains("portal-register__row", html);
        Assert.DoesNotContain("d-md-none", html);
        Assert.DoesNotContain("<table", html);
    }


    private static HttpClient CreateClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add(PatientAuthenticationHandler.UserHeader, PatientUserId);
        return client;
    }

    private static WebApplicationFactory<Program> CreateSeededFactory()
    {
        // A store of its own, so records seeded here cannot bleed into the
        // shared "OkaforHospitalTests" database the other suites use.
        var databaseName = $"PatientPortalRender-{Guid.NewGuid():N}";

        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor is not null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseInMemoryDatabase(databaseName)
                            .ConfigureWarnings(warnings =>
                                warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

                    services.AddAuthentication(options =>
                        {
                            options.DefaultAuthenticateScheme = TestAuthenticationScheme;
                            options.DefaultChallengeScheme = TestAuthenticationScheme;
                            options.DefaultForbidScheme = TestAuthenticationScheme;
                        })
                        .AddScheme<AuthenticationSchemeOptions, PatientAuthenticationHandler>(
                            TestAuthenticationScheme,
                            _ => { });
                });
            });

        Seed(factory);
        return factory;
    }

    private static void Seed(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();

        var user = new ApplicationUser
        {
            Id = PatientUserId,
            UserName = PatientEmail,
            NormalizedUserName = PatientEmail.ToUpperInvariant(),
            Email = PatientEmail,
            NormalizedEmail = PatientEmail.ToUpperInvariant(),
            EmailConfirmed = true
        };
        context.Users.Add(user);

        var department = new Department { Name = "General Medicine", Description = "Everyday care." };
        context.Departments.Add(department);

        var doctor = new Doctor
        {
            FullName = "Dr. Render Test",
            Slug = "dr-render-test",
            Specialty = "General Practice",
            Department = department
        };
        context.Doctors.Add(doctor);

        var profile = new PatientProfile
        {
            ApplicationUserId = PatientUserId,
            FullName = "Adaeze Render",
            Phone = "0803 000 0000",
            Address = "Umuchu"
        };
        context.PatientProfiles.Add(profile);
        context.SaveChanges();

        context.PatientAppointments.Add(new PatientAppointment
        {
            PatientProfileId = profile.Id,
            DepartmentId = department.Id,
            DoctorId = doctor.Id,
            AppointmentDate = DateTime.Now.AddDays(4).Date.AddHours(10).AddMinutes(30),
            Status = PatientAppointmentStatus.Confirmed,
            Notes = "Bring the referral letter."
        });

        context.AppointmentRequests.Add(new AppointmentRequest
        {
            PatientName = "Adaeze Render",
            Email = PatientEmail,
            Phone = "0803 000 0000",
            DepartmentId = department.Id,
            DoctorId = doctor.Id,
            PreferredDate = DateTime.Now.AddDays(12).Date,
            PreferredTime = "09:00",
            Status = AppointmentStatus.Pending,
            Message = "Any morning would suit."
        });

        context.TeleconsultationRequests.Add(new TeleconsultationRequest
        {
            PatientName = "Adaeze Render",
            Email = PatientEmail,
            Phone = "0803 000 0000",
            DepartmentId = department.Id,
            DoctorId = doctor.Id,
            ConsultationType = TeleconsultationType.Video,
            PreferredDate = DateTime.Now.AddDays(6).Date,
            PreferredTime = "14:00",
            Reason = "Follow-up on test results.",
            ConsentAccepted = true,
            Status = TeleconsultationStatus.Confirmed,
            ApplicationUserId = PatientUserId,
            PatientProfileId = profile.Id
        });

        context.PatientMessages.Add(new PatientMessage
        {
            PatientProfileId = profile.Id,
            Subject = "Moving my appointment",
            Body = "Could the visit be moved to the afternoon?",
            IsRead = false
        });

        context.PatientDocuments.Add(new PatientDocument
        {
            PatientProfileId = profile.Id,
            Title = "blood-results.pdf",
            Description = "Filed by the laboratory.",
            FileUrl = "/secure/blood-results.pdf"
        });

        context.SaveChanges();
    }

    private sealed class PatientAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string UserHeader = "X-Test-Patient";

        public PatientAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(UserHeader, out var userId) || string.IsNullOrWhiteSpace(userId))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, PatientEmail),
                new Claim(ClaimTypes.Role, "Patient")
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, TestAuthenticationScheme));
            var ticket = new AuthenticationTicket(principal, TestAuthenticationScheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
