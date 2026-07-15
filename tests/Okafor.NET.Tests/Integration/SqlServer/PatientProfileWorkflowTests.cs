using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Okafor_.NET.Areas.Patient.Controllers;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.ViewModels;

namespace Okafor_.NET.Tests.Integration.SqlServer;

[Collection(SqlServerIntegrationCollection.Name)]
[Trait("Category", "DatabaseIntegration")]
public sealed class PatientProfileWorkflowTests : SqlServerIntegrationTestBase
{
    public PatientProfileWorkflowTests(SqlServerIntegrationFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task Create_ValidProfile_NormalizesAndPersistsForCurrentPatient()
    {
        await using var context = Fixture.CreateDbContext();
        var user = await SeedUserAsync(context, "patient-create");
        using var services = CreateServices(context);
        var controller = CreateController(context, services, user.Id);

        var result = await controller.Create(new PatientProfileEditViewModel
        {
            FullName = "  Ada Eze  ",
            Phone = "  +234 800 000 0000  ",
            Address = "  Enugu, Nigeria  "
        });

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Index");
        var profile = await context.PatientProfiles.AsNoTracking().SingleAsync();
        profile.ApplicationUserId.Should().Be(user.Id);
        profile.FullName.Should().Be("Ada Eze");
        profile.Phone.Should().Be("+234 800 000 0000");
        profile.Address.Should().Be("Enugu, Nigeria");
    }

    [Fact]
    public async Task Edit_ValidProfile_UpdatesOnlyCurrentPatientsRecord()
    {
        await using var context = Fixture.CreateDbContext();
        var currentUser = await SeedUserAsync(context, "patient-current");
        var otherUser = await SeedUserAsync(context, "patient-other");
        context.PatientProfiles.AddRange(
            new PatientProfile { ApplicationUserId = currentUser.Id, FullName = "Before Name" },
            new PatientProfile { ApplicationUserId = otherUser.Id, FullName = "Other Patient" });
        await context.SaveChangesAsync();
        using var services = CreateServices(context);
        var controller = CreateController(context, services, currentUser.Id);

        var result = await controller.Edit(new PatientProfileEditViewModel
        {
            FullName = "  Updated Name  ",
            Phone = "  +1 902 555 0100  ",
            Address = "  Halifax, Canada  "
        });

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Index");
        var profiles = await context.PatientProfiles.AsNoTracking()
            .OrderBy(profile => profile.ApplicationUserId)
            .ToListAsync();
        profiles.Single(profile => profile.ApplicationUserId == currentUser.Id).FullName
            .Should().Be("Updated Name");
        profiles.Single(profile => profile.ApplicationUserId == otherUser.Id).FullName
            .Should().Be("Other Patient");
    }

    [Fact]
    public async Task Database_RejectsSecondProfileForSameApplicationUser()
    {
        await using var context = Fixture.CreateDbContext();
        var user = await SeedUserAsync(context, "patient-unique");
        context.PatientProfiles.Add(new PatientProfile
        {
            ApplicationUserId = user.Id,
            FullName = "First Profile"
        });
        await context.SaveChangesAsync();
        context.PatientProfiles.Add(new PatientProfile
        {
            ApplicationUserId = user.Id,
            FullName = "Duplicate Profile"
        });

        Func<Task> saveDuplicate = () => context.SaveChangesAsync();

        await saveDuplicate.Should().ThrowAsync<DbUpdateException>();
    }

    private static async Task<ApplicationUser> SeedUserAsync(ApplicationDbContext context, string id)
    {
        var user = new ApplicationUser
        {
            Id = id,
            UserName = $"{id}@example.test",
            NormalizedUserName = $"{id.ToUpperInvariant()}@EXAMPLE.TEST",
            Email = $"{id}@example.test",
            NormalizedEmail = $"{id.ToUpperInvariant()}@EXAMPLE.TEST",
            SecurityStamp = Guid.NewGuid().ToString("N")
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private static ServiceProvider CreateServices(ApplicationDbContext context)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(context);
        services.AddIdentityCore<ApplicationUser>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        return services.BuildServiceProvider();
    }

    private static ProfileController CreateController(
        ApplicationDbContext context,
        IServiceProvider services,
        string userId)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, $"{userId}@example.test"),
                new Claim(ClaimTypes.Role, "Patient")
            ], "ProfileWorkflowTest"))
        };
        var controller = new ProfileController(
            context,
            services.GetRequiredService<UserManager<ApplicationUser>>())
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new TestTempDataProvider())
        };
        return controller;
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
