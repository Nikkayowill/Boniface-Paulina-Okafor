using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.Services;
using Okafor_.NET.ViewModels;
using AdminContactSubmissionsController = Okafor_.NET.Areas.Admin.Controllers.ContactSubmissionsController;

namespace Okafor_.NET.Tests.Integration.SqlServer;

[Collection(SqlServerIntegrationCollection.Name)]
[Trait("Category", "DatabaseIntegration")]
public sealed class ContactWorkflowTests : SqlServerIntegrationTestBase
{
    public ContactWorkflowTests(SqlServerIntegrationFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task Contact_ValidSubmission_NormalizesAndPersistsToSqlServer()
    {
        await using var context = Fixture.CreateDbContext();
        using var services = CreateServices(context);
        var controller = CreatePublicController(context, services);
        var beforeSubmission = DateTime.UtcNow;

        var result = await controller.Contact(new ContactSubmission
        {
            Name = "  Chiamaka Patient  ",
            Email = "  chiamaka.patient@example.test  ",
            Subject = "  Clinic opening hours  ",
            Message = "  Please confirm Saturday opening hours.  "
        });

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Contact");
        controller.TempData["ContactSuccess"].Should().Be(true);
        var saved = await context.ContactSubmissions.AsNoTracking().SingleAsync();
        saved.Name.Should().Be("Chiamaka Patient");
        saved.Email.Should().Be("chiamaka.patient@example.test");
        saved.Subject.Should().Be("Clinic opening hours");
        saved.Message.Should().Be("Please confirm Saturday opening hours.");
        saved.SubmittedAt.Should().BeOnOrAfter(beforeSubmission);
    }

    [Fact]
    public async Task Contact_InvalidSubmission_ReturnsErrorsWithoutWritingData()
    {
        await using var context = Fixture.CreateDbContext();
        using var services = CreateServices(context);
        var controller = CreatePublicController(context, services);

        var result = await controller.Contact(new ContactSubmission
        {
            Name = "   ",
            Email = "not-an-email",
            Subject = "   ",
            Message = "   "
        });

        result.Should().BeOfType<ViewResult>();
        controller.ModelState.IsValid.Should().BeFalse();
        (await context.ContactSubmissions.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task AdminInbox_OrdersReadsAndDeletesPersistedSubmissions()
    {
        await using var context = Fixture.CreateDbContext();
        var older = CreateSubmission("Older message", DateTime.UtcNow.AddHours(-2));
        var newer = CreateSubmission("Newer message", DateTime.UtcNow.AddHours(-1));
        context.ContactSubmissions.AddRange(older, newer);
        await context.SaveChangesAsync();
        using var services = CreateServices(context);
        var controller = CreateAdminController(context, services);

        var indexResult = await controller.Index();
        var inbox = indexResult.Should().BeOfType<ViewResult>().Which.Model
            .Should().BeAssignableTo<IReadOnlyCollection<ContactSubmission>>().Subject;
        inbox.Select(item => item.Subject).Should().Equal("Newer message", "Older message");

        var detailsResult = await controller.Details(older.Id);
        detailsResult.Should().BeOfType<ViewResult>().Which.Model
            .Should().BeEquivalentTo(older, options => options.Excluding(item => item.SubmittedAt));

        var deleteResult = await controller.Delete(older.Id);
        deleteResult.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Index");
        (await context.ContactSubmissions.AsNoTracking().Select(item => item.Subject).SingleAsync())
            .Should().Be("Newer message");
    }

    [Fact]
    public async Task Search_EntireSite_ReturnsRelevantPublishedContentAcrossSections()
    {
        await using var context = Fixture.CreateDbContext();
        await SeedSearchContentAsync(context);
        using var services = CreateServices(context);
        var controller = CreatePublicController(context, services);

        var result = await controller.Search("  care  ", "Entire Site");

        var model = result.Should().BeOfType<ViewResult>().Which.Model
            .Should().BeOfType<PublicSearchResultsViewModel>().Subject;
        model.Query.Should().Be("care");
        model.Scope.Should().Be("all");
        model.Doctors.Select(item => item.FullName).Should().Equal("Dr. Search Test");
        model.Services.Select(item => item.Name).Should().Equal("Preventive Care");
        model.News.Select(item => item.Title).Should().Equal("Preventive Care Update");
        model.PatientInformation.Should().NotBeEmpty();
        model.HasAnyResults.Should().BeTrue();
    }

    [Fact]
    public async Task Search_NewsScope_ExcludesOtherSectionsAndUnpublishedPosts()
    {
        await using var context = Fixture.CreateDbContext();
        await SeedSearchContentAsync(context);
        using var services = CreateServices(context);
        var controller = CreatePublicController(context, services);

        var result = await controller.Search("care", "news");

        var model = result.Should().BeOfType<ViewResult>().Which.Model
            .Should().BeOfType<PublicSearchResultsViewModel>().Subject;
        model.Scope.Should().Be("news");
        model.News.Select(item => item.Title).Should().Equal("Preventive Care Update");
        model.Doctors.Should().BeEmpty();
        model.Services.Should().BeEmpty();
        model.PatientInformation.Should().BeEmpty();
    }

    private static ContactSubmission CreateSubmission(string subject, DateTime submittedAt) => new()
    {
        Name = "Contact Workflow Patient",
        Email = "contact.workflow@example.test",
        Subject = subject,
        Message = "Fictional contact message used only by automated tests.",
        SubmittedAt = submittedAt
    };

    private static async Task SeedSearchContentAsync(ApplicationDbContext context)
    {
        var department = new Department
        {
            Name = "Preventive Care",
            Description = "Routine screening and health education"
        };
        context.Doctors.Add(new Doctor
        {
            FullName = "Dr. Search Test",
            Slug = "dr-search-test",
            Specialty = "Preventive Care",
            Bio = "Fictional search test clinician.",
            Qualifications = "Test qualification",
            Department = department
        });
        context.Posts.AddRange(
            new Post
            {
                Title = "Preventive Care Update",
                Slug = "preventive-care-update",
                Content = "New preventive care clinic information.",
                Published = true,
                CreatedAt = DateTime.UtcNow
            },
            new Post
            {
                Title = "Unpublished Care Draft",
                Slug = "unpublished-care-draft",
                Content = "This draft must not appear in public search.",
                Published = false,
                CreatedAt = DateTime.UtcNow.AddMinutes(1)
            });
        await context.SaveChangesAsync();
    }

    private static ServiceProvider CreateServices(ApplicationDbContext context)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(context);
        services.AddControllersWithViews();
        return services.BuildServiceProvider();
    }

    private static Okafor_.NET.Controllers.HomeController CreatePublicController(
        ApplicationDbContext context,
        IServiceProvider services)
    {
        var controller = new Okafor_.NET.Controllers.HomeController(context, new TestImageService());
        InitializeController(controller, services, isAdmin: false);
        return controller;
    }

    private static AdminContactSubmissionsController CreateAdminController(
        ApplicationDbContext context,
        IServiceProvider services)
    {
        var controller = new AdminContactSubmissionsController(context);
        InitializeController(controller, services, isAdmin: true);
        return controller;
    }

    private static void InitializeController(Controller controller, IServiceProvider services, bool isAdmin)
    {
        var claims = isAdmin
            ? new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "contact-admin"),
                new Claim(ClaimTypes.Name, "contact-admin@example.test"),
                new Claim(ClaimTypes.Role, "Admin")
            }
            : [];
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services,
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "ContactWorkflowTest"))
        };
        controller.ControllerContext = new ControllerContext(
            new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor()));
        controller.TempData = new TempDataDictionary(httpContext, new TestTempDataProvider());
        controller.ObjectValidator = services.GetRequiredService<IObjectModelValidator>();
    }

    private sealed class TestImageService : IImageService
    {
        public string GetRandomHospitalImage() => "/images/test-hospital.webp";

        public List<string> GetRandomHospitalImages(int count) =>
            Enumerable.Repeat("/images/test-hospital.webp", count).ToList();
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) =>
            new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}
