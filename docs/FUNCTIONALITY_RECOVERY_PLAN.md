# Functionality Recovery Plan

The goal is to restore and protect backend behavior after moving development from Windows to Linux. Visual redesign work can continue separately, but the application should remain provable: it builds, runs, stores data, sends the right requests, and keeps patient/admin workflows intact.

## Recovery Principles

- The backend is the system of record for appointments, teleconsultations, payments, patient documents, admin actions, and provider integrations.
- Public UI can be redesigned, including a future React frontend, as long as secure writes still go through backend routes or documented APIs.
- Local development must work on Linux and Windows with documented prerequisites.
- Features should move from `Code-present` to `Verified` only after a repeatable test or checklist confirms them.
- Live integrations must be tested with provider sandbox credentials before production credentials are used.

## Phase 1: Local Platform Stability

Status: mostly in place.

Required outcomes:

- .NET SDK can restore, build, and run the project on Linux and Windows.
- SQL Server is available locally through Docker on Linux or Docker/LocalDB/SQL Server on Windows.
- Development startup applies EF Core migrations and seed data.
- `dotnet watch` works on Linux through polling mode when inotify limits are too low.
- Tailwind CSS can be rebuilt with npm.

Evidence and commands:

```bash
dotnet build Okafor-.NET.csproj
dotnet build tests/Okafor.NET.Tests/Okafor.NET.Tests.csproj
dotnet test tests/Okafor.NET.Tests/Okafor.NET.Tests.csproj --filter "Category!=Smoke"
npm install
npm run build:css
```

Linux helper:

```bash
./scripts/dev-watch.sh
```

## Phase 2: Database And Identity

Status: next priority.

Required outcomes:

- SQL Server container or local SQL instance starts reliably.
- `ConnectionStrings:DefaultConnection` points to the correct local server.
- Migrations apply cleanly.
- Seed data creates roles, clinical departments, doctors, news posts, sample appointment data, and the configured admin account.
- Admin, Staff, and Patient authorization boundaries behave correctly.

Verification:

- Start the app in `Development`.
- Confirm `/health` returns `200`.
- Sign in as the seeded admin.
- Visit `/Admin/Dashboard`.
- Confirm public doctors/news pages show seeded content.

Risk:

- Seed admin credentials must not be committed. Use user secrets or local environment variables.

## Phase 3: Core Hospital Workflows

Status: code exists; manual SQL-backed verification required.

Verify in this order:

1. Public appointment request.
2. Admin appointment review and approval.
3. Doctor availability and slot generation.
4. Patient registration/login.
5. Patient profile creation.
6. Patient appointment list and cancellation.
7. Patient document upload/delete.
8. Patient message send/list.
9. Teleconsultation request.
10. Admin teleconsultation status update.

Completion rule:

- A workflow is considered recovered when it works against SQL Server in `Development`, survives an app restart, and has either an automated test or a documented manual checklist entry.

## Phase 4: Payments

Status: mock provider should be verified first; Paystack requires credentials.

Required outcomes:

- `Payments:Provider=Mock` works locally without external calls.
- Donation form creates a payment record and receipt.
- Bill payment form creates a payment record and receipt.
- Paystack configuration is isolated in user secrets/environment variables.
- Paystack webhook verification is tested with signed sandbox requests before production.

Completion rule:

- Mock flows are required for local development.
- Paystack flows are required before launch.

## Phase 5: Notifications And WhatsApp

Status: code exists; provider-specific live behavior depends on credentials.

Required outcomes:

- Email failure path is safe and logged.
- SMS provider can be enabled only when Africa's Talking credentials are configured.
- WhatsApp click-to-chat widget is visible on public pages.
- WhatsApp Cloud API outbound templates work with sandbox/live credentials.
- WhatsApp webhook verification and inbound scheduling flow are tested.
- Push notification subscription save/delete/test works in a browser.

Completion rule:

- Local development can use `Notifications:Provider=Lean`.
- Production-like testing must use real sandbox credentials for WhatsApp, SMS, email, Paystack, and VAPID.

## Phase 6: PWA And Offline Behavior

Status: automated coverage exists; browser verification remains important.

Required outcomes:

- Manifest, icons, service worker, offline page, and app shell load.
- Public routes cache safely.
- Patient/admin/private routes are not cached unsafely.
- Offline appointment flow stores pending data client-side and syncs when online.
- Install prompt does not overlap the WhatsApp widget.

Verification:

- Run automated non-smoke tests.
- Use browser devtools to simulate offline mode.
- Confirm offline fallback pages render.

## Phase 7: Collaboration Readiness

Status: in place, but branch protection must be configured on GitHub.

Required outcomes:

- Teammate can clone, install prerequisites, build, and run.
- Husky blocks direct local pushes to `main`/`master`.
- GitHub branch protection requires pull requests and passing CI.
- Frontend/backend boundaries are documented.
- New features update `docs/FEATURE_INVENTORY.md` and `docs/VERIFICATION_CHECKLIST.md`.

## Definition Of Done For "Back To Full Functionality"

The app is considered functionally restored when:

- Project-level build passes on Linux and Windows.
- Non-smoke automated tests pass.
- Smoke tests pass against a running local app.
- SQL Server development run applies migrations and seed data.
- Admin can sign in and perform core management tasks.
- Public appointment, teleconsultation, donation, and bill payment flows work locally.
- Patient portal profile, documents, appointments, and messages work locally.
- PWA/offline behavior is checked in a browser.
- External integrations have clear local fallback behavior and sandbox verification notes.
