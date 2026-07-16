using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Okafor_.NET.Data;
using Okafor_.NET.Hubs;
using Okafor_.NET.Models;
using Okafor_.NET.Services;
using Okafor_.NET.ViewModels;
using AdminAppointmentRequestsController = Okafor_.NET.Areas.Admin.Controllers.AppointmentRequestsController;
using PublicAppointmentRequestsController = Okafor_.NET.Controllers.AppointmentRequestsController;

namespace Okafor_.NET.Tests.Integration.SqlServer;

[Collection(SqlServerIntegrationCollection.Name)]
[Trait("Category", "DatabaseIntegration")]
public sealed class AppointmentWorkflowTests : SqlServerIntegrationTestBase
{
    public AppointmentWorkflowTests(SqlServerIntegrationFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task BookSlot_ValidRequest_PersistsNormalizedRequestAndReservesSlot()
    {
        await using var context = Fixture.CreateDbContext();
        var clinicalData = await SeedClinicalDataAsync(context);
        using var services = CreateServices(context);
        var notifications = new RecordingNotificationService();
        var hub = new RecordingHubContext();
        var controller = CreatePublicController(context, services, notifications, hub);

        var result = await controller.BookSlot(new BookSlotViewModel
        {
            DoctorId = clinicalData.DoctorId,
            SlotDate = clinicalData.SlotDateTime.ToString("yyyy-MM-dd"),
            SlotTime = clinicalData.SlotDateTime.ToString("HH:mm"),
            PatientName = "  Ada Patient  ",
            PatientEmail = "  ada.patient@example.test  ",
            PatientPhone = "  +2348000000000  ",
            ReasonForVisit = "  Routine consultation  "
        });

        var payload = JsonSerializer.SerializeToElement(result.Should().BeOfType<JsonResult>().Which.Value);
        payload.GetProperty("success").GetBoolean().Should().BeTrue();

        var request = await context.AppointmentRequests.AsNoTracking().SingleAsync();
        request.PatientName.Should().Be("Ada Patient");
        request.Email.Should().Be("ada.patient@example.test");
        request.Phone.Should().Be("+2348000000000");
        request.Message.Should().Be("Routine consultation");
        request.Status.Should().Be(AppointmentStatus.Pending);

        var slot = await context.AppointmentSlots.AsNoTracking().SingleAsync();
        slot.DoctorId.Should().Be(clinicalData.DoctorId);
        slot.SlotDateTime.Should().Be(clinicalData.SlotDateTime);
        slot.AppointmentRequestId.Should().Be(request.Id);
        slot.IsBooked.Should().BeTrue();
        notifications.ConfirmationRequests.Should().ContainSingle();
        notifications.AdminAlertRequests.Should().ContainSingle();
        hub.Messages.Select(message => message.Method)
            .Should().Contain(["appointmentSubmitted", "slotBooked"]);
    }

    [Fact]
    public async Task Create_ValidFallbackForm_PersistsReservesAndNotifies()
    {
        await using var context = Fixture.CreateDbContext();
        var clinicalData = await SeedClinicalDataAsync(context);
        using var services = CreateServices(context);
        var notifications = new RecordingNotificationService();
        var hub = new RecordingHubContext();
        var controller = CreatePublicController(context, services, notifications, hub);

        var result = await controller.Create(new AppointmentRequest
        {
            PatientName = "  Grace Patient  ",
            Email = "  grace.patient@example.test  ",
            Phone = "  +2348111111111  ",
            DepartmentId = clinicalData.DepartmentId,
            DoctorId = clinicalData.DoctorId,
            PreferredDate = clinicalData.SlotDateTime.Date,
            PreferredTime = clinicalData.SlotDateTime.ToString("HH:mm"),
            Message = "  Low-bandwidth form submission  "
        });

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Submitted");
        var request = await context.AppointmentRequests.AsNoTracking().SingleAsync();
        request.PatientName.Should().Be("Grace Patient");
        request.Email.Should().Be("grace.patient@example.test");
        request.Message.Should().Be("Low-bandwidth form submission");
        var slot = await context.AppointmentSlots.AsNoTracking().SingleAsync();
        slot.AppointmentRequestId.Should().Be(request.Id);
        slot.IsBooked.Should().BeTrue();
        notifications.ConfirmationRequests.Should().ContainSingle()
            .Which.AppointmentRequestId.Should().Be(request.Id);
        notifications.AdminAlertRequests.Should().ContainSingle();
    }

    [Fact]
    public async Task Create_BeyondBookingWindow_IsRejectedWithoutPersistence()
    {
        await using var context = Fixture.CreateDbContext();
        var clinicalData = await SeedClinicalDataAsync(context);
        using var services = CreateServices(context);
        var controller = CreatePublicController(
            context,
            services,
            new RecordingNotificationService(),
            new RecordingHubContext());

        var result = await controller.Create(new AppointmentRequest
        {
            PatientName = "Future Patient",
            Email = "future.patient@example.test",
            Phone = "+2348222222222",
            DepartmentId = clinicalData.DepartmentId,
            DoctorId = clinicalData.DoctorId,
            PreferredDate = DateTime.Today.AddDays(61),
            PreferredTime = "09:00",
            Message = "Attempt outside the booking window"
        });

        result.Should().BeOfType<ViewResult>();
        controller.ModelState[nameof(AppointmentRequest.PreferredDate)]!.Errors
            .Should().ContainSingle(error => error.ErrorMessage.Contains("60 days"));
        (await context.AppointmentRequests.CountAsync()).Should().Be(0);
        (await context.AppointmentSlots.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task GetByDepartment_ReturnsOnlyMatchingDoctorsInNameOrder()
    {
        await using var context = Fixture.CreateDbContext();
        var primary = new Department { Name = "Primary Care" };
        var dental = new Department { Name = "Dental" };
        context.Doctors.AddRange(
            CreateDoctor("Dr. Zainab Bello", "dr-zainab-bello", primary),
            CreateDoctor("Dr. Ada Eze", "dr-ada-eze", primary),
            CreateDoctor("Dr. Chidi Okoro", "dr-chidi-okoro", dental));
        await context.SaveChangesAsync();

        var controller = new Okafor_.NET.Controllers.DoctorsController(context);
        var result = await controller.GetByDepartment(primary.Id);

        var payload = JsonSerializer.SerializeToElement(result.Should().BeOfType<JsonResult>().Which.Value);
        payload.GetArrayLength().Should().Be(2);
        payload.EnumerateArray()
            .Select(item => item.GetProperty("FullName").GetString())
            .Should().Equal("Dr. Ada Eze", "Dr. Zainab Bello");
    }

    [Fact]
    public async Task ReserveSlot_ConcurrentSqlServerRequests_ExactlyOneWins()
    {
        int doctorId;
        DateTime slotDateTime;
        int firstRequestId;
        int secondRequestId;

        await using (var seedContext = Fixture.CreateDbContext())
        {
            var clinicalData = await SeedClinicalDataAsync(seedContext);
            doctorId = clinicalData.DoctorId;
            slotDateTime = clinicalData.SlotDateTime;
            var first = CreateAppointmentRequest("first@example.test", clinicalData);
            var second = CreateAppointmentRequest("second@example.test", clinicalData);
            seedContext.AppointmentRequests.AddRange(first, second);
            await seedContext.SaveChangesAsync();
            firstRequestId = first.Id;
            secondRequestId = second.Id;
        }

        await using var firstContext = Fixture.CreateDbContext();
        await using var secondContext = Fixture.CreateDbContext();
        var firstService = new AvailabilityService(firstContext, NullLogger<AvailabilityService>.Instance);
        var secondService = new AvailabilityService(secondContext, NullLogger<AvailabilityService>.Instance);

        var outcomes = await Task.WhenAll(
            firstService.ReserveSlotAsync(doctorId, slotDateTime, firstRequestId),
            secondService.ReserveSlotAsync(doctorId, slotDateTime, secondRequestId));

        outcomes.Count(outcome => outcome.Success).Should().Be(1);
        outcomes.Count(outcome => !outcome.Success).Should().Be(1);
        await using var verificationContext = Fixture.CreateDbContext();
        var persistedSlot = await verificationContext.AppointmentSlots.AsNoTracking().SingleAsync();
        persistedSlot.AppointmentRequestId.Should().BeOneOf(firstRequestId, secondRequestId);
        persistedSlot.IsBooked.Should().BeTrue();
    }

    [Fact]
    public async Task AdminDecision_ApprovalThenRejection_ReservesThenReleasesSlot()
    {
        int requestId;
        ClinicalData clinicalData;
        await using (var seedContext = Fixture.CreateDbContext())
        {
            clinicalData = await SeedClinicalDataAsync(seedContext);
            var request = CreateAppointmentRequest("decision@example.test", clinicalData);
            seedContext.AppointmentRequests.Add(request);
            await seedContext.SaveChangesAsync();
            requestId = request.Id;
        }

        var notifications = new RecordingNotificationService();
        var hub = new RecordingHubContext();
        await using (var approvalContext = Fixture.CreateDbContext())
        using (var services = CreateServices(approvalContext))
        {
            var controller = CreateAdminController(approvalContext, services, notifications, hub);
            var result = await controller.Edit(requestId, new AdminAppointmentRequestUpdateViewModel
            {
                Id = requestId,
                Status = AppointmentStatus.Approved,
                DoctorId = clinicalData.DoctorId,
                ContactConfirmed = true,
                ContactMethod = "Email",
                ContactNotes = "  Confirmed with patient  "
            });

            result.Should().BeOfType<RedirectToActionResult>();
        }

        await using (var approvalVerification = Fixture.CreateDbContext())
        {
            var approved = await approvalVerification.AppointmentRequests.AsNoTracking().SingleAsync();
            approved.Status.Should().Be(AppointmentStatus.Approved);
            approved.ApprovedAt.Should().NotBeNull();
            approved.ApprovedByUserId.Should().Be("launch-admin@example.test");
            approved.ContactNotes.Should().Be("Confirmed with patient");
            var reservedSlot = await approvalVerification.AppointmentSlots.AsNoTracking().SingleAsync();
            reservedSlot.IsBooked.Should().BeTrue();
            reservedSlot.AppointmentRequestId.Should().Be(requestId);
        }

        await using (var rejectionContext = Fixture.CreateDbContext())
        using (var services = CreateServices(rejectionContext))
        {
            var controller = CreateAdminController(rejectionContext, services, notifications, hub);
            var result = await controller.Edit(requestId, new AdminAppointmentRequestUpdateViewModel
            {
                Id = requestId,
                Status = AppointmentStatus.Rejected,
                DoctorId = clinicalData.DoctorId,
                ContactConfirmed = true,
                ContactMethod = "Email",
                ContactNotes = "Alternative care instructions provided"
            });

            result.Should().BeOfType<RedirectToActionResult>();
        }

        await using var rejectionVerification = Fixture.CreateDbContext();
        var rejected = await rejectionVerification.AppointmentRequests.AsNoTracking().SingleAsync();
        rejected.Status.Should().Be(AppointmentStatus.Rejected);
        rejected.ApprovedAt.Should().BeNull();
        rejected.ApprovedByUserId.Should().BeNull();
        var releasedSlot = await rejectionVerification.AppointmentSlots.AsNoTracking().SingleAsync();
        releasedSlot.IsBooked.Should().BeFalse();
        releasedSlot.AppointmentRequestId.Should().BeNull();
        notifications.StatusUpdates.Select(update => update.Status)
            .Should().Equal("Approved", "Not Approved");
    }

    private static async Task<ClinicalData> SeedClinicalDataAsync(ApplicationDbContext context)
    {
        var slotDate = NextOccurrence(DayOfWeek.Monday);
        var department = new Department { Name = "General Medicine" };
        var doctor = CreateDoctor("Dr. Launch Test", "dr-launch-test", department);
        context.Doctors.Add(doctor);
        context.DoctorAvailabilities.Add(new DoctorAvailability
        {
            Doctor = doctor,
            DayOfWeek = slotDate.DayOfWeek,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(11, 0, 0),
            SlotDurationMinutes = 30,
            IsActive = true
        });
        await context.SaveChangesAsync();
        return new ClinicalData(department.Id, doctor.Id, slotDate.AddHours(9));
    }

    private static Doctor CreateDoctor(string name, string slug, Department department) => new()
    {
        FullName = name,
        Slug = slug,
        Specialty = department.Name,
        Bio = "Fictional clinician used only by automated tests.",
        Qualifications = "Test qualification",
        Department = department
    };

    private static AppointmentRequest CreateAppointmentRequest(string email, ClinicalData data) => new()
    {
        PatientName = "Appointment Workflow Patient",
        Email = email,
        Phone = "+2348000000000",
        DepartmentId = data.DepartmentId,
        DoctorId = data.DoctorId,
        PreferredDate = data.SlotDateTime.Date,
        PreferredTime = data.SlotDateTime.ToString("HH:mm"),
        Message = "Fictional appointment workflow request.",
        Status = AppointmentStatus.Pending
    };

    private static DateTime NextOccurrence(DayOfWeek dayOfWeek)
    {
        var date = DateTime.Today.AddDays(7);
        while (date.DayOfWeek != dayOfWeek)
        {
            date = date.AddDays(1);
        }

        return date;
    }

    private static ServiceProvider CreateServices(ApplicationDbContext context)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(context);
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Notifications:WhatsAppNumber"] = "+2348000000000"
            })
            .Build());
        services.AddControllersWithViews();
        return services.BuildServiceProvider();
    }

    private static PublicAppointmentRequestsController CreatePublicController(
        ApplicationDbContext context,
        IServiceProvider services,
        INotificationService notifications,
        IHubContext<BookingHub> hub)
    {
        var controller = new PublicAppointmentRequestsController(
            context,
            new AvailabilityService(context, NullLogger<AvailabilityService>.Instance),
            notifications,
            hub,
            NullLogger<PublicAppointmentRequestsController>.Instance);
        InitializeController(controller, services, isAdmin: false);
        return controller;
    }

    private static AdminAppointmentRequestsController CreateAdminController(
        ApplicationDbContext context,
        IServiceProvider services,
        INotificationService notifications,
        IHubContext<BookingHub> hub)
    {
        var controller = new AdminAppointmentRequestsController(
            context,
            hub,
            notifications,
            new AppointmentRequestMaintenanceService(context),
            NullLogger<AdminAppointmentRequestsController>.Instance);
        InitializeController(controller, services, isAdmin: true);
        return controller;
    }

    private static void InitializeController(Controller controller, IServiceProvider services, bool isAdmin)
    {
        var claims = isAdmin
            ? new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "launch-admin"),
                new Claim(ClaimTypes.Name, "launch-admin@example.test"),
                new Claim(ClaimTypes.Role, "Admin")
            }
            : [];
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services,
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "AppointmentWorkflowTest"))
        };
        controller.ControllerContext = new ControllerContext(
            new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor()));
        controller.TempData = new TempDataDictionary(httpContext, new TestTempDataProvider());
        controller.ObjectValidator = services.GetRequiredService<IObjectModelValidator>();
    }

    private sealed record ClinicalData(int DepartmentId, int DoctorId, DateTime SlotDateTime);

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) =>
            new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }

    private sealed class RecordingNotificationService : INotificationService
    {
        public List<NotificationRequest> ConfirmationRequests { get; } = [];
        public List<NotificationRequest> AdminAlertRequests { get; } = [];
        public List<(NotificationRequest Request, string Status)> StatusUpdates { get; } = [];

        public Task<bool> SendConfirmationAsync(NotificationRequest request)
        {
            ConfirmationRequests.Add(request);
            return Task.FromResult(true);
        }

        public Task<bool> SendAdminAlertAsync(NotificationRequest request)
        {
            AdminAlertRequests.Add(request);
            return Task.FromResult(true);
        }

        public Task<bool> SendAppointmentStatusAsync(NotificationRequest request, string status, string nextStep)
        {
            StatusUpdates.Add((request, status));
            return Task.FromResult(true);
        }

        public Task<bool> SendReminderAsync(NotificationRequest request) => Task.FromResult(true);
        public Task<bool> SendTeleconsultationReceivedAsync(NotificationRequest request) => Task.FromResult(true);
        public Task<bool> SendTeleconsultationStatusAsync(NotificationRequest request, string status, string nextStep) => Task.FromResult(true);
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
