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
| Seed data exists | Roles, doctors, departments, posts, sample appointments appear | Pending |
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
| Sensitive cache exclusions | Private/admin routes are not cached for offline replay | Pending |
| Install prompt | Prompt is usable and does not overlap WhatsApp widget | Pending |

## Regression Rule

When a feature breaks, add one of these before closing the fix:

- An automated test if the behavior can be tested without real provider credentials.
- A manual checklist entry if it requires browser/device/provider validation.
- A note in `docs/FEATURE_INVENTORY.md` if the feature status changes.
