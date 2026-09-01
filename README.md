# Boniface & Paulina Okafor Memorial Hospital — Web Application

An ASP.NET Core MVC hospital management website with a public-facing site, admin panel, and patient document portal.

The current source-backed hosting decision is in [`DEPLOYMENT.md`](DEPLOYMENT.md).
The production-fidelity browser strategy and commands are in [`docs/E2E_TESTING.md`](docs/E2E_TESTING.md).

Primary hospital identity used by the public site:

- **Address**: Ndibemaduka Compound, Umudim Ngodo Isuochi, Umunneochi L.G.A, Abia State, Nigeria
- **Email**: `info@okaformemorial.org`
- **Emergency numbers**: `112 / 199`

---

## Technology Stack

- **Framework**: ASP.NET Core MVC (.NET 10)
- **Database**: PostgreSQL 16 (Supabase when hosted)
- **ORM**: Entity Framework Core (code-first migrations)
- **Auth**: ASP.NET Core Identity with roles (`Admin`, `Staff`, `Patient`)
- **Frontend**: Razor Views — compiled Tailwind CSS utilities (public), Bootstrap 5 (admin/patient), Alpine.js interactions. The public landing page (`Views/Home/Index.cshtml`) mounts a React tree (`client/landing/`, built by Vite to `wwwroot/js/landing.js`) into a server-rendered shell — see [`docs/LANDING_PAGE_HANDOFF.md`](docs/LANDING_PAGE_HANDOFF.md) and [`docs/decisions/0006-react-landing-page-non-headless.md`](docs/decisions/0006-react-landing-page-non-headless.md).

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL 16, or Docker for the included local PostgreSQL service
- Visual Studio 2022+ or VS Code with C# Dev Kit

### Linux development

- Install the .NET 10 SDK (user-level installer is convenient if you don't want root):

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 10.0 --install-dir $HOME/.dotnet
export PATH=$HOME/.dotnet:$HOME/.dotnet/tools:$PATH
```

- Docker: if you plan to run PostgreSQL locally, ensure your user can access the Docker socket. Either run with `sudo` or add your user to the `docker` group and re-login:

```bash
# add user to docker group (requires sudo)
sudo usermod -aG docker $USER
# then log out and log back in for the group change to take effect
```

- Start PostgreSQL with Docker Compose. Copy `.env.example` to `.env`, replace
  both matching password placeholders, and URL-encode special characters in the
  password inside `DATABASE_URL`:

```bash
cp .env.example .env
# edit .env before starting the container
docker compose up -d
```

- If you can't run Docker, run the app in `Testing` environment (uses InMemory DB):

```bash
$HOME/.dotnet/dotnet run --launch-profile demo
```

The demo profile runs at `http://localhost:5187` without PostgreSQL and uses clearly labelled mock payments; it never collects real money.

- To build frontend assets (Tailwind CSS and the React landing page):

```bash
npm install
npm run build
```

- Fedora users can install Node/npm with:

```bash
sudo dnf install -y nodejs npm
```

- More detailed Linux notes are in [`docs/LOCAL_LINUX_SETUP.md`](docs/LOCAL_LINUX_SETUP.md).


---

## Getting Started

### 1. Clone and restore

```bash
git clone <repo-url>
cd Okafor-.NET
dotnet restore
```

### 2. Configure PostgreSQL

Set `DATABASE_URL` to either a PostgreSQL URL or an Npgsql key/value connection
string. Local Docker values are shown in `.env.example`; hosted values belong in
the host's secret manager. For Supabase, copy the Direct connection or Shared
Pooler **Session mode** string on port `5432` from the Dashboard's **Connect**
panel. Do not use Transaction mode port `6543` for EF migrations.

> **Note**: Do not commit real credentials. The application loads a local `.env`
> file for development, and deployment platforms should inject the same setting
> as a secret.

### 3. Apply migrations

Run all pending migrations to create the database schema:

```bash
dotnet run -- --migrate-db
```

### 4. Run the application

```bash
dotnet run
```

The application will be available at `https://localhost:5001` (or the port shown in your terminal).

---

## Frontend Assets

The public site uses Tailwind CSS from a local compiled stylesheet, not the Tailwind CDN. The
public landing page (`Views/Home/Index.cshtml`) is a React tree built from `client/landing/`
— see [`docs/LANDING_PAGE_HANDOFF.md`](docs/LANDING_PAGE_HANDOFF.md).

Build both after changing Razor utility classes, `wwwroot/css/tailwind.input.css`, or anything
under `client/landing/`:

```bash
npm install
npm run build
```

During active UI work you can run either watcher on its own:

```bash
npm run watch:css       # rebuilds wwwroot/css/tailwind.css on change
npm run watch:landing   # rebuilds wwwroot/js/landing.js on change
```

`npm run build:css` writes `wwwroot/css/tailwind.css` (referenced by `Views/Shared/_Layout.cshtml`).
`npm run build:landing` writes `wwwroot/js/landing.js` (referenced by `Views/Home/Index.cshtml`).
Both outputs are committed, the same way `tailwind.css` always has been — there is no build step
in CI or at deploy time, so re-run this after editing either source and commit the result.

---

## Collaboration Docs

- [`docs/COLLABORATION_WORKFLOW.md`](docs/COLLABORATION_WORKFLOW.md) explains backend/frontend ownership boundaries.
- [`docs/LANDING_PAGE_HANDOFF.md`](docs/LANDING_PAGE_HANDOFF.md) describes the current frontend/backend handoff for the homepage redesign.
- [`docs/FUNCTIONALITY_RECOVERY_PLAN.md`](docs/FUNCTIONALITY_RECOVERY_PLAN.md) defines the backend recovery phases and completion rules.
- [`docs/FUNCTIONALITY_LOOP.md`](docs/FUNCTIONALITY_LOOP.md) defines the repeatable Codex improvement loop.
- [`docs/FUNCTIONALITY_LOOP_BOARD.md`](docs/FUNCTIONALITY_LOOP_BOARD.md) separates Codex-lane work from owner-only tasks.
- [`docs/API_SIGNUP_CHECKLIST.md`](docs/API_SIGNUP_CHECKLIST.md) lists the external accounts and API keys needed for launch.
- [`docs/REPO_READINESS_AUDIT.md`](docs/REPO_READINESS_AUDIT.md) tracks cleanup, visual risks, and next-dev onboarding findings.
- [`docs/FEATURE_INVENTORY.md`](docs/FEATURE_INVENTORY.md) lists the implemented features and their verification status.
- [`docs/VERIFICATION_CHECKLIST.md`](docs/VERIFICATION_CHECKLIST.md) is the manual/automated checklist for proving functionality.
- [`docs/RECOVERY_STATUS.md`](docs/RECOVERY_STATUS.md) records the latest verified local result.
- [`DEPLOYMENT.md`](DEPLOYMENT.md) defines the active Supabase/Render release,
  migration, verification, and rollback process.
- [`docs/ENVIRONMENT_VARIABLES.md`](docs/ENVIRONMENT_VARIABLES.md) lists local and provider configuration keys.
- [`docs/LOCAL_WINDOWS_SETUP.md`](docs/LOCAL_WINDOWS_SETUP.md) gives Windows-specific clone/build/run steps.
- Architecture decision records live in [`docs/decisions`](docs/decisions).

---

## Backend Verification

Linux/macOS:

```bash
./scripts/verify-backend.sh
RUN_SMOKE=1 ./scripts/verify-backend.sh
```

Windows PowerShell:

```powershell
.\scripts\verify-backend.ps1
.\scripts\verify-backend.ps1 -Smoke
```

The first command restores, builds, and runs non-smoke tests. The smoke option starts the app in `Testing` mode and verifies critical routes against `http://localhost:5187`.

---

## Seed Data

On first run, the application seeds operational identity data in every non-test
environment. Fictional demonstration content is restricted to `Development` and
`Staging` and is never inserted by `Production` startup:

| Seed Class              | What it seeds                                                              |
|-------------------------|----------------------------------------------------------------------------|
| `IdentitySeed`          | Roles (`Admin`, `Staff`, `Patient`) and configured admin user in non-test environments |
| `DemoDataSeed`          | Runs the clinical, news, and appointment demonstration seeds in `Development` only. Any other host must opt in with `DemoData:Enabled=true`; `Production`, `E2E`, and `Testing` refuse the opt-in. The hosted preview serves the public site as `Staging`, so it must never seed demo records. |
| `ClinicalDataSeed`      | 7 departments and 9 demonstration providers with bios, qualifications, and consultation details |
| `NewsDataSeed`          | 5 published demonstration posts, 1 featured, 1 draft                      |
| `AppointmentDataSeed`   | 5 fictional appointment requests (pending, approved, rejected)            |

All seed classes are idempotent. Production clinical, provider, news, and
appointment content must be entered or imported from owner-approved real data.

---

## Default Admin Account

The admin account is seeded only when both settings below are configured:

| Setting                  | Default value             |
|--------------------------|---------------------------|
| `SeedAdmin:Email`        | `admin@example-hospital.local` |
| `SeedAdmin:Password`     | `CHANGE_ME_USE_USER_SECRETS`   |

Override these in `appsettings.json` or user secrets before deploying:

```json
{
  "SeedAdmin": {
    "Email": "admin@yourhospital.org",
    "Password": "YourStrongPassword1!"
  }
}
```

> **Never commit real admin credentials to source control.**

---

## Migrations

To create a new migration after model changes:

```bash
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

To roll back to a specific migration:

```bash
dotnet ef database update <MigrationName>
```

To remove the last unapplied migration:

```bash
dotnet ef migrations remove
```

---

## Upload Storage

| Path | Contents | Access | Max size |
|---|---|---|---|
| `wwwroot/uploads/posts/` | Blog post thumbnail images | Public | 5 MB |
| `App_Data/patient-documents/` | Patient health documents | Authorized controller only | 10 MB |

The public `wwwroot/uploads/` root is created automatically for CMS images. Private patient storage is created on first document upload. Set `PatientDocuments:StorageRoot` to a persistent, non-public production volume when the default `App_Data` location is unsuitable. Requests to the legacy `/uploads/patient-documents` public path are blocked.

Allowed file types:
- **Post thumbnails**: `.jpg`, `.jpeg`, `.png`, `.webp`
- **Patient uploads**: `.pdf`, `.jpg`, `.jpeg`, `.png`, `.doc`, `.docx`
- **Admin patient-document uploads**: `.pdf`, `.jpg`, `.jpeg`, `.png`, `.webp`

> Upload storage is excluded from source control via `.gitignore`. Back up both CMS images and private patient-document storage separately in production.

---

## Application Areas

### Public Site (`/`)
| Route                          | Description                        |
|--------------------------------|------------------------------------|
| `/`                            | Homepage                           |
| `/Home/About`                  | About the hospital                 |
| `/Home/Services`               | Clinical departments               |
| `/Home/Doctors`                | Doctors listing                    |
| `/doctors/{slug}`              | Individual doctor profile          |
| `/Home/Contact`                | Contact form                       |
| `/AppointmentRequests/Create`  | Public appointment booking form    |
| `/AppointmentRequests/GetAvailableSlots` | Availability API for booking widget |
| `/AppointmentRequests/BookSlot` | AJAX booking endpoint             |
| `/Teleconsultations/Create`     | Public teleconsultation request form |
| `/Teleconsultations/Submitted?reference={protected-reference}` | Protected teleconsultation request confirmation |
| `/BillPayments`                 | Online bill payment form (sandbox by default) |
| `/BillPayments/Receipt/{id}`    | Bill payment receipt              |
| `/Home/Team`                    | Doctors, leadership, and care staff overview |
| `/news/{slug}`                 | Blog post detail                   |
| `/Home/News`                   | Blog listing                       |
| `/Home/PatientInformationHub`  | Patient information resources      |
| `/Home/Search`                 | Public site search                 |
| `/Donation`                    | Online donation form               |
| `/robots.txt`                  | Search crawler policy              |
| `/sitemap.xml`                 | Public sitemap for core routes     |

### Admin Panel (`/Admin/`)
Primarily requires `Admin` role. Appointment request review currently allows both `Admin` and `Staff`.

| Route                              | Description                     |
|------------------------------------|---------------------------------|
| `/Admin/Dashboard`                 | Overview dashboard              |
| `/Admin/Availability`              | Manage doctor availability, generate slots, review notification logs |
| `/Admin/Doctors`                   | Manage doctors                  |
| `/Admin/Departments`               | Manage departments              |
| `/Admin/AppointmentRequests`       | Review and approve appointments |
| `/Admin/Teleconsultations`         | Review, confirm, reschedule, complete, or reject teleconsultations |
| `/Admin/BillPayments`              | Review bill payment records and sandbox/production status |
| `/Admin/PatientAppointments`       | Create and manage scheduled patient appointments |
| `/Admin/Posts`                     | Manage blog posts               |
| `/Admin/ContactSubmissions`        | View contact form submissions   |
| `/Admin/Users`                     | Manage user accounts            |
| `/Admin/PatientProfiles`           | Patient profiles and documents  |

### Patient Portal (`/Portal/`)
Requires `Patient` role.

| Route                              | Description                     |
|------------------------------------|---------------------------------|
| `/Portal`                          | Dashboard redirect              |
| `/Portal/Profile`                  | View and edit patient profile   |
| `/Portal/Appointments`             | View appointments and booking requests |
| `/Portal/Appointments/DownloadCalendar` | Download appointment calendar file |
| `/Portal/Documents`                | View personal documents         |
| `/Portal/Messages`                 | View patient messages           |
| `/Portal/Messages/Send`            | Send message to hospital        |

Patients are linked to a `PatientProfile` by an admin. Each patient can only access their own documents.

---

## Security Notes

- Most admin routes require `[Authorize(Roles = "Admin")]` via `AdminBaseController`.
- `Areas/Admin/Controllers/AppointmentRequestsController.cs` explicitly allows `Admin` and `Staff` roles for appointment review actions.
- All patient portal routes require `[Authorize(Roles = "Patient")]` via `PatientBaseController`.
- The public appointment form is explicitly `[AllowAnonymous]`.
- Public teleconsultation and bill payment forms use server-side validation and anti-forgery protection.
- Admin teleconsultation and bill payment oversight requires `Admin` or `Staff`.
- Global `AutoValidateAntiforgeryTokenAttribute` is applied to all MVC controllers.
- File uploads validate extension, MIME type, size, and file signature server-side.
- Patients cannot access other patients' documents — the portal filters by the authenticated user's profile ID.
- Cookie security is set to `HttpOnly`, `Secure`, and `SameSite=Lax`.
- Account lockout is enabled (5 failed attempts, 15-minute lockout).
- Production HSTS is enabled in `Program.cs`; SSL/TLS certificates remain a hosting responsibility.
- Security headers are applied globally: `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, and a conservative Content Security Policy that permits the compiled local Tailwind stylesheet, the existing Alpine.js/SignalR script dependencies, Google Fonts, and Google Maps.
- The current Alpine.js CDN build and inline Alpine expressions require `'unsafe-eval'` in `script-src`. To remove that allowance later, migrate the affected components to Alpine's CSP-compatible build and avoid inline expression evaluation.
- Backup and recovery are operational deployment requirements. Back up the PostgreSQL database, `wwwroot/uploads/posts/`, and the configured private patient-document storage on a regular schedule before production launch.

---

## SEO And Public Identity

- The shared public layout supports a page-specific `ViewData["MetaDescription"]` with a hospital-focused fallback description.
- The layout includes basic `Hospital` structured data for the hospital name, Abia State address, email, and medical specialties.
- `wwwroot/robots.txt` and `wwwroot/sitemap.xml` are included for launch. Update sitemap host names if the production domain differs from `https://www.okaformemorial.org`.
- The contact page uses the configured hospital address and a configurable Google Maps embed URL.

Hospital configuration:

```json
{
  "Hospital": {
    "Name": "Boniface and Paulina Okafor Memorial Hospital",
    "Address": "Ndibemaduka Compound, Umudim Ngodo Isuochi, Umunneochi L.G.A, Abia State, Nigeria",
    "Email": "info@okaformemorial.org",
    "EmergencyNumbers": "112 / 199",
    "GoogleMapEmbedUrl": "https://www.google.com/maps?q=Ndibemaduka%20Compound%20Umudim%20Ngodo%20Isuochi%20Umunneochi%20Abia%20State%20Nigeria&output=embed"
  }
}
```

---

## Placeholder Images

Random hospital/gallery images are loaded from `wwwroot/images/placeholders/Hospital/` by `ImageService`.

Current behavior:
- The homepage and about page request randomized hospital images through `IImageService`.
- If no local placeholder images are available, `ImageService` falls back to `/images/placeholders/placeholder.svg`.
- The repository currently includes a populated `Hospital/` placeholder folder with `.webp` images used for these randomized sections.

If you replace the placeholder set, keep the images under `wwwroot/images/placeholders/Hospital/` so the existing image service continues to work.

---

## Environment Notes

| Setting                       | Development                     | Production recommendation         |
|-------------------------------|---------------------------------|------------------------------------|
| `ASPNETCORE_ENVIRONMENT`      | `Development`                   | `Production`                       |
| Exception pages               | Full developer pages            | Custom `/Home/Error` handler       |
| HSTS                          | Off                             | On (enabled in `Program.cs`)       |
| `DATABASE_URL`                | Local PostgreSQL                | Supabase secret from the host       |
| Admin credentials             | User secrets or `.env`          | Host environment secrets            |

---

## Teleconsultations

Teleconsultation requests are first-class records separate from in-person appointment requests.

| Route | Description |
|-------|-------------|
| `/Teleconsultations/Create` | Anonymous or authenticated patient teleconsultation request |
| `/Teleconsultations/Submitted?reference={protected-reference}` | Confirmation page with a non-guessable protected request reference |
| `/Admin/Teleconsultations` | Admin/staff review queue |

Supported statuses:
- `Pending`
- `Confirmed`
- `Rescheduled`
- `Completed`
- `Rejected`

Authenticated requests are linked to the current `ApplicationUser` and, when present, the matching `PatientProfile`.
Notifications use the existing `INotificationService` abstraction.

---

## Online Bill Payments

Bill payments are distinct from donations. They store invoice/reference numbers, patient identity details, amount, currency, payment provider metadata, sandbox flag, timestamps, and status.

| Route | Description |
|-------|-------------|
| `/BillPayments` | Public bill payment form |
| `/BillPayments/Receipt/{id}` | Patient receipt page |
| `/Admin/BillPayments` | Admin/staff payment review |

The default provider is `MockPaymentGateway`, which records sandbox-approved transactions only.
Sandbox payments are clearly marked in the user flow, receipts, admin views, and email receipt content.

Donation operations are available at `/Admin/Donations`. When confirmed accounts are required outside Development, startup also requires real SMTP host and sender settings so registration cannot silently launch without email delivery.

Configuration:

```json
{
  "Payments": {
    "Provider": "Mock",
    "Mock": {
      "ReferencePrefix": "SANDBOX"
    }
  },
  "Email": {
    "SmtpHost": "",
    "Port": 25,
    "EnableSsl": false,
    "FromAddress": "info@okaformemorial.org",
    "Username": "",
    "Password": ""
  }
}
```

To integrate a production gateway, implement `IBillPaymentProvider` and register it in `Program.cs` based on `Payments:Provider`.

---

## Team Page

The explicit team experience is available at `/Home/Team`.
It combines static leadership/care-team sections with the existing doctor directory data.

---

## Notifications

Notification provider selection is config-driven:

```json
{
  "Notifications": {
    "Provider": "Lean",
    "AdminEmail": "admin@okaformemorial.org",
    "AdminPhone": "+2348012345678",
    "HospitalPhone": "112",
    "WhatsAppNumber": "+2348012345678"
  }
}
```

Development uses `Lean` by default. The Africa's Talking implementation remains a sandbox/logging-ready extension point until the real SDK calls and live credentials are enabled.

---

## License

This project was built as a demonstration and academic portfolio piece. All patient data in the seed files is fictional.

---

## Testing

Run the test suite with:

```bash
dotnet test
```

Launch-critical browser journeys use Playwright, Kestrel, PostgreSQL Testcontainers, migrations, and Respawn in a separate project. With Docker available:

```bash
E2E_INSTALL_BROWSERS=1 ./scripts/verify-e2e.sh
./scripts/verify-e2e.sh
```

Live API keys are intentionally not required by this deterministic E2E suite. Provider credentials are verified later in staging.

The repository now includes:
- Unit tests for `ImageService`
- Controller tests for doctor create/edit behavior
- An integration test that boots the app in `Testing` mode and checks `/health` and `/`

## Observability

The app now exposes a lightweight health endpoint at `/health` and logs image fallback behavior from `ImageService` so empty placeholder folders are visible in application logs.

## CI

GitHub Actions is configured in `.github/workflows/ci.yml` to restore, build, test, and run a vulnerability scan on every push and pull request.

## Staging And Smoke Tests

A simple staging smoke test checklist is now:
1. Confirm `/health` returns `200 OK`.
2. Confirm `/` loads without errors.
3. Confirm doctor and appointment management routes still render after deployment.
4. Confirm `dotnet test` and `dotnet list package --vulnerable --include-transitive` pass in CI.

## Backlog Priority

Recommended next priorities after the current cleanup:
1. Expand booking and appointment workflow coverage.
2. Add deployment-specific smoke checks for admin and patient areas.
3. Add central request/exception logging if a structured logging provider is introduced later.
4. Add performance or load checks for the public search and doctor pages.
