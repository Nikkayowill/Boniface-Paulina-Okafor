# Architecture Quality Audit

Date: 2026-06-24

## Scope

This audit reverse-engineers the current ASP.NET Core MVC hospital management system and identifies architecture, data-flow, scalability, performance, and maintainability risks. The goal is to improve code quality without changing user-facing functionality.

## Current Architecture

The application is a modular MVC monolith:

- `Program.cs` owns startup, DI registration, security headers, EF Core setup, Identity, seeding, SignalR, hosted services, and routes.
- `Data/ApplicationDbContext.cs` owns all EF Core entity configuration for public website, appointments, patient portal, payments, notifications, teleconsultations, and WhatsApp scheduling.
- `Controllers/` exposes public workflows: home/content pages, appointment requests, teleconsultations, bill payments, donations, notifications, webhooks, doctors, and departments.
- `Areas/Admin/` exposes staff/admin workflows for appointments, teleconsultations, users, CMS posts, billing, contacts, availability, patient profiles, and patient appointments.
- `Areas/Patient/` exposes authenticated patient portal workflows.
- `Services/` contains notification providers, payment gateways, scheduling helpers, push notifications, email senders, image lookup, and background jobs.
- `Views/` and `wwwroot/` own Razor UI, CSS, JavaScript, PWA assets, and static media.
- `tests/Okafor.NET.Tests/` covers PWA behavior, responsive/accessibility assertions, smoke routes, notifications, scheduling, and some controller/service behavior.

## Main Data Flows

### Public Appointment Request

1. Visitor opens `/AppointmentRequests/Create`.
2. `AppointmentRequestsController` loads departments/doctors from `ApplicationDbContext`.
3. Form POST validates date, department, and doctor.
4. Controller creates `AppointmentRequest`.
5. SignalR notifies the admin queue.
6. Admin later approves/rejects in `Areas/Admin/Controllers/AppointmentRequestsController`.
7. Approval may reserve an `AppointmentSlot`, create a linked `PatientAppointment`, send notifications, and broadcast status.

### Slot Booking Widget

1. Browser requests available slots for doctor/date.
2. `AvailabilityService` generates slots from `DoctorAvailability` and filters booked `AppointmentSlot` rows.
3. Booking POST creates `AppointmentRequest`.
4. Controller calls `AvailabilityService.ReserveSlotAsync`.
5. Notifications are sent and SignalR broadcasts slot/admin updates.

### Teleconsultation

1. Public visitor submits `/Teleconsultations/Create`.
2. Controller validates department/doctor/date/WhatsApp opt-in.
3. Controller creates `TeleconsultationRequest`.
4. Notification services send patient/admin/WhatsApp messages.
5. SignalR alerts admin queue.
6. Admin updates status from the admin area.

### Payments

1. Donation and bill payment controllers validate form input.
2. Controllers create local pending records.
3. `IPaymentGateway` initializes checkout through mock or Paystack provider.
4. Callback/webhook verifies payment.
5. Shared payment verification mapping updates local payment/donation state.
6. Receipt email is sent if the payment transitions into paid/sandbox-approved.

### Notifications

1. Controllers/services build `NotificationRequest`.
2. `INotificationService` maps to Lean, Africa's Talking, or composite providers.
3. WhatsApp-specific teleconsultation messages use `IWhatsAppNotificationService`.
4. `NotificationLog` records delivery attempts and webhook updates.
5. Background services handle reminders and stale push-subscription cleanup.

### PWA And Offline

1. Layout references manifest, service worker registration, PWA JavaScript, and offline pages.
2. Service worker avoids private/admin/payment/upload/hub routes.
3. Tests assert cache exclusions, notification click behavior, offline appointment storage, and registration cleanup.

## Critical Problem Areas

### 1. Composition Root Is Too Large

`Program.cs` has too many responsibilities: environment loading, security policy, data provider selection, identity, notification selection, payment selection, seeding, routes, SignalR, and hosted services. This makes production behavior hard to reason about and easy to break during small changes.

Recommended refactor:

- Move service registrations into extension methods such as `AddOkaforData`, `AddOkaforPayments`, `AddOkaforNotifications`, `AddOkaforScheduling`, and `UseOkaforSecurityHeaders`.
- Keep `Program.cs` as the readable composition script.

### 2. Business Workflows Live In Controllers

Controllers currently own validation, persistence, notification orchestration, SignalR broadcasts, and status transitions. This is especially visible in appointment approval, public appointment booking, teleconsultation submission, donations, and bill payments.

Risks:

- Hard to test workflows without MVC.
- Duplicate logic appears across public, admin, WhatsApp, and payment flows.
- Future API/React work will duplicate controller logic again unless workflows move behind application services.

Recommended refactor:

- Introduce application services for `AppointmentBookingService`, `AppointmentApprovalService`, `TeleconsultationIntakeService`, and `PaymentProcessingService`.
- Controllers should bind/validate request models, call one application service, then return view/result.

### 3. Appointment Slot Reservation Logic Is Split

Slot behavior exists in:

- `AvailabilityService.ReserveSlotAsync`
- public `AppointmentRequestsController.BookSlot`
- admin `AppointmentRequestsController.TryReserveApprovedSlotAsync`
- `WhatsAppSchedulingSessionService.TryConfirmSelectionAsync`
- `WhatsAppAppointmentSlotService.FindAvailableSlotsAsync`

Risks:

- Race-condition fixes must be copied to several places.
- Admin-approved appointments and patient-booked appointments may diverge.
- Date/time formatting and parsing rules differ by flow.

Recommended refactor:

- Create one `IAppointmentSlotReservationService`.
- Make all flows call the same reservation primitive.
- Keep a unique database index on `{ DoctorId, SlotDateTime }`.
- Catch `DbUpdateException` for race-condition collisions and return a domain result.

### 4. EF Core Model Configuration Is Centralized In One File

`ApplicationDbContext.OnModelCreating` configures every entity in a single method. It is manageable now, but it will become a merge-conflict and review burden as the system grows.

Recommended refactor:

- Move each aggregate into `IEntityTypeConfiguration<T>` classes under `Data/Configurations`.
- Apply with `builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);`.

### 5. Query Scalability Risks

Examples:

- Admin dashboard performs many separate count and recent-activity queries.
- Admin appointment index loads every appointment request with department and doctor.
- Search uses `Contains` against multiple text columns without paging or full-text indexing.
- Doctor/profile/posts/admin lists are mostly unpaged.
- `DateTime.Date` inside EF predicates can prevent efficient index usage depending on provider translation.

Recommended refactor:

- Add paging to admin/public list pages.
- Project directly to list view models instead of loading full entities.
- Use date range predicates instead of `.Date` comparisons.
- Add indexes around common dashboard filters and status/date queries.
- Consider SQL full-text search or a dedicated search table if public search grows.

### 6. Provider Configuration Is Stringly Typed

Payments and notifications select providers through raw configuration strings. This is workable, but provider-specific failures are easy to miss at startup.

Recommended refactor:

- Bind options classes such as `PaymentOptions`, `PaystackOptions`, `NotificationOptions`, and `WhatsAppOptions`.
- Validate required options on startup for active providers.
- Keep provider selection in one registration extension.

### 7. External Side Effects Are Intermixed With Transactions

Some workflows save state, send notifications, save again, then broadcast through SignalR. This is pragmatic, but not durable enough for production if traffic grows.

Recommended refactor:

- Persist domain state first.
- Record outbound notification jobs in an outbox table.
- Let a background worker send email/SMS/WhatsApp/push messages.
- Make notification delivery retryable and idempotent.

### 8. Security And Operational Hardening Needs A Pass

Good existing pieces:

- Identity is configured with reasonable password/lockout settings.
- Antiforgery is globally enabled for MVC.
- Security headers exist.
- Private/admin routes are excluded from service-worker caching.
- CODEOWNERS and branch protection are now in place.

Risks:

- CSP still allows `'unsafe-inline'` for scripts/styles.
- Paystack webhook controller depended on concrete gateway registration that was not guaranteed.
- Uploaded patient documents need ongoing review for file type, size, malware scanning, authorization checks, and private storage.
- Some provider secrets are read directly from configuration throughout services.

## Code Quality Upgrade Completed

Implemented behavior-preserving cleanup:

- Added `Services/PaymentVerificationApplicator.cs`.
- Removed duplicated payment verification mapping from donation and bill payment controllers.
- Removed controller-to-controller coupling from `PaystackWebhooksController`.
- Registered `PaystackPaymentGateway` as a typed HTTP client so webhook activation can resolve the concrete gateway consistently.
- Added focused tests in `PaymentVerificationApplicatorTests`.

Why this matters:

- Payment verification rules now live in one place.
- Webhooks no longer call static methods on MVC controllers.
- Paystack webhook DI is production-safe even when `IPaymentGateway` is selected conditionally.

## Recommended Refactor Sequence

### Phase 1: Composition And Boundaries

- Extract service registration extension methods from `Program.cs`.
- Add typed options and validation for payments, notifications, WhatsApp, SMTP, and push.
- Move EF configurations into `Data/Configurations`.

### Phase 2: Core Workflow Services

- Introduce appointment booking and approval services.
- Centralize slot reservation.
- Introduce teleconsultation intake/update services.
- Move payment initialization/callback/webhook handling into a payment application service.

### Phase 3: Scalability And Reliability

- Add paging and projections to admin/public list pages.
- Replace dashboard count scatter with a query service.
- Add outbox-backed notifications.
- Add idempotency keys to external webhook processing.
- Add explicit query indexes for common status/date/admin filters.

### Phase 4: Tests

- Add integration tests for public appointment submission.
- Add integration tests for teleconsultation submission.
- Add payment callback/webhook tests for both bill payment and donation flows.
- Add authorization tests for patient/admin routes.
- Add controller-thin/application-service tests as workflows move out of MVC.

## Production-Grade Code Direction

Controller shape after refactoring should trend toward:

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Index(BillPaymentViewModel model, CancellationToken cancellationToken)
{
    if (!ModelState.IsValid)
        return View(model);

    var result = await _paymentProcessing.StartBillPaymentAsync(model, User, Request.Scheme, cancellationToken);

    if (!result.Success)
    {
        ModelState.AddModelError(string.Empty, result.Message);
        return View(model);
    }

    return result.RequiresRedirect
        ? Redirect(result.RedirectUrl)
        : RedirectToAction(nameof(Receipt), new { result.Id, result.Reference });
}
```

Service shape should own transaction, persistence, provider calls, and domain result mapping:

```csharp
public interface IPaymentProcessingService
{
    Task<PaymentStartResult> StartBillPaymentAsync(
        BillPaymentViewModel model,
        ClaimsPrincipal user,
        string requestScheme,
        CancellationToken cancellationToken);

    Task<PaymentWebhookResult> ApplyPaystackVerificationAsync(
        string reference,
        PaymentVerificationResult verification,
        CancellationToken cancellationToken);
}
```

This preserves MVC while making business workflows reusable from Razor, APIs, background jobs, or future React-backed endpoints.

## Verification

Baseline non-smoke tests initially passed after running outside the sandbox because VSTest needs local socket permissions.

After the payment cleanup:

- `dotnet test tests/Okafor.NET.Tests/Okafor.NET.Tests.csproj --filter "Category!=Smoke"`
- Result: 172 passed, 0 failed.

