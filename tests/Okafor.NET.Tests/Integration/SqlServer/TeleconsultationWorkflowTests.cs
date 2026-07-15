using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Okafor_.NET.Data;
using Okafor_.NET.Hubs;
using Okafor_.NET.Models;
using Okafor_.NET.Services;
using Okafor_.NET.ViewModels;
using AdminTeleconsultationsController = Okafor_.NET.Areas.Admin.Controllers.TeleconsultationsController;
using PatientTeleconsultationsController = Okafor_.NET.Areas.Patient.Controllers.TeleconsultationsController;
using PublicTeleconsultationsController = Okafor_.NET.Controllers.TeleconsultationsController;

namespace Okafor_.NET.Tests.Integration.SqlServer;

[Collection(SqlServerIntegrationCollection.Name)]
[Trait("Category", "DatabaseIntegration")]
public sealed class TeleconsultationWorkflowTests : SqlServerIntegrationTestBase
{
    public TeleconsultationWorkflowTests(SqlServerIntegrationFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task PublicSubmission_ValidRequest_PersistsAndUsesProtectedConfirmationReference()
    {
        await using var context = Fixture.CreateDbContext();
        var clinicalData = await SeedClinicalDataAsync(context);
        using var services = CreateServices(context);
        var notifications = new RecordingNotificationService();
        var whatsApp = new RecordingWhatsAppService();
        var hub = new RecordingHubContext();
        var controller = CreatePublicController(context, services, notifications, whatsApp, hub);

        var result = await controller.Create(new TeleconsultationRequestViewModel
        {
            PatientName = "  Ada Virtual Patient  ",
            Email = "  ada.virtual@example.test  ",
            PhoneCountryCode = "+234",
            Phone = " 08012345678 ",
            WhatsAppOptIn = true,
            DepartmentId = clinicalData.DepartmentId,
            DoctorId = clinicalData.DoctorId,
            ConsultationType = TeleconsultationType.Video,
            PreferredDate = DateTime.Today.AddDays(2),
            PreferredTime = "10:00 AM",
            Reason = "  Follow-up consultation  ",
            ConsentAccepted = true
        });

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Submitted");
        var protectedReference = redirect.RouteValues!["reference"].Should().BeOfType<string>().Subject;
        protectedReference.Should().NotBe("1");
        context.ChangeTracker.Clear();
        var request = await context.TeleconsultationRequests.AsNoTracking().SingleAsync();
        request.PatientName.Should().Be("Ada Virtual Patient");
        request.Email.Should().Be("ada.virtual@example.test");
        request.Phone.Should().Be("+2348012345678");
        request.Reason.Should().Be("Follow-up consultation");
        request.Status.Should().Be(TeleconsultationStatus.Pending);
        request.ApplicationUserId.Should().BeNull();
        notifications.Calls.Should().Contain(["TeleconsultationReceived", "AdminAlert"]);
        whatsApp.Calls.Should().ContainSingle().Which.Should().Be("TeleconsultationReceived");
        hub.Messages.Should().ContainSingle(message => message.Method == "teleconsultationSubmitted");

        var submitted = await controller.Submitted(protectedReference);

        submitted.Should().BeOfType<ViewResult>()
            .Which.Model.Should().BeOfType<TeleconsultationRequest>()
            .Which.Id.Should().Be(request.Id);
    }

    [Fact]
    public async Task AdminConfirmation_PersistsStatusAndPublishesPatientUpdates()
    {
        await using var context = Fixture.CreateDbContext();
        var clinicalData = await SeedClinicalDataAsync(context);
        var request = CreateRequest(
            clinicalData.DepartmentId,
            clinicalData.DoctorId,
            "confirmed.patient@example.test");
        context.TeleconsultationRequests.Add(request);
        await context.SaveChangesAsync();
        using var services = CreateServices(context);
        var notifications = new RecordingNotificationService();
        var whatsApp = new RecordingWhatsAppService();
        var hub = new RecordingHubContext();
        var controller = new AdminTeleconsultationsController(
            context,
            notifications,
            whatsApp,
            hub,
            new TeleconsultationLifecycleService(),
            NullLogger<AdminTeleconsultationsController>.Instance);
        InitializeController(controller, services, userId: null);

        var result = await controller.Edit(request.Id, new AdminTeleconsultationUpdateViewModel
        {
            Id = request.Id,
            Status = TeleconsultationStatus.Confirmed,
            PreferredDate = DateTime.Today.AddDays(3),
            PreferredTime = " 11:00 AM ",
            MeetingLink = " https://meet.example.test/consultation ",
            AdminNotes = " Join five minutes early. "
        });

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Index");
        context.ChangeTracker.Clear();
        var updated = await context.TeleconsultationRequests.AsNoTracking().SingleAsync();
        updated.Status.Should().Be(TeleconsultationStatus.Confirmed);
        updated.PreferredTime.Should().Be("11:00 AM");
        updated.MeetingLink.Should().Be("https://meet.example.test/consultation");
        updated.AdminNotes.Should().Be("Join five minutes early.");
        updated.UpdatedAt.Should().NotBeNull();
        notifications.Calls.Should().ContainSingle().Which.Should().Be("TeleconsultationStatus:Confirmed");
        whatsApp.Calls.Should().ContainSingle().Which.Should().Be("TeleconsultationStatus");
        hub.Messages.Should().Contain(message => message.Method == "bookingStatusChanged");
        hub.Messages.Should().Contain(message => message.Method == "bookingActioned");
    }

    [Fact]
    public async Task PatientHistory_ReturnsOwnedAndMatchingGuestRequestsOnly()
    {
        await using var context = Fixture.CreateDbContext();
        var clinicalData = await SeedClinicalDataAsync(context);
        var currentUser = await SeedUserAsync(context, "tele-current");
        var otherUser = await SeedUserAsync(context, "tele-other");
        var owned = CreateRequest(clinicalData.DepartmentId, clinicalData.DoctorId, "old-email@example.test");
        owned.ApplicationUserId = currentUser.Id;
        var matchingGuest = CreateRequest(clinicalData.DepartmentId, clinicalData.DoctorId, currentUser.Email!);
        var other = CreateRequest(clinicalData.DepartmentId, clinicalData.DoctorId, otherUser.Email!);
        other.ApplicationUserId = otherUser.Id;
        context.TeleconsultationRequests.AddRange(owned, matchingGuest, other);
        await context.SaveChangesAsync();
        using var services = CreateServices(context);
        var controller = new PatientTeleconsultationsController(
            context,
            services.GetRequiredService<UserManager<ApplicationUser>>(),
            new RecordingHubContext(),
            NullLogger<PatientTeleconsultationsController>.Instance);
        InitializeController(controller, services, currentUser.Id);

        var result = await controller.Index();

        var history = result.Should().BeOfType<ViewResult>()
            .Which.Model.Should().BeAssignableTo<IEnumerable<PortalTeleconsultationViewModel>>()
            .Subject.ToList();
        history.Select(item => item.Id).Should().BeEquivalentTo([owned.Id, matchingGuest.Id]);
        history.Should().NotContain(item => item.Id == other.Id);
    }

    private static PublicTeleconsultationsController CreatePublicController(
        ApplicationDbContext context,
        IServiceProvider services,
        INotificationService notifications,
        IWhatsAppNotificationService whatsApp,
        IHubContext<BookingHub> hub)
    {
        var controller = new PublicTeleconsultationsController(
            context,
            notifications,
            whatsApp,
            services.GetRequiredService<UserManager<ApplicationUser>>(),
            hub,
            new EphemeralDataProtectionProvider(),
            NullLogger<PublicTeleconsultationsController>.Instance);
        InitializeController(controller, services, userId: null);
        return controller;
    }

    private static async Task<TestClinicalData> SeedClinicalDataAsync(ApplicationDbContext context)
    {
        var department = new Department
        {
            Name = $"Virtual Care {Guid.NewGuid():N}",
            Description = "Department used by SQL Server teleconsultation tests."
        };
        var doctor = new Doctor
        {
            FullName = "Dr. Virtual Workflow",
            Slug = $"dr-virtual-workflow-{Guid.NewGuid():N}",
            Specialty = "Virtual Care",
            Bio = "Fictional doctor used only by automated tests.",
            Qualifications = "Test qualification",
            Department = department
        };
        context.Doctors.Add(doctor);
        await context.SaveChangesAsync();
        return new TestClinicalData(department.Id, doctor.Id);
    }

    private static TeleconsultationRequest CreateRequest(
        int departmentId,
        int doctorId,
        string email) =>
        new()
        {
            PatientName = "Virtual Patient",
            Email = email,
            Phone = "+2348012345678",
            WhatsAppOptIn = true,
            DepartmentId = departmentId,
            DoctorId = doctorId,
            ConsultationType = TeleconsultationType.Video,
            PreferredDate = DateTime.Today.AddDays(2),
            PreferredTime = "10:00 AM",
            Reason = "Follow-up virtual care",
            ConsentAccepted = true,
            Status = TeleconsultationStatus.Pending
        };

    private static async Task<ApplicationUser> SeedUserAsync(ApplicationDbContext context, string id)
    {
        var email = $"{id}@example.test";
        var user = new ApplicationUser
        {
            Id = id,
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
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
        services.AddControllersWithViews();
        return services.BuildServiceProvider();
    }

    private static void InitializeController(Controller controller, IServiceProvider services, string? userId)
    {
        var httpContext = new DefaultHttpContext { RequestServices = services };
        if (userId is not null)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, $"{userId}@example.test"),
                new Claim(ClaimTypes.Role, "Patient")
            ], "TeleconsultationWorkflowTest"));
        }

        controller.ControllerContext = new ControllerContext(
            new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor()));
        controller.TempData = new TempDataDictionary(httpContext, new TestTempDataProvider());
        controller.ObjectValidator = services.GetRequiredService<IObjectModelValidator>();
    }

    private sealed record TestClinicalData(int DepartmentId, int DoctorId);

    private sealed class RecordingNotificationService : INotificationService
    {
        public List<string> Calls { get; } = [];

        public Task<bool> SendConfirmationAsync(NotificationRequest request) => RecordAsync("Confirmation");
        public Task<bool> SendAdminAlertAsync(NotificationRequest request) => RecordAsync("AdminAlert");
        public Task<bool> SendReminderAsync(NotificationRequest request) => RecordAsync("Reminder");
        public Task<bool> SendAppointmentStatusAsync(NotificationRequest request, string status, string nextStep) =>
            RecordAsync($"AppointmentStatus:{status}");
        public Task<bool> SendTeleconsultationReceivedAsync(NotificationRequest request) =>
            RecordAsync("TeleconsultationReceived");
        public Task<bool> SendTeleconsultationStatusAsync(NotificationRequest request, string status, string nextStep) =>
            RecordAsync($"TeleconsultationStatus:{status}");

        private Task<bool> RecordAsync(string call)
        {
            Calls.Add(call);
            return Task.FromResult(true);
        }
    }

    private sealed class RecordingWhatsAppService : IWhatsAppNotificationService
    {
        public List<string> Calls { get; } = [];

        public Task<bool> SendTextMessageAsync(string recipientPhone, string message, CancellationToken cancellationToken = default) =>
            RecordAsync("Text");

        public Task<bool> SendTeleconsultationReceivedAsync(TeleconsultationRequest request, CancellationToken cancellationToken = default) =>
            RecordAsync("TeleconsultationReceived");

        public Task<bool> SendTeleconsultationStatusAsync(TeleconsultationRequest request, CancellationToken cancellationToken = default) =>
            RecordAsync("TeleconsultationStatus");

        private Task<bool> RecordAsync(string call)
        {
            Calls.Add(call);
            return Task.FromResult(true);
        }
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) =>
            new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }

    private sealed class RecordingHubContext : IHubContext<BookingHub>
    {
        private readonly RecordingClientProxy _proxy = new();

        public IReadOnlyCollection<HubMessage> Messages => _proxy.Messages;
        public IHubClients Clients => new RecordingHubClients(_proxy);
        public IGroupManager Groups { get; } = new NoOpGroupManager();
    }

    private sealed class RecordingHubClients(IClientProxy proxy) : IHubClients
    {
        public IClientProxy All => proxy;
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => proxy;
        public IClientProxy Client(string connectionId) => proxy;
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => proxy;
        public IClientProxy Group(string groupName) => proxy;
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => proxy;
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => proxy;
        public IClientProxy User(string userId) => proxy;
        public IClientProxy Users(IReadOnlyList<string> userIds) => proxy;
    }

    private sealed class RecordingClientProxy : IClientProxy
    {
        private readonly List<HubMessage> _messages = [];

        public IReadOnlyCollection<HubMessage> Messages => _messages;

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            _messages.Add(new HubMessage(method, args));
            return Task.CompletedTask;
        }
    }

    private sealed class NoOpGroupManager : IGroupManager
    {
        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed record HubMessage(string Method, object?[] Arguments);
}
