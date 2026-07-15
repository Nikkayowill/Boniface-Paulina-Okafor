using System.Security.Claims;
using FluentAssertions;
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
using Okafor_.NET.Areas.Patient.Controllers;
using Okafor_.NET.Data;
using Okafor_.NET.Hubs;
using Okafor_.NET.Models;
using Okafor_.NET.ViewModels;

namespace Okafor_.NET.Tests.Integration.SqlServer;

[Collection(SqlServerIntegrationCollection.Name)]
[Trait("Category", "DatabaseIntegration")]
public sealed class PatientMessagingAndCancellationWorkflowTests : SqlServerIntegrationTestBase
{
    public PatientMessagingAndCancellationWorkflowTests(SqlServerIntegrationFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task Send_ValidMessage_NormalizesAndStoresItForCurrentPatient()
    {
        await using var context = Fixture.CreateDbContext();
        var user = await SeedUserWithProfileAsync(context, "message-sender", "Message Sender");
        using var services = CreateServices(context);
        var controller = CreateMessagesController(context, services, user.Id);

        var result = await controller.Send(new PatientMessageViewModel
        {
            Subject = "  Prescription question  ",
            Body = "  Please call me about my prescription.  "
        });

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Index");
        var message = await context.PatientMessages.AsNoTracking().SingleAsync();
        message.Subject.Should().Be("Prescription question");
        message.Body.Should().Be("Please call me about my prescription.");
        message.IsRead.Should().BeFalse();
        message.PatientProfileId.Should().Be(user.ProfileId);
    }

    [Fact]
    public async Task Send_WhitespaceOnlyMessage_IsRejectedWithoutWritingData()
    {
        await using var context = Fixture.CreateDbContext();
        var user = await SeedUserWithProfileAsync(context, "invalid-message", "Invalid Message Patient");
        using var services = CreateServices(context);
        var controller = CreateMessagesController(context, services, user.Id);

        var result = await controller.Send(new PatientMessageViewModel
        {
            Subject = "   ",
            Body = "   "
        });

        result.Should().BeOfType<ViewResult>();
        controller.ModelState.IsValid.Should().BeFalse();
        (await context.PatientMessages.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Index_ReturnsOnlyCurrentPatientsMessages()
    {
        await using var context = Fixture.CreateDbContext();
        var current = await SeedUserWithProfileAsync(context, "message-owner", "Message Owner");
        var other = await SeedUserWithProfileAsync(context, "message-other", "Other Patient");
        context.PatientMessages.AddRange(
            new PatientMessage { PatientProfileId = current.ProfileId, Subject = "Mine", Body = "Owned message" },
            new PatientMessage { PatientProfileId = other.ProfileId, Subject = "Not mine", Body = "Other message" });
        await context.SaveChangesAsync();
        using var services = CreateServices(context);
        var controller = CreateMessagesController(context, services, current.Id);

        var result = await controller.Index();

        var messages = result.Should().BeOfType<ViewResult>()
            .Which.Model.Should().BeAssignableTo<IEnumerable<PatientMessage>>()
            .Subject.ToList();
        messages.Should().ContainSingle().Which.Subject.Should().Be("Mine");
    }

    [Fact]
    public async Task Cancel_ScheduledAppointment_CancelsLinkedRequestAndReleasesSlot()
    {
        await using var context = Fixture.CreateDbContext();
        var patient = await SeedUserWithProfileAsync(context, "scheduled-cancel", "Scheduled Patient");
        var clinicalData = await SeedClinicalDataAsync(context);
        var request = CreateRequest(patient.Email!, clinicalData.DepartmentId, clinicalData.DoctorId, AppointmentStatus.Approved);
        context.AppointmentRequests.Add(request);
        await context.SaveChangesAsync();
        var appointment = new PatientAppointment
        {
            PatientProfileId = patient.ProfileId,
            AppointmentRequestId = request.Id,
            DepartmentId = clinicalData.DepartmentId,
            DoctorId = clinicalData.DoctorId,
            AppointmentDate = DateTime.Now.AddDays(3),
            Status = PatientAppointmentStatus.Confirmed,
            Notes = "Bring test results."
        };
        var slot = new AppointmentSlot
        {
            DoctorId = clinicalData.DoctorId,
            SlotDateTime = appointment.AppointmentDate,
            IsBooked = true,
            ReminderSent = true,
            AppointmentRequestId = request.Id
        };
        context.AddRange(appointment, slot);
        await context.SaveChangesAsync();
        var hub = new RecordingHubContext();
        using var services = CreateServices(context);
        var controller = CreateAppointmentsController(context, services, patient.Id, hub);

        var result = await controller.Cancel("scheduled", appointment.Id);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Index");
        context.ChangeTracker.Clear();
        (await context.PatientAppointments.SingleAsync()).Status.Should().Be(PatientAppointmentStatus.Cancelled);
        var savedRequest = await context.AppointmentRequests.SingleAsync();
        savedRequest.Status.Should().Be(AppointmentStatus.Cancelled);
        savedRequest.ContactNotes.Should().Contain("Cancelled by patient");
        var savedSlot = await context.AppointmentSlots.SingleAsync();
        savedSlot.IsBooked.Should().BeFalse();
        savedSlot.ReminderSent.Should().BeFalse();
        savedSlot.AppointmentRequestId.Should().BeNull();
        hub.Messages.Should().ContainSingle(message => message.Method == "bookingActioned");
    }

    [Fact]
    public async Task Cancel_PendingRequest_CancelsOnlyRequestOwnedByCurrentEmail()
    {
        await using var context = Fixture.CreateDbContext();
        var patient = await SeedUserWithProfileAsync(context, "request-cancel", "Request Patient");
        var other = await SeedUserWithProfileAsync(context, "request-other", "Other Request Patient");
        var clinicalData = await SeedClinicalDataAsync(context);
        var ownedRequest = CreateRequest(patient.Email!, clinicalData.DepartmentId, clinicalData.DoctorId, AppointmentStatus.Pending);
        var otherRequest = CreateRequest(other.Email!, clinicalData.DepartmentId, clinicalData.DoctorId, AppointmentStatus.Pending);
        context.AppointmentRequests.AddRange(ownedRequest, otherRequest);
        await context.SaveChangesAsync();
        using var services = CreateServices(context);
        var controller = CreateAppointmentsController(context, services, patient.Id, new RecordingHubContext());

        var result = await controller.Cancel("request", ownedRequest.Id);

        result.Should().BeOfType<RedirectToActionResult>();
        context.ChangeTracker.Clear();
        (await context.AppointmentRequests.FindAsync(ownedRequest.Id))!.Status.Should().Be(AppointmentStatus.Cancelled);
        (await context.AppointmentRequests.FindAsync(otherRequest.Id))!.Status.Should().Be(AppointmentStatus.Pending);
    }

    [Fact]
    public async Task Cancel_AnotherPatientsScheduledAppointment_ReturnsNotFoundWithoutChanges()
    {
        await using var context = Fixture.CreateDbContext();
        var current = await SeedUserWithProfileAsync(context, "cancel-current", "Current Patient");
        var other = await SeedUserWithProfileAsync(context, "cancel-other", "Other Patient");
        var clinicalData = await SeedClinicalDataAsync(context);
        var appointment = new PatientAppointment
        {
            PatientProfileId = other.ProfileId,
            DepartmentId = clinicalData.DepartmentId,
            DoctorId = clinicalData.DoctorId,
            AppointmentDate = DateTime.Now.AddDays(3),
            Status = PatientAppointmentStatus.Confirmed
        };
        context.PatientAppointments.Add(appointment);
        await context.SaveChangesAsync();
        using var services = CreateServices(context);
        var controller = CreateAppointmentsController(context, services, current.Id, new RecordingHubContext());

        var result = await controller.Cancel("scheduled", appointment.Id);

        result.Should().BeOfType<NotFoundResult>();
        context.ChangeTracker.Clear();
        (await context.PatientAppointments.SingleAsync()).Status.Should().Be(PatientAppointmentStatus.Confirmed);
    }

    [Fact]
    public async Task Cancel_UnsupportedSourceType_ReturnsBadRequest()
    {
        await using var context = Fixture.CreateDbContext();
        var patient = await SeedUserWithProfileAsync(context, "invalid-source", "Invalid Source Patient");
        using var services = CreateServices(context);
        var controller = CreateAppointmentsController(context, services, patient.Id, new RecordingHubContext());

        var result = await controller.Cancel("unknown", 123);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static async Task<TestPatient> SeedUserWithProfileAsync(
        ApplicationDbContext context,
        string id,
        string fullName)
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
        var profile = new PatientProfile
        {
            ApplicationUser = user,
            FullName = fullName
        };
        context.PatientProfiles.Add(profile);
        await context.SaveChangesAsync();
        return new TestPatient(user.Id, email, profile.Id);
    }

    private static async Task<TestClinicalData> SeedClinicalDataAsync(ApplicationDbContext context)
    {
        var department = new Department
        {
            Name = $"Patient workflow {Guid.NewGuid():N}",
            Description = "Department used by SQL Server workflow tests."
        };
        var doctor = new Doctor
        {
            FullName = "Dr. Patient Workflow",
            Slug = $"dr-patient-workflow-{Guid.NewGuid():N}",
            Specialty = "General Medicine",
            Bio = "Fictional doctor used only by automated tests.",
            Qualifications = "Test qualification",
            Department = department
        };
        context.Doctors.Add(doctor);
        await context.SaveChangesAsync();
        return new TestClinicalData(department.Id, doctor.Id);
    }

    private static AppointmentRequest CreateRequest(
        string email,
        int departmentId,
        int doctorId,
        AppointmentStatus status) =>
        new()
        {
            PatientName = "Patient Workflow",
            Email = email,
            Phone = "+2348000000000",
            DepartmentId = departmentId,
            DoctorId = doctorId,
            PreferredDate = DateTime.Today.AddDays(3),
            PreferredTime = "09:00",
            Status = status
        };

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

    private static MessagesController CreateMessagesController(
        ApplicationDbContext context,
        IServiceProvider services,
        string userId)
    {
        var controller = new MessagesController(
            context,
            services.GetRequiredService<UserManager<ApplicationUser>>());
        InitializeController(controller, services, userId);
        return controller;
    }

    private static AppointmentsController CreateAppointmentsController(
        ApplicationDbContext context,
        IServiceProvider services,
        string userId,
        IHubContext<BookingHub> hub)
    {
        var controller = new AppointmentsController(
            context,
            services.GetRequiredService<UserManager<ApplicationUser>>(),
            hub,
            NullLogger<AppointmentsController>.Instance);
        InitializeController(controller, services, userId);
        return controller;
    }

    private static void InitializeController(Controller controller, IServiceProvider services, string userId)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services,
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, $"{userId}@example.test"),
                new Claim(ClaimTypes.Role, "Patient")
            ], "PatientWorkflowTest"))
        };
        controller.ControllerContext = new ControllerContext(
            new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor()));
        controller.TempData = new TempDataDictionary(httpContext, new TestTempDataProvider());
        controller.ObjectValidator = services.GetRequiredService<IObjectModelValidator>();
    }

    private sealed record TestPatient(string Id, string Email, int ProfileId);

    private sealed record TestClinicalData(int DepartmentId, int DoctorId);

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

        public Task SendCoreAsync(
            string method,
            object?[] args,
            CancellationToken cancellationToken = default)
        {
            _messages.Add(new HubMessage(method, args));
            return Task.CompletedTask;
        }
    }

    private sealed class NoOpGroupManager : IGroupManager
    {
        public Task AddToGroupAsync(
            string connectionId,
            string groupName,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RemoveFromGroupAsync(
            string connectionId,
            string groupName,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed record HubMessage(string Method, object?[] Arguments);
}
