# Verification Checklist

Use this checklist when restoring functionality, reviewing a pull request, or preparing for a launch test. Mark an item complete only when it was tested against the right environment.

## Environments

| Environment | Purpose | Database | External Providers |
|---|---|---|---|
| `Testing` | Fast build/test/smoke checks | InMemory | Mock/fallback only |
| `Development` | Real local workflow checks | SQL Server | Mock/fallback by default |
| `Staging` | Production-like validation | SQL Server | Sandbox/live provider credentials |
| `Production` | Live hospital system | SQL Server | Live provider credentials |

## Automated Baseline

Run before manual testing:

```bash
./scripts/verify-backend.sh
```

Run with smoke tests:

```bash
RUN_SMOKE=1 ./scripts/verify-backend.sh
```

Windows PowerShell:

```powershell
.\scripts\verify-backend.ps1
.\scripts\verify-backend.ps1 -Smoke
```

## Startup And Database

| Check | Expected Result | Status |
|---|---|---|
| `dotnet restore` | Packages restore without errors | Pending |
| Project build | `Okafor-.NET.csproj` builds | Pending |
| Test project build | Test project builds | Pending |
| Non-smoke tests | All non-smoke tests pass | Pending |
| App starts in `Testing` | `/health` returns `200` | Pending |
| App starts in `Development` | SQL Server connection works | Pending |
| Migrations apply | Schema is current | Pending |
| Development/Staging demo seed | Roles, demonstration doctors, departments, posts, and sample appointments appear | Pending |
| Production demo-data guard | Production startup creates identity roles/configured admin but no fictional doctors, posts, or appointments | Pending |
| Admin seed login | Configured admin can sign in | Pending |

## Public Website

| Check | Expected Result | Status |
|---|---|---|
| Home page | Loads without `500` | Pending |
| About page | Loads | Pending |
| Services page | Loads | Pending |
| Doctors listing | Shows doctors from database | Pending |
| Doctor profile | Slug route loads for a seeded doctor | Pending |
| News listing | Shows published posts | Pending |
| News detail | Slug route loads for a seeded post | Pending |
| Contact form | Saves submission and confirms success | Pending |
| Site search | Returns relevant results | Pending |
| WhatsApp widget | Bottom-right click opens configured WhatsApp number | Pending |

## Appointment Workflow

| Check | Expected Result | Status |
|---|---|---|
| Appointment request page | Loads for public users | Pending |
| Department/doctor selection | Doctors filter by department | Pending |
| Available slots endpoint | Returns expected slots | Pending |
| Submit appointment request | Request saves to SQL Server | Pending |
| Admin appointment queue | New request appears | Pending |
| Admin approval | Status changes and persists | Pending |
| Admin rejection | Status changes and persists | Pending |
| SignalR update | Admin/public realtime update works where used | Pending |
| Reminder service | Does not crash startup and logs safely | Pending |

## Teleconsultation Workflow

| Check | Expected Result | Status |
|---|---|---|
| Request page | Loads | Pending |
| Consultation type policy | Form offers video/follow-up only; posted phone-call teleconsultation is rejected | Pending |
| Submit request | Request saves to SQL Server | Pending |
| WhatsApp opt-in | Value persists | Pending |
| Submitted page | Loads by request id | Pending |
| Admin queue | New request appears | Pending |
| Admin status update | Status and notes persist | Pending |
| Patient history | Patient can see their requests | Pending |

## Patient Portal

| Check | Expected Result | Status |
|---|---|---|
| Patient registration | User can register | Pending |
| Email confirmation gate | Unconfirmed patient cannot sign in; confirmed patient can sign in | Pending |
| Patient login | Patient can sign in | Pending |
| Patient dashboard | Loads authorized data only | Pending |
| Profile create/edit | Saves and persists | Pending |
| Appointment list | Shows patient appointments | Pending |
| Calendar download | Downloads valid calendar file | Pending |
| Appointment cancel | Cancels allowed appointment | Pending |
| Document upload | Uploads valid file | Pending |
| Document delete | Deletes only authorized document | Pending |
| Messages | Patient can send and view messages | Pending |

## Admin And Staff

| Check | Expected Result | Status |
|---|---|---|
| Admin dashboard | Loads for Admin role | Pending |
| Staff access | Staff can access permitted screens only | Pending |
| User management | Admin can create user and edit roles | Pending |
| Patient profiles | Admin can create/view/edit | Pending |
| Patient appointments | Admin can create/view/edit | Pending |
| Document upload | Admin can attach patient document | Pending |
| CMS post create/edit | Post saves with publish state | Pending |
| Contact submissions | Saved contact form appears | Pending |
| Unauthorized access | Non-admin cannot access admin screens | Pending |

## Payments

| Check | Expected Result | Status |
|---|---|---|
| Mock donation | Creates donation and receipt | Pending |
| Mock bill payment | Creates bill payment and receipt | Pending |
| Receipt email fallback | Failure is logged, not fatal | Pending |
| Paystack init | Sandbox keys create payment authorization URL | Pending |
| Paystack callback | Payment status updates | Pending |
| Paystack webhook | Signed webhook updates matching payment | Pending |

## Notifications

| Check | Expected Result | Status |
|---|---|---|
| Lean notifications | Local fallback does not require provider secrets | Pending |
| SMTP | Sends email with configured SMTP credentials | Pending |
| Africa's Talking SMS | Sends sandbox/live SMS with configured credentials | Pending |
| WhatsApp webhook verify | Provider challenge succeeds | Pending |
| WhatsApp inbound message | Scheduling flow responds safely | Pending |
| WhatsApp outbound template | Template sends to opted-in phone number | Pending |
| Push subscription save | Browser subscription persists | Pending |
| Push test notification | Browser receives test notification | Pending |

## PWA And Offline

| Check | Expected Result | Status |
|---|---|---|
| Manifest | `/site.webmanifest` loads | Pending |
| Service worker | `/service-worker.js` loads and registers | Pending |
| Offline fallback | Offline page renders with network disabled | Pending |
| Offline appointments | Appointment offline page renders | Pending |
| Offline documents policy | Confirm documents are not advertised as offline unless explicit encrypted vault is implemented | Pending |
| Sensitive cache exclusions | Private/admin routes are not cached for offline replay | Pending |
| Install prompt | Prompt is usable and does not overlap WhatsApp widget | Pending |

### Manual Checklist: PWA Install Prompt

Requires a deployed/staging URL over HTTPS (install prompts do not fire on `http://` origins except `localhost`). Run this after every change to `wwwroot/js/pwa-register.js`, `wwwroot/service-worker.js`, or `wwwroot/site.webmanifest`.

**Desktop Chrome/Edge (fires `beforeinstallprompt` — most reliable target):**
1. Open the site in a fresh profile (or clear site data first) so the install prompt hasn't already been dismissed this session.
2. Confirm a `[data-pwa-install]` button appears near the footer within a few seconds of page load.
3. Click it once: the browser's native install dialog should appear immediately (button click must call `installPrompt.prompt()` synchronously — if the dialog doesn't appear, check the console for a "user gesture" rejection).
4. Accept the install. Confirm: the app opens in its own window, the `[data-pwa-install]` button is removed from the original page (via the `appinstalled` listener), and the OS shows an installed app icon.
5. Reload the original tab. Confirm the install button does not reappear (browser suppresses `beforeinstallprompt` once installed).
6. Uninstall the PWA and repeat once, this time dismissing the native dialog instead of accepting: confirm the button becomes clickable again (re-enabled, not stuck disabled) — this covers the `installPrompt` nulled/re-enable guard in `pwa-register.js`.

**Android Chrome:** same flow as desktop; additionally confirm the install button does not visually collide with the floating WhatsApp button at 360px/390px/430px widths (flagged as a risk in `docs/REPO_READINESS_AUDIT.md`).

**iOS Safari (no `beforeinstallprompt` support — this is expected, not a bug):** confirm the `[data-pwa-install]` button correctly never appears (Safari never fires the event, so `showInstallButton()` never runs). Manually verify "Add to Home Screen" from the Safari share sheet still installs a working icon/splash screen using `site.webmanifest`.

**Failed registration path:** with dev tools open, block `/service-worker.js` (Network tab → block request URL) and reload. Confirm the page still loads normally (registration failure is caught and swallowed, per `pwa-register.js`'s `.catch(function () {})` — verified structurally by `PWARegistrationTests.ServiceWorkerRegistration_IsDeferredToLoad_AndFailureIsHandled`, but the resulting page behavior still needs a human check).

### Manual Checklist: Offline Appointment Sync

1. With a normal network connection, sign in as a patient with at least one upcoming appointment and open `/Portal/Appointments` so the current appointment list gets cached by `pwa-appointments.js`.
2. Enable airplane mode / DevTools "Offline" throttling, then open `/offline-appointments.html` directly.
3. Confirm the previously-viewed appointment summary renders from the local encrypted store, not a blank/error state.
4. With no appointments ever viewed (fresh browser profile, still offline), open `/offline-appointments.html` and confirm the empty state renders: the `aria-live="polite"` / `role="status"` banner from `wwwroot/offline-appointments.html` (`data-offline-appointments-empty`), not a broken page.
5. Restore network connectivity and confirm a normal `/Portal/Appointments` load still works and reflects live server data (i.e., the offline cache is a fallback, not a stale source of truth once the network returns).
6. Confirm private/authenticated routes (`/Portal/*`, `/Admin/*`) never get served from the service worker's cache while offline with no prior visit — they should show the "Connection required" fallback from `handleNetworkOnly()` in `service-worker.js`, not a cached page or stale patient data.

## CI Failure Evidence

When a GitHub Actions check fails, open the failed workflow run and download its artifact from the **Artifacts** section:

- `linux-test-and-smoke-failure` contains the Testing-host log plus non-smoke or smoke TRX results.
- `windows-test-failure` contains the Windows non-smoke TRX result.
- `e2e-failure-evidence` contains Playwright screenshots and traces for failed browser journeys.

Artifacts are retained for seven days. Treat logs as operational data: review them privately, redact patient or provider information before sharing, and never paste credentials into an issue or pull request.

## Deployment And Recovery

| Check | Expected Result | Status |
|---|---|---|
| Immutable release image | Staging and Production use the same recorded image digest | Pending |
| Explicit migration job | `--migrate-db` completes once before candidate traffic | Pending |
| Zero-traffic candidate | Candidate revision passes live/ready probes before traffic moves | Pending |
| Application rollback | Previous healthy revision can receive 100% traffic | Pending |
| Azure SQL restore | Point-in-time restore creates and validates an isolated database | Pending |
| Azure Files restore | Private and CMS shares restore to isolated alternate locations | Pending |
| Coordinated document recovery | Authorized metadata-to-file links work and cross-patient access remains denied | Pending |
| Recovery objectives | Measured RPO/RTO and owner acceptance are recorded | Pending |

## Regression Rule

When a feature breaks, add one of these before closing the fix:

- An automated test if the behavior can be tested without real provider credentials.
- A manual checklist entry if it requires browser/device/provider validation.
- A note in `docs/FEATURE_INVENTORY.md` if the feature status changes.
