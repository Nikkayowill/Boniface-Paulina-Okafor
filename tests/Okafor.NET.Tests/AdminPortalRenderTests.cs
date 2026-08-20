using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
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
/// Proves every admin portal page renders with real records in it.
///
/// The admin portal was rebuilt onto the same component set as the patient
/// portal — the register, the docket, the index, the plate and the standing
/// mark — and most of its lists previously carried their records twice, once as
/// a stack of mobile cards and once as a desktop table. A page that throws in
/// Razor, or silently loses the component it is built from, is the failure this
/// guards against. Status assertions matter doubly: booking-realtime.js keys off
/// the same bg-* classes the mark is styled from, so a status that stops
/// rendering them stops updating live.
/// </summary>
public sealed class AdminPortalRenderTests
{
    private const string TestAuthenticationScheme = "AdminRenderTest";
    private const string AdminUserId = "admin-render-test-user";
    private const string AdminEmail = "chidi.render@example.invalid";
    private const string StaffUserId = "staff-render-test-user";
    private const string StaffEmail = "ngozi.render@example.invalid";

    [Fact]
    public async Task EveryAdminPage_RendersForASignedInAdministrator()
    {
        using var factory = CreateSeededFactory(out var seeded);
        using var client = CreateClient(factory, AdminUserId);

        foreach (var url in AdminPages(seeded))
        {
            using var response = await client.GetAsync(url);
            var html = await response.Content.ReadAsStringAsync();

            Assert.True(
                response.StatusCode == HttpStatusCode.OK,
                $"{url} returned {(int)response.StatusCode} {response.StatusCode}.");

            // The layout's own chrome, so a page that rendered an error view or
            // an empty body cannot pass.
            Assert.True(html.Contains("portal-crown"), $"{url} did not render the crown.");
            Assert.True(html.Contains("portal-page-title"), $"{url} did not render a page title.");
        }
    }

    [Fact]
    public async Task NoAdminPage_CarriesItsListTwice()
    {
        using var factory = CreateSeededFactory(out var seeded);
        using var client = CreateClient(factory, AdminUserId);

        foreach (var url in AdminPages(seeded))
        {
            using var response = await client.GetAsync(url);
            var html = await response.Content.ReadAsStringAsync();

            // The paired mobile-card / desktop-table rendering is gone from the
            // whole portal: no second copy of a list hidden behind a display
            // utility, and no table to be that second copy.
            Assert.True(!html.Contains("d-md-none"), $"{url} still hides a duplicate list at a breakpoint.");
            Assert.True(!html.Contains("<table"), $"{url} still renders a table.");
        }
    }

    [Fact]
    public async Task Dashboard_LeadsWithTheOldestOutstandingRequest_NotACountTile()
    {
        using var factory = CreateSeededFactory(out _);
        using var client = CreateClient(factory, AdminUserId);

        var html = await client.GetStringAsync("/Admin");

        Assert.Contains("portal-plate", html);
        Assert.Contains("Waiting longest", html);
        // The seeded appointment request is the oldest thing outstanding, so it
        // is the one named on the plate.
        Assert.Contains("Adaeze Render", html);
        Assert.Contains("Appointment request", html);

        // The counts are one ruled index, not a grid of KPI cards.
        Assert.Contains("portal-index__row", html);
        Assert.DoesNotContain("dashboard-kpi", html);
    }

    [Theory]
    [InlineData("/Admin/AppointmentRequests", "appointment")]
    [InlineData("/Admin/Teleconsultations", "teleconsultation")]
    public async Task BookingQueues_KeepTheHooksTheRealtimeScriptReads(string url, string type)
    {
        using var factory = CreateSeededFactory(out _);
        using var client = CreateClient(factory, AdminUserId);

        var html = await client.GetStringAsync(url);

        // booking-realtime.js removes a cancelled record by this attribute,
        // rewrites the status by that one, and adjusts the pending figure by the
        // third. All three have to survive on the register row.
        Assert.Contains($"data-booking-row=\"{type}:", html);
        Assert.Contains($"data-booking-status=\"{type}:", html);
        Assert.Contains($"data-booking-pending-count=\"{type}\"", html);
        Assert.Contains("data-booking-realtime", html);

        // The record list is one register at every width.
        Assert.Contains("portal-register__row", html);
    }

    [Theory]
    [InlineData("/Admin/AppointmentRequests", "Pending")]
    [InlineData("/Admin/Teleconsultations", "Pending")]
    [InlineData("/Admin/PatientMessages", "Awaiting review")]
    public async Task StatusMarks_CarryBothAWordAndTheClassTheRealtimeScriptWrites(string url, string standing)
    {
        using var factory = CreateSeededFactory(out _);
        using var client = CreateClient(factory, AdminUserId);

        var html = await client.GetStringAsync(url);

        Assert.Contains("portal-mark", html);
        // Colour never carries the state on its own — the word is in the markup.
        Assert.Contains(standing, html);
        // ...and the class booking-realtime.js swaps is the one portal.css styles.
        Assert.Contains("bg-warning", html);
    }

    [Fact]
    public async Task Availability_IsBuiltFromPortalComponents_NotDeadTailwindClasses()
    {
        using var factory = CreateSeededFactory(out _);
        using var client = CreateClient(factory, AdminUserId);

        var html = await client.GetStringAsync("/Admin/Availability");

        Assert.Contains("portal-shift", html);
        Assert.Contains("portal-legend", html);

        // The admin layout loads Bootstrap and portal.css only. Tailwind utility
        // classes here render as nothing at all, which is how this page came to
        // be the one unstyled screen in the portal.
        Assert.DoesNotContain("border-secondary-300", html);
        Assert.DoesNotContain("tracking-[", html);
        Assert.DoesNotContain("text-[13px]", html);
    }

    [Fact]
    public async Task TheRail_SendsDoctorsAndDepartmentsToTheAdminLists_NotThePublicPages()
    {
        using var factory = CreateSeededFactory(out _);
        using var client = CreateClient(factory, AdminUserId);

        var html = await client.GetStringAsync("/Admin");

        // A bare /doctors is the public care-team page: the "doctors_index"
        // route claims it for Home/Team. The admin lists answer under /Admin,
        // and the rail has to point there.
        Assert.Contains("href=\"/Admin/Doctors\"", html);
        Assert.Contains("href=\"/Admin/Departments\"", html);
    }

    [Fact]
    public async Task TheRail_OffersStaffOnlyTheSectionsTheirRoleCanOpen()
    {
        using var factory = CreateSeededFactory(out _);
        using var client = CreateClient(factory, StaffUserId);

        var html = await client.GetStringAsync("/Admin/AppointmentRequests");

        // Staff are admitted to the booking, teleconsultation and payment queues.
        Assert.Contains("Appointment requests", html);
        Assert.Contains("Teleconsultations", html);

        // Everything else in the area is [Authorize(Roles = "Admin")]. Listing
        // it would send a member of staff to a page that refuses them.
        Assert.DoesNotContain("Patient files", html);
        Assert.DoesNotContain("Staff accounts", html);
        Assert.DoesNotContain("Consulting hours", html);
        Assert.DoesNotContain("Integration readiness", html);
    }

    [Fact]
    public async Task TheAdminPortal_NoLongerCarriesADonationsQueue()
    {
        using var factory = CreateSeededFactory(out _);
        using var client = CreateClient(factory, AdminUserId);

        // Donations are collected by CanadaHelps, off this site — every public
        // CTA points at ProgramInfo.DonateUrl — so there is nothing for a
        // member of staff to review here.
        var dashboard = await client.GetStringAsync("/Admin");
        Assert.DoesNotContain("Donations", dashboard);

        using var queue = await client.GetAsync("/Admin/Donations");
        Assert.Equal(HttpStatusCode.NotFound, queue.StatusCode);
    }

    [Fact]
    public async Task TheLayout_RendersAConfirmationWrittenToEitherTempDataKey()
    {
        using var factory = CreateSeededFactory(out _);
        using var client = CreateClient(factory, AdminUserId);

        // UsersController used to write "SuccessMessage" while every other
        // controller wrote "Success"; only Users/Index rendered the former. Both
        // now go through the layout's one notice.
        var layout = ReadRepoFile("Areas/Admin/Views/Shared/_AdminLayout.cshtml");
        Assert.Contains("TempData[\"Success\"]", layout);

        var users = ReadRepoFile("Areas/Admin/Controllers/UsersController.cs");
        Assert.DoesNotContain("SuccessMessage", users);

        // ...and the page it redirects to no longer prints an alert of its own.
        var index = ReadRepoFile("Areas/Admin/Views/Users/Index.cshtml");
        Assert.DoesNotContain("alert-dismissible", index);

        var html = await client.GetStringAsync("/Admin/Users");
        Assert.Contains("portal-register__row", html);
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory, string userId)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add(AdminAuthenticationHandler.UserHeader, userId);
        return client;
    }

    private static IEnumerable<string> AdminPages(SeededRecords seeded) =>
    [
        "/Admin",
        "/Admin/AppointmentRequests",
        $"/Admin/AppointmentRequests/Details/{seeded.AppointmentRequestId}",
        $"/Admin/AppointmentRequests/Edit/{seeded.AppointmentRequestId}",
        $"/Admin/AppointmentRequests/Delete/{seeded.AppointmentRequestId}",
        "/Admin/Teleconsultations",
        $"/Admin/Teleconsultations/Details/{seeded.TeleconsultationId}",
        $"/Admin/Teleconsultations/Edit/{seeded.TeleconsultationId}",
        "/Admin/PatientProfiles",
        $"/Admin/PatientProfiles/Details/{seeded.PatientProfileId}",
        "/Admin/PatientProfiles/Create",
        $"/Admin/PatientProfiles/UploadDocument?patientId={seeded.PatientProfileId}",
        "/Admin/PatientAppointments",
        "/Admin/PatientAppointments/Create",
        $"/Admin/PatientAppointments/Edit/{seeded.PatientAppointmentId}",
        "/Admin/PatientMessages",
        $"/Admin/PatientMessages/Details/{seeded.PatientMessageId}",
        "/Admin/ContactSubmissions",
        $"/Admin/ContactSubmissions/Details/{seeded.ContactSubmissionId}",
        "/Admin/BillPayments",
        $"/Admin/BillPayments/Details/{seeded.BillPaymentId}",
        "/Admin/Posts",
        "/Admin/Posts/Create",
        $"/Admin/Posts/Edit/{seeded.PostId}",
        "/Admin/Users",
        "/Admin/Users/Create",
        $"/Admin/Users/EditRoles/{AdminUserId}",
        "/Admin/Availability",
        "/Admin/Integrations",
        "/Admin/Doctors",
        "/Admin/Doctors/Create",
        $"/Admin/Doctors/Details/{seeded.DoctorId}",
        $"/Admin/Doctors/Edit/{seeded.DoctorId}",
        $"/Admin/Doctors/Delete/{seeded.DoctorId}",
        "/Admin/Departments",
        "/Admin/Departments/Create",
        $"/Admin/Departments/Details/{seeded.DepartmentId}",
        $"/Admin/Departments/Edit/{seeded.DepartmentId}",
        $"/Admin/Departments/Delete/{seeded.DepartmentId}"
    ];

    private static WebApplicationFactory<Program> CreateSeededFactory(out SeededRecords seeded)
    {
        // A store of its own, so records seeded here cannot bleed into the
        // shared "OkaforHospitalTests" database the other suites use.
        var databaseName = $"AdminPortalRender-{Guid.NewGuid():N}";

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
                        .AddScheme<AuthenticationSchemeOptions, AdminAuthenticationHandler>(
                            TestAuthenticationScheme,
                            _ => { });
                });
            });

        seeded = Seed(factory);
        return factory;
    }

    private static SeededRecords Seed(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();

        foreach (var (id, email) in new[] { (AdminUserId, AdminEmail), (StaffUserId, StaffEmail) })
        {
            context.Users.Add(new ApplicationUser
            {
                Id = id,
                UserName = email,
                NormalizedUserName = email.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                EmailConfirmed = true
            });
        }

        var department = new Department { Name = "General Medicine", Description = "Everyday care." };
        context.Departments.Add(department);

        var doctor = new Doctor
        {
            FullName = "Dr. Render Test",
            Slug = "dr-render-test",
            Specialty = "General Practice",
            Bio = "Sees patients on weekday mornings.",
            Department = department
        };
        context.Doctors.Add(doctor);

        var profile = new PatientProfile
        {
            ApplicationUserId = AdminUserId,
            FullName = "Adaeze Render",
            Phone = "0803 000 0000",
            Address = "Umuchu"
        };
        context.PatientProfiles.Add(profile);
        context.SaveChanges();

        var appointment = new PatientAppointment
        {
            PatientProfileId = profile.Id,
            DepartmentId = department.Id,
            DoctorId = doctor.Id,
            AppointmentDate = DateTime.Now.AddDays(4).Date.AddHours(10).AddMinutes(30),
            Status = PatientAppointmentStatus.Confirmed,
            Notes = "Bring the referral letter."
        };
        context.PatientAppointments.Add(appointment);

        // Deliberately the oldest outstanding record, so the dashboard plate has
        // a known answer to name.
        var request = new AppointmentRequest
        {
            PatientName = "Adaeze Render",
            Email = AdminEmail,
            Phone = "0803 000 0000",
            DepartmentId = department.Id,
            DoctorId = doctor.Id,
            PreferredDate = DateTime.Now.AddDays(12).Date,
            PreferredTime = "09:00",
            Status = AppointmentStatus.Pending,
            Message = "Any morning would suit.",
            CreatedAt = DateTime.UtcNow.AddDays(-9)
        };
        context.AppointmentRequests.Add(request);

        var teleconsultation = new TeleconsultationRequest
        {
            PatientName = "Ifeoma Render",
            Email = "ifeoma.render@example.invalid",
            Phone = "0803 000 0001",
            DepartmentId = department.Id,
            DoctorId = doctor.Id,
            ConsultationType = TeleconsultationType.Video,
            PreferredDate = DateTime.Now.AddDays(6).Date,
            PreferredTime = "14:00",
            Reason = "Follow-up on test results.",
            ConsentAccepted = true,
            Status = TeleconsultationStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        context.TeleconsultationRequests.Add(teleconsultation);

        var message = new PatientMessage
        {
            PatientProfileId = profile.Id,
            Subject = "Moving my appointment",
            Body = "Could the visit be moved to the afternoon?",
            IsRead = false,
            SentAt = DateTime.UtcNow.AddDays(-1)
        };
        context.PatientMessages.Add(message);

        context.PatientDocuments.Add(new PatientDocument
        {
            PatientProfileId = profile.Id,
            Title = "blood-results.pdf",
            Description = "Filed by the laboratory.",
            FileUrl = "/secure/blood-results.pdf"
        });

        var submission = new ContactSubmission
        {
            Name = "Emeka Render",
            Email = "emeka.render@example.invalid",
            Subject = "Visiting hours",
            Message = "What are the visiting hours on a Sunday?"
        };
        context.ContactSubmissions.Add(submission);

        var payment = new BillPayment
        {
            InvoiceNumber = "INV-RENDER-0001",
            PatientName = "Adaeze Render",
            PatientEmail = AdminEmail,
            PatientPhone = "0803 000 0000",
            Amount = 15000m,
            Currency = "NGN",
            Provider = "Manual",
            Status = BillPaymentStatus.Pending
        };
        context.BillPayments.Add(payment);

        var post = new Post
        {
            Title = "A new clinic on Saturday mornings",
            Slug = "saturday-morning-clinic",
            Summary = "The general medicine clinic now opens on Saturday mornings.",
            Content = "The clinic opens at eight.",
            Published = true
        };
        context.Posts.Add(post);

        context.NotificationLogs.Add(new NotificationLog
        {
            Channel = "Email",
            Recipient = "ifeoma.render@example.invalid",
            MessageBody = "Your teleconsultation has been confirmed.",
            Success = true,
            TeleconsultationRequestId = teleconsultation.Id
        });

        context.SaveChanges();

        return new SeededRecords(
            department.Id,
            doctor.Id,
            profile.Id,
            appointment.Id,
            request.Id,
            teleconsultation.Id,
            message.Id,
            submission.Id,
            payment.Id,
            post.Id);
    }

    private static string ReadRepoFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find {relativePath} from {AppContext.BaseDirectory}.");
    }

    private sealed record SeededRecords(
        int DepartmentId,
        int DoctorId,
        int PatientProfileId,
        int PatientAppointmentId,
        int AppointmentRequestId,
        int TeleconsultationId,
        int PatientMessageId,
        int ContactSubmissionId,
        int BillPaymentId,
        int PostId);

    private sealed class AdminAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string UserHeader = "X-Test-Admin";

        public AdminAuthenticationHandler(
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

            var id = userId.ToString();
            var isStaffOnly = id == StaffUserId;

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, id),
                new Claim(ClaimTypes.Name, isStaffOnly ? StaffEmail : AdminEmail),
                new Claim(ClaimTypes.Role, isStaffOnly ? "Staff" : "Admin")
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, TestAuthenticationScheme));
            var ticket = new AuthenticationTicket(principal, TestAuthenticationScheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
