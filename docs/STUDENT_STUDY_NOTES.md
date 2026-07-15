# Student Study Notes and Project Talking Points

Last updated: July 13, 2026

This is the learning record for launch work. Every substantial feature slice should add or revise a row so the implementation, reasoning, and interview explanation stay connected.

## How to Maintain This File

For each feature slice, record:

1. The user or operational problem.
2. The implementation concept.
3. The safety or reliability reason.
4. A plain-language explanation suitable for a non-technical stakeholder.
5. One fact that can be verified in the code or a demo.

## Concepts Implemented

| Feature slice | Engineering concept | Student explanation | Evidence in the project |
|---|---|---|---|
| Mobile-first public redesign | Progressive enhancement and responsive hierarchy | Design the smallest screen first, then add space and layout columns when the viewport can support them. The important care actions remain available without depending on a desktop layout. | `Views/Home/Index.cshtml`, `wwwroot/css/public-site.css` |
| Hospital image carousel | Accessible stateful UI | The carousel has one source of state, pauses on request, supports touch and buttons, and respects reduced-motion preferences. This avoids duplicate cards and animation that users cannot control. | `wwwroot/js/hero-carousel.js` |
| Scoped site search | Bounded server-side search | Search checks approved content groups, normalizes the query, and limits results. Bounded work protects response time and makes categories understandable to users. | `Controllers/HomeController.cs`, `Views/Home/Search.cshtml` |
| Appointment booking | Dependent selection and stale-state prevention | Selecting a department filters doctors locally. Changing an earlier choice clears later dates and slots so an old selection cannot be submitted accidentally. | `wwwroot/js/booking-widget.js`, `Controllers/AppointmentRequestsController.cs` |
| Patient portal authorization | Ownership-based access control | A Patient role opens the portal, but every sensitive record query also proves that the record belongs to the signed-in user's profile. Role authorization answers “may this type of user enter?”; ownership answers “may this user see this record?” | `Areas/Patient/Controllers/PatientBaseController.cs` and patient controllers |
| Private patient documents | Defense-in-depth file handling | Files are stored outside the public web root, checked by size, extension, MIME type, and file signature, and downloaded only through an authorized controller. Legacy public document paths are blocked. | `Services/PatientDocumentStorageService.cs`, `Program.cs` |
| Private response caching | Cache-control and PWA boundaries | Medical, identity, admin, billing, and receipt responses must not be saved in shared browser or service-worker caches. Public shell assets can be cached; private routes stay network-only. | `Program.cs`, `wwwroot/service-worker.js` |
| Payment initialization | Recoverable transaction reference | The local record and provider reference are saved before the browser leaves for checkout. If the response is interrupted, a signed webhook can still match the provider transaction to the local record. | `Controllers/BillPaymentsController.cs`, `Controllers/PaystackWebhooksController.cs` |
| Payment verification | Server-side trust boundary | A browser redirect is not proof of payment. Production payments become paid only after server verification succeeds and provider amount and currency match the expected local values. | `Services/PaymentVerificationApplicator.cs` |
| Duplicate invoice protection | Idempotency and state-aware retry | Paid and pending invoices reject another attempt. Failed or cancelled attempts can be retried with a new provider reference. This reduces double charges without permanently locking a recoverable invoice. | `Controllers/BillPaymentsController.cs` |
| Donation operations | Operational completeness | A public transaction is not complete unless staff can review it. Donations now have checkout recovery, truthful receipt states, an admin queue, filters, and dashboard visibility. | `Controllers/DonationController.cs`, `Areas/Admin/Controllers/DonationsController.cs` |
| Appointment calendar export | Domain timezone correctness | Hospital appointment times are expressed with the `Africa/Lagos` timezone instead of converting through the server's timezone. Only approved or scheduled future appointments can create reminders or calendar events. | `Areas/Patient/Controllers/AppointmentsController.cs` |
| Offline appointment summaries | Encrypted device storage | A small appointment summary can be encrypted in IndexedDB for unreliable connections. Notes, messages, documents, billing, and medical records are excluded, and logout clears keys, records, and app caches. | `wwwroot/js/encrypted-offline-store.js`, `wwwroot/js/pwa-appointments.js` |
| Email confirmation | Fail-fast integration design | SMTP errors are returned to the caller instead of being logged as success. Production startup refuses a configuration that requires email confirmation but has no real SMTP sender, and patients can safely request another confirmation email. | `Services/SmtpEmailSender.cs`, `Program.cs`, `Areas/Identity/Pages/Account/ResendEmailConfirmation.*` |
| Hosting health signals | Liveness and readiness separation | The host can distinguish a running web process from an application that can actually reach SQL Server. This prevents traffic from being routed to an instance that is alive but unable to serve database workflows. | `Services/SqlServerHealthCheck.cs`, `Program.cs` |
| Restart-safe authentication | Persistent Data Protection keys | Authentication and antiforgery data are encrypted with a key ring. A configured persistent key path prevents normal container revisions from invalidating every user's cookie. | `Program.cs`, `Dockerfile` |
| Repeatable deployment | Multi-stage, non-root container build | The SDK image compiles the app, the smaller runtime image serves it, and the final process does not run as root. The same artifact can move between staging and production configuration. | `Dockerfile`, `.dockerignore` |
| Controlled schema release | Explicit migration command | Production schema changes run as a deliberate release step with `--migrate-db`, rather than every replica automatically attempting migrations during startup. | `Program.cs`, `DEPLOYMENT.md` |
| Real SQL Server testing | Test fidelity and isolated state | SQL Server containers exercise the same constraints and SQL behavior as production. Respawn clears business data quickly while preserving the migrated schema. | `docs/SQLSERVER_INTEGRATION_TESTING_ROADMAP.md` |
| Patient-safe error recovery | Information disclosure prevention | Production error pages never show stack traces, configuration advice, or exception details. They preserve the HTTP status, stay out of search results, offer clear recovery actions, and expose only a support reference that staff can match to logs. | `Program.cs`, `Views/Shared/Error.cshtml`, `Views/Home/HttpStatus.cshtml` |
| Operational privacy notice | Transparency and data minimization | The privacy page explains what the product actually collects, why it is used, who may support processing, how offline storage works, and how patients can raise concerns. It avoids promising absolute security and still requires owner/privacy adviser approval. | `Views/Home/Privacy.cshtml`, `docs/FUNCTIONALITY_LOOP_BOARD.md` |
| Hosted job controls | Process lifecycle and graceful cancellation | Reminder and cleanup loops can be enabled per environment, pass shutdown signals into database work, and stop cleanly during a deployment. Configuration does not pretend that jobs run when a free host has scaled to zero. | `Services/BackgroundTaskOptions.cs`, `Services/AppointmentReminderService.cs`, `Services/PushSubscriptionCleanupService.cs` |
| Real-browser critical journeys | E2E test pyramid and cross-layer fidelity | Playwright drives the mobile UI through Kestrel while the application uses a migrated SQL Server container. Each scenario gets a clean browser context and Respawn database reset, then verifies both patient-visible outcomes and SQL post-conditions. External providers stay in deterministic safe modes until staging. | `tests/Okafor.NET.E2E`, `docs/E2E_TESTING.md` |
| Browser dependency ordering | Runtime asset dependency | The E2E appointment journey detected that jQuery Validation was loaded before jQuery on public pages. Loading the shared dependency in the public layout prevents uncaught runtime errors across every public form that uses unobtrusive validation. | `Views/Shared/_Layout.cshtml`, `Views/Shared/_ValidationScriptsPartial.cshtml` |
| Provider-specific care pathway | Intentional routing and additive seed data | Father Toochukwu is represented as a real provider so directory, department, and teleconsultation filtering use the existing data model. His profile routes to a preselected reviewed teleconsultation request, while the ordinary appointment form excludes the teleconsultation-only specialty. Additive seeding inserts the new provider into an existing database without duplicating current records. | `Seed/ClinicalDataSeed.cs`, `Controllers/AppointmentRequestsController.cs`, `Views/Home/DoctorProfile.cshtml` |
| Designated program giving | Separation of charitable support and care payment | A donation can be assigned to Father Toochukwu's hospital-managed spiritual care program and the designation travels through checkout, staff review, email, and receipt. The interface states that giving is optional, is not the psychotherapy fee, and cannot influence appointment priority. | `Models/Donation.cs`, `Controllers/DonationController.cs`, `Views/Donation/Index.cshtml`, `Views/Home/DoctorProfile.cshtml` |
| Donation operations view | Query composition and operational visibility | Staff filters are applied to the SQL query before counting and limiting results. This lets staff isolate a program designation, payment state, or donor search while summary cards describe the same result set and sandbox payments remain distinct from confirmed production gifts. | `Areas/Admin/Controllers/DonationsController.cs`, `Areas/Admin/Views/Donations/Index.cshtml` |
| Secret-safe integration readiness | Configuration presence versus connectivity | An admin-only screen checks whether required configuration keys contain non-placeholder values without ever rendering those values. “Configured” means the app has the inputs; staging verification is still required to prove the external provider accepts them and webhooks or deliveries work. | `Areas/Admin/Controllers/IntegrationsController.cs`, `Areas/Admin/Views/Integrations/Index.cshtml`, `ViewModels/IntegrationReadinessViewModel.cs` |
| Development SQL readiness gate | Liveness versus dependency readiness | The verification script first proves SQL Server can answer a query, then applies migrations through the same controlled application path used by deployment. `/health/live` proves the web process runs, while `/health/ready` proves it can also reach SQL Server. | `scripts/verify-development-sql.sh`, `Services/SqlServerHealthCheck.cs`, `Program.cs` |
| Quiet-by-default provider diagnostics | Operational data minimization | External SDK debug logging is an explicit configuration choice instead of automatically turning on in Development. Routine logs stay useful without unnecessarily printing provider configuration details; verbose diagnostics can still be enabled temporarily during controlled troubleshooting. | `Program.cs`, `docs/ENVIRONMENT_VARIABLES.md` |
| CI failure evidence | Ephemeral observability | CI writes structured TRX test results and the temporary app log only when a job fails, then retains them briefly. This gives developers enough evidence to diagnose failures without permanently accumulating potentially sensitive operational logs. | `.github/workflows/ci.yml`, `docs/VERIFICATION_CHECKLIST.md` |
| Role-boundary integration tests | Authentication versus authorization | Authentication proves who a request represents; authorization decides whether that identity's role may enter a route. The tests use a controlled test identity to prove Patient, Staff, and Admin access independently, while anonymous requests still exercise the real login redirect. | `tests/Okafor.NET.Tests/AuthorizationBoundaryIntegrationTests.cs`, `Areas/Patient/Controllers/PatientBaseController.cs`, `Areas/Admin/Controllers/AdminBaseController.cs` |
| Patient profile uniqueness | Application checks versus database invariants | The controller avoids creating a second profile during a normal request, while a unique SQL Server index guarantees one profile per account even when two requests arrive together. The migration consolidates legacy duplicates and preserves their related care records before enabling the constraint. | `Data/ApplicationDbContext.cs`, `Data/Migrations/20260715031307_EnforceUniquePatientProfile.cs`, `PatientProfileWorkflowTests.cs` |
| Medical document content validation | File name versus file structure | An extension and browser-supplied MIME type are only claims. The upload service also checks the file signature; for DOCX it opens the package and requires the standard Word document entries, preventing an unrelated ZIP file renamed `.docx` from passing validation. | `Services/PatientDocumentStorageService.cs`, `PatientDocumentStorageServiceTests.cs` |
| Patient-owned workflow tests | Object-level authorization and state consistency | A role check permits entry to the patient area, but each database query must also limit records to the current patient's profile or verified email. Cancellation changes the portal appointment and linked request together, releases the reserved slot, and sends realtime notification only after SQL Server saves successfully. | `Areas/Patient/Controllers/MessagesController.cs`, `Areas/Patient/Controllers/AppointmentsController.cs`, `PatientMessagingAndCancellationWorkflowTests.cs` |
| Discoverable EF migrations | Schema intent versus executable history | A migration class is not part of deployment merely because its C# file exists. EF discovers it through migration metadata. Real SQL Server workflow tests exposed a donation migration that lacked this metadata, while the model snapshot alone incorrectly suggested the schema was complete. | `Data/Migrations/20260513152000_AddDonationPaymentProviderFields.cs`, `PaymentWorkflowTests.cs`, `SqlServerIntegrationFixture.cs` |

## Core Terms in Plain Language

### Idempotency

An idempotent operation can be repeated without creating an unintended second result. In payments, the invoice and transaction state prevent a double payment when a user taps twice or refreshes.

### Trust Boundary

A trust boundary is the point where untrusted information must be verified before it changes trusted state. The browser saying “payment complete” is untrusted; the server-to-provider verification and signed webhook are trusted evidence.

### Defense in Depth

No single control carries the entire security burden. Patient documents use authorization, ownership filtering, non-public storage, content validation, blocked legacy paths, and no-store response headers.

### Scale to Zero

A serverless host can stop all app instances while nobody is using the site. This saves compute cost, but an in-process reminder service also stops. Time-based clinical reminders therefore need an always-running instance or a separately scheduled job.

### Readiness vs. Liveness

Liveness asks, “Is the web process running?” Readiness asks, “Can it serve real work, including reaching SQL Server?” Hosts use these signals differently during restarts and deployments.

### Safe Error Disclosure

Patients need a useful recovery message, while developers need diagnostic detail. The browser receives a neutral message and support reference; exception details remain in protected server logs where they can be investigated without exposing internals publicly.

### Graceful Cancellation

During a restart or deployment, the host sends a cancellation signal to background work. Passing that signal into delays and database calls lets the process stop deliberately instead of abandoning work unpredictably.

### End-to-End Test

An E2E test uses the product through the same public interface as a user, usually a browser. Here it crosses UI JavaScript, HTTP middleware, controller logic, EF Core, and SQL Server, then checks both what the patient sees and what the database stored.

### Browser Context Isolation

A Playwright browser context acts like a fresh browser profile. New cookies, storage, and cache state prevent one test's login or offline data from leaking into the next test while still allowing the expensive browser process to be shared.

## Interview Questions and Strong Answers

### Why not trust the payment callback in the browser?

The callback is controlled by a user-facing browser and can be replayed or manipulated. I use it only as a trigger to call the payment provider from the server. The record changes to Paid only when verification succeeds and the returned amount and currency match the original transaction.

### Why store patient documents outside `wwwroot`?

Anything under `wwwroot` can be served by static-file middleware without executing controller authorization. Private documents belong in non-public storage and must be streamed through an endpoint that verifies both role and record ownership.

### What makes the PWA appropriate for a hospital with mobile connectivity constraints?

The public shell and static assets are cacheable, while sensitive routes are explicitly network-only. The only offline patient feature is an intentionally small encrypted appointment summary. Logout deletes the encryption seed, IndexedDB records, legacy local storage, and app caches.

### What did you do to prevent configuration from failing silently?

I changed SMTP delivery to propagate failures, added startup validation when confirmed accounts require email, and documented required environment variables. A broken dependency should be visible before launch rather than appear as a successful patient workflow.

### Why are appointment reminders not guaranteed on scale-to-zero hosting?

The reminder loop runs inside the ASP.NET process. When the host scales to zero, there is no process or timer executing. I made the job configurable and cancellation-safe, documented the constraint, and would use always-on compute or an external scheduled worker when delivery timing becomes a launch requirement.

### Why use both Playwright and SQL Server Testcontainers for E2E?

Playwright proves the real responsive UI and JavaScript behavior, while Testcontainers proves the workflow against the same database engine used in production. The appointment scenario also checks SQL after the browser succeeds, so a cosmetic success screen cannot hide a persistence failure.

## LinkedIn Talking Points

- Built a mobile-first hospital PWA for a Nigerian care context using ASP.NET Core, EF Core, SQL Server, Razor, Tailwind, and Bootstrap portal surfaces.
- Hardened appointment, teleconsultation, patient document, messaging, bill payment, donation, and receipt workflows from both patient and staff perspectives.
- Applied ownership-based authorization, non-public medical document storage, signed payment webhooks, amount/currency verification, and private-route cache exclusions.
- Designed constrained-connectivity support with a cacheable public app shell and encrypted, minimal offline appointment summaries.
- Improved operational readiness with admin work queues, truthful transaction states, SMTP fail-fast behavior, health checks, and documented deployment tradeoffs.
- Kept SQL Server as the single confirmed provider and designed high-fidelity integration testing around SQL Server containers rather than an in-memory substitute.

## Demo Story

1. A patient finds a department or doctor through scoped search.
2. The patient books a valid available slot on a phone.
3. Staff reviews the request and updates its status.
4. The patient sees the status, exports the confirmed time in Lagos time, and optionally saves a minimal encrypted offline summary.
5. The patient securely uploads a document that is never publicly served.
6. Billing or donations use recoverable provider references and server-side verification before a receipt is considered valid.
7. Admin queues show the work requiring staff attention.
