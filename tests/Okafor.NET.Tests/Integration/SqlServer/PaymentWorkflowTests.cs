using System.Text;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Okafor_.NET.Controllers;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.Services;
using Okafor_.NET.ViewModels;

namespace Okafor_.NET.Tests.Integration.SqlServer;

[Collection(SqlServerIntegrationCollection.Name)]
[Trait("Category", "DatabaseIntegration")]
public sealed class PaymentWorkflowTests : SqlServerIntegrationTestBase
{
    public PaymentWorkflowTests(SqlServerIntegrationFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task DonationCheckout_MockProviderRecordsSandboxDonationAndReceipt()
    {
        await using var context = Fixture.CreateDbContext();
        var receipts = new RecordingDonationReceiptSender();
        var controller = CreateDonationController(context, receipts);

        var result = await controller.Index(new DonationCheckoutViewModel
        {
            DonorName = "  Ada Donor  ",
            DonorEmail = "  ada.donor@example.test  ",
            DonorPhone = "  +2348000000001  ",
            Amount = 25000m,
            Currency = " ngn ",
            PurposeCode = DonationPurposeCodes.GeneralHospitalSupport,
            DonorMessage = "  Please use this where it is needed most.  "
        });

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Receipt");
        context.ChangeTracker.Clear();
        var donation = await context.Donations.AsNoTracking().SingleAsync();
        donation.DonorName.Should().Be("Ada Donor");
        donation.DonorEmail.Should().Be("ada.donor@example.test");
        donation.DonorPhone.Should().Be("+2348000000001");
        donation.Currency.Should().Be("NGN");
        donation.PreferredMethod.Should().Be(DonationMethodCodes.OnlineCheckout);
        donation.DonorMessage.Should().Be("Please use this where it is needed most.");
        donation.ContactConsent.Should().BeFalse();
        donation.Status.Should().Be(DonationStatus.SandboxApproved);
        donation.Provider.Should().Be("Mock");
        donation.ProviderReference.Should().StartWith("SANDBOX-DON-");
        donation.IsSandbox.Should().BeTrue();
        donation.PaidAt.Should().NotBeNull();
        receipts.DonationIds.Should().ContainSingle().Which.Should().Be(donation.Id);
    }

    [Fact]
    public async Task DonationCheckout_RequiresEmail()
    {
        await using var context = Fixture.CreateDbContext();
        var controller = CreateDonationController(context, new RecordingDonationReceiptSender());

        var result = await controller.Index(new DonationCheckoutViewModel
        {
            DonorName = "Test Donor",
            Amount = 1000m,
            Currency = "NGN",
            PurposeCode = DonationPurposeCodes.GeneralHospitalSupport,
            DonorEmail = string.Empty
        });

        result.Should().BeOfType<ViewResult>();
        controller.ModelState[nameof(DonationCheckoutViewModel.DonorEmail)]!.Errors.Should().ContainSingle();
        (await context.Donations.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task DonationCheckout_LiveProviderRedirectsToHostedCheckoutAndStaysPending()
    {
        await using var context = Fixture.CreateDbContext();
        var receipts = new RecordingDonationReceiptSender();
        var controller = CreateDonationController(
            context,
            receipts,
            new RedirectPaymentGateway("https://checkout.paystack.com/access-code"));

        var result = await controller.Index(new DonationCheckoutViewModel
        {
            DonorName = "Ada Donor",
            DonorEmail = "ada@example.test",
            Amount = 25000m,
            Currency = "NGN",
            PurposeCode = DonationPurposeCodes.GeneralHospitalSupport
        });

        result.Should().BeOfType<RedirectResult>()
            .Which.Url.Should().Be("https://checkout.paystack.com/access-code");
        context.ChangeTracker.Clear();
        var donation = await context.Donations.AsNoTracking().SingleAsync();
        donation.Status.Should().Be(DonationStatus.Pending);
        donation.Provider.Should().Be("Paystack");
        donation.ProviderReference.Should().Be(donation.PaymentReference);
        donation.PaidAt.Should().BeNull();
        receipts.DonationIds.Should().BeEmpty();
    }

    [Fact]
    public async Task DonationCheckout_RejectsNonPaystackRedirectWithoutMarkingPaid()
    {
        await using var context = Fixture.CreateDbContext();
        var controller = CreateDonationController(
            context,
            new RecordingDonationReceiptSender(),
            new RedirectPaymentGateway("https://attacker.example/checkout"));

        var result = await controller.Index(new DonationCheckoutViewModel
        {
            DonorName = "Ada Donor",
            DonorEmail = "ada@example.test",
            Amount = 25000m,
            Currency = "NGN",
            PurposeCode = DonationPurposeCodes.GeneralHospitalSupport
        });

        result.Should().BeOfType<ViewResult>();
        context.ChangeTracker.Clear();
        var donation = await context.Donations.AsNoTracking().SingleAsync();
        donation.Status.Should().Be(DonationStatus.Failed);
        donation.PaidAt.Should().BeNull();
    }

    [Fact]
    public async Task DonationCheckout_RejectsUnexpectedLiveProviderReference()
    {
        await using var context = Fixture.CreateDbContext();
        var controller = CreateDonationController(
            context,
            new RecordingDonationReceiptSender(),
            new RedirectPaymentGateway(
                "https://checkout.paystack.com/access-code",
                "DON-DIFFERENT-REFERENCE"));

        var result = await controller.Index(new DonationCheckoutViewModel
        {
            DonorName = "Ada Donor",
            DonorEmail = "ada@example.test",
            Amount = 25000m,
            Currency = "NGN",
            PurposeCode = DonationPurposeCodes.GeneralHospitalSupport
        });

        result.Should().BeOfType<ViewResult>();
        context.ChangeTracker.Clear();
        var donation = await context.Donations.AsNoTracking().SingleAsync();
        donation.Status.Should().Be(DonationStatus.Failed);
        donation.PaidAt.Should().BeNull();
    }

    [Fact]
    public async Task DonationInterest_AdminCanRecordContactAndConfirmFundsReceived()
    {
        await using var context = Fixture.CreateDbContext();
        var donation = new Donation
        {
            DonorName = "Follow-up Donor",
            DonorEmail = "followup@example.test",
            Amount = 7500m,
            Currency = "NGN",
            PurposeCode = DonationPurposeCodes.GeneralHospitalSupport,
            PreferredMethod = DonationMethodCodes.HospitalContact,
            ContactConsent = true,
            PaymentReference = "DON-FOLLOWUP-ABC123",
            Status = DonationStatus.Pending,
            Provider = "Manual",
            Channel = "HospitalFollowUp",
            IsSandbox = false
        };
        context.Donations.Add(donation);
        await context.SaveChangesAsync();
        var controller = new Okafor_.NET.Areas.Admin.Controllers.DonationsController(context);
        InitializeController(controller);
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "staff-user-1")],
            "Test"));

        var contactedResult = await controller.UpdateStatus(
            donation.Id,
            DonationStatus.Contacted,
            "Called donor and confirmed preferred method.");
        var paidResult = await controller.UpdateStatus(
            donation.Id,
            DonationStatus.Paid,
            "Bank transfer confirmed by hospital staff.");

        contactedResult.Should().BeOfType<RedirectToActionResult>();
        paidResult.Should().BeOfType<RedirectToActionResult>();
        context.ChangeTracker.Clear();
        var saved = await context.Donations.AsNoTracking().SingleAsync();
        saved.Status.Should().Be(DonationStatus.Paid);
        saved.PaidAt.Should().NotBeNull();
        saved.ReviewedAt.Should().NotBeNull();
        saved.ReviewedByUserId.Should().Be("staff-user-1");
        saved.StaffNotes.Should().Be("Bank transfer confirmed by hospital staff.");
    }

    [Fact]
    public async Task DonationReceipt_RequiresMatchingPrivateReference()
    {
        await using var context = Fixture.CreateDbContext();
        var donation = new Donation
        {
            DonorName = "Receipt Donor",
            DonorEmail = "receipt.donor@example.test",
            Amount = 5000m,
            Currency = "NGN",
            PurposeCode = DonationPurposeCodes.GeneralHospitalSupport,
            PaymentReference = "DON-RECEIPT-ABC123",
            ProviderReference = "SANDBOX-DON-RECEIPT-ABC123",
            Status = DonationStatus.SandboxApproved,
            Provider = "Mock",
            IsSandbox = true
        };
        context.Donations.Add(donation);
        await context.SaveChangesAsync();
        var controller = CreateDonationController(context, new RecordingDonationReceiptSender());

        var wrongReference = await controller.Receipt(donation.Id, "DON-WRONG-ABC123");
        var correctReference = await controller.Receipt(donation.Id, donation.PaymentReference);

        wrongReference.Should().BeOfType<NotFoundResult>();
        correctReference.Should().BeOfType<ViewResult>()
            .Which.Model.Should().BeOfType<Donation>()
            .Which.Id.Should().Be(donation.Id);
    }

    [Fact]
    public async Task BillPayment_MockCheckout_PersistsSandboxApprovalAndSendsReceipt()
    {
        await using var context = Fixture.CreateDbContext();
        var receipts = new RecordingBillReceiptSender();
        var controller = CreateBillController(context, receipts);

        var result = await controller.Index(new BillPaymentViewModel
        {
            InvoiceNumber = "  inv-1001  ",
            PatientName = "  Chidi Patient  ",
            PatientEmail = "  chidi@example.test  ",
            PatientPhone = "  +2348000000000  ",
            Amount = 15000m,
            Currency = " ngn ",
            SandboxAcknowledged = true
        });

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Receipt");
        context.ChangeTracker.Clear();
        var payment = await context.BillPayments.AsNoTracking().SingleAsync();
        payment.InvoiceNumber.Should().Be("INV-1001");
        payment.PatientName.Should().Be("Chidi Patient");
        payment.Currency.Should().Be("NGN");
        payment.Status.Should().Be(BillPaymentStatus.SandboxApproved);
        payment.Provider.Should().Be("Mock");
        payment.ProviderReference.Should().StartWith("SANDBOX-BILL-");
        payment.IsSandbox.Should().BeTrue();
        payment.PaidAt.Should().NotBeNull();
        receipts.PaymentIds.Should().ContainSingle().Which.Should().Be(payment.Id);
    }

    [Fact]
    public async Task BillPayment_MockCheckout_RequiresExplicitSandboxAcknowledgement()
    {
        await using var context = Fixture.CreateDbContext();
        var controller = CreateBillController(context, new RecordingBillReceiptSender());

        var result = await controller.Index(new BillPaymentViewModel
        {
            InvoiceNumber = "INV-1002",
            PatientName = "Test Patient",
            PatientEmail = "patient@example.test",
            PatientPhone = "+2348000000000",
            Amount = 5000m,
            Currency = "NGN",
            SandboxAcknowledged = false
        });

        result.Should().BeOfType<ViewResult>();
        controller.ModelState[nameof(BillPaymentViewModel.SandboxAcknowledged)]!.Errors
            .Should().ContainSingle();
        (await context.BillPayments.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task BillReceipt_RequiresGeneratedProviderReference_NotInvoiceNumber()
    {
        await using var context = Fixture.CreateDbContext();
        var payment = new BillPayment
        {
            InvoiceNumber = "INV-RECEIPT",
            PatientName = "Receipt Patient",
            PatientEmail = "receipt.patient@example.test",
            PatientPhone = "+2348000000000",
            Amount = 5000m,
            Currency = "NGN",
            Status = BillPaymentStatus.SandboxApproved,
            Provider = "Mock",
            ProviderReference = "SANDBOX-BILL-ABC123",
            IsSandbox = true
        };
        context.BillPayments.Add(payment);
        await context.SaveChangesAsync();
        var controller = CreateBillController(context, new RecordingBillReceiptSender());

        var invoiceResult = await controller.Receipt(payment.Id, payment.InvoiceNumber);
        var providerResult = await controller.Receipt(payment.Id, payment.ProviderReference);

        invoiceResult.Should().BeOfType<NotFoundResult>();
        providerResult.Should().BeOfType<ViewResult>()
            .Which.Model.Should().BeOfType<BillPayment>()
            .Which.Id.Should().Be(payment.Id);
    }

    [Fact]
    public async Task PaystackWebhook_InvalidSignature_ReturnsUnauthorizedWithoutChangingPayment()
    {
        await using var context = Fixture.CreateDbContext();
        var payment = new BillPayment
        {
            InvoiceNumber = "INV-WEBHOOK",
            PatientName = "Webhook Patient",
            PatientEmail = "webhook.patient@example.test",
            PatientPhone = "+2348000000000",
            Amount = 5000m,
            Currency = "NGN",
            Status = BillPaymentStatus.Pending,
            Provider = "Paystack",
            ProviderReference = "PAYSTACK-WEBHOOK-ABC123",
            IsSandbox = true
        };
        context.BillPayments.Add(payment);
        await context.SaveChangesAsync();
        var billReceipts = new RecordingBillReceiptSender();
        var donationReceipts = new RecordingDonationReceiptSender();
        var configuration = BuildConfiguration();
        var controller = new PaystackWebhooksController(
            context,
            new PaystackPaymentGateway(new HttpClient(), configuration),
            billReceipts,
            donationReceipts,
            NullLogger<PaystackWebhooksController>.Instance);
        var body = $"{{\"event\":\"charge.success\",\"data\":{{\"reference\":\"{payment.ProviderReference}\"}}}}";
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        httpContext.Request.Headers["x-paystack-signature"] = "invalid-signature";
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.Receive(CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
        context.ChangeTracker.Clear();
        (await context.BillPayments.SingleAsync()).Status.Should().Be(BillPaymentStatus.Pending);
        billReceipts.PaymentIds.Should().BeEmpty();
        donationReceipts.DonationIds.Should().BeEmpty();
    }

    private static DonationController CreateDonationController(
        ApplicationDbContext context,
        IDonationReceiptEmailSender receipts,
        IPaymentGateway? gateway = null)
    {
        var controller = new DonationController(
            context,
            receipts,
            gateway ?? new MockPaymentGateway(BuildConfiguration()),
            NullLogger<DonationController>.Instance);
        InitializeController(controller);
        return controller;
    }

    private static BillPaymentsController CreateBillController(
        ApplicationDbContext context,
        IBillPaymentReceiptEmailSender receipts)
    {
        var controller = new BillPaymentsController(
            context,
            new MockPaymentGateway(BuildConfiguration()),
            receipts,
            null!,
            NullLogger<BillPaymentsController>.Instance);
        InitializeController(controller);
        return controller;
    }

    private static void InitializeController(Controller controller)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("hospital.example.test");
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ControllerActionDescriptor());
        controller.ControllerContext = new ControllerContext(actionContext);
        controller.Url = new TestUrlHelper(actionContext);
    }

    private static IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Payments:Mock:ReferencePrefix"] = "SANDBOX",
                ["Payments:Paystack:SecretKey"] = "sk_test_webhook-secret",
                ["Payments:Paystack:BaseUrl"] = "https://api.paystack.co"
            })
            .Build();

    private sealed class RecordingDonationReceiptSender : IDonationReceiptEmailSender
    {
        public List<int> DonationIds { get; } = [];

        public Task SendReceiptAsync(Donation donation, CancellationToken cancellationToken = default)
        {
            DonationIds.Add(donation.Id);
            return Task.CompletedTask;
        }
    }

    private sealed class RedirectPaymentGateway(
        string authorizationUrl,
        string? providerReference = null) : IPaymentGateway
    {
        public string ProviderName => "Paystack";
        public bool IsSandbox => false;

        public Task<PaymentInitializeResult> InitializeAsync(
            PaymentInitializeRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new PaymentInitializeResult(
                Success: true,
                Provider: ProviderName,
                ProviderReference: providerReference ?? request.Reference,
                Channel: "HostedCheckout",
                Message: "Checkout initialized.",
                IsSandbox: false,
                RequiresRedirect: true,
                AuthorizationUrl: authorizationUrl));

        public Task<PaymentVerificationResult> VerifyAsync(
            string reference,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingBillReceiptSender : IBillPaymentReceiptEmailSender
    {
        public List<int> PaymentIds { get; } = [];

        public Task SendReceiptAsync(BillPayment payment, CancellationToken cancellationToken = default)
        {
            PaymentIds.Add(payment.Id);
            return Task.CompletedTask;
        }
    }

    private sealed class TestUrlHelper(ActionContext actionContext) : IUrlHelper
    {
        public ActionContext ActionContext { get; } = actionContext;

        public string? Action(UrlActionContext actionContext) =>
            $"https://hospital.example.test/{actionContext.Action}";

        public string Content(string? contentPath) => contentPath ?? string.Empty;

        public bool IsLocalUrl(string? url) => true;

        public string? Link(string? routeName, object? values) =>
            "https://hospital.example.test/payment";

        public string? RouteUrl(UrlRouteContext routeContext) =>
            "https://hospital.example.test/payment";
    }
}
