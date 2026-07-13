# Boniface & Paulina Okafor Memorial Hospital — Web Application

An offline-aware ASP.NET Core MVC hospital platform designed around the realities of rural care delivery in Nigeria: mobile-first access, intermittent connectivity, simple staff workflows, and careful handling of patient information.

The application combines:

- Public hospital information, doctor discovery, news, contact, appointment, teleconsultation, donation, and bill-payment journeys.
- Role-based admin and staff workflows for scheduling, patient records, content, availability, and operational review.
- An authenticated patient portal for profiles, appointments, calendar exports, documents, messages, teleconsultations, and notification preferences.
- PWA installation and public offline fallbacks while deliberately excluding private, admin, payment, upload, and realtime routes from general offline caching.

## Current Launch Status

The product is in launch hardening, not represented as production-launched. Local restore, build, automated tests, Testing-mode startup, HTTP smoke checks, and SQL Server development flows have been verified. Production hosting, provider credentials, staging rehearsal, browser/device QA, backup restoration, and final privacy/operational approval remain explicit launch gates.

See [Recovery Status](docs/RECOVERY_STATUS.md), [Feature Inventory](docs/FEATURE_INVENTORY.md), and [Project Showcase](docs/PROJECT_SHOWCASE.md) for evidence and portfolio-ready context.

Primary hospital identity used by the public site:

- **Address**: Ndibemaduka Compound, Umudim Ngodo Isuochi, Umunneochi L.G.A, Abia State, Nigeria
- **Email**: `info@okaformemorial.org`
- **Emergency numbers**: `112 / 199`

---

## Technology Stack

- **Framework**: ASP.NET Core MVC (.NET 10)
- **Database**: SQL Server (LocalDB for development)
- **ORM**: Entity Framework Core (code-first migrations)
- **Auth**: ASP.NET Core Identity with roles (`Admin`, `Staff`, `Patient`)
- **Frontend**: Razor Views — compiled Tailwind CSS utilities (public), Bootstrap 5 (admin/patient), Alpine.js interactions
- **Realtime/PWA**: SignalR, service worker, web manifest, offline fallbacks, and push-notification support
- **Integrations**: Paystack-ready payments, SMTP email, WhatsApp Cloud API, Africa's Talking, and web push behind configuration

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server LocalDB (included with Visual Studio) **or** a SQL Server instance
- Visual Studio 2022+ or VS Code with C# Dev Kit

### Linux development

- Install the .NET 10 SDK (user-level installer is convenient if you don't want root):

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 10.0 --install-dir $HOME/.dotnet
export PATH=$HOME/.dotnet:$HOME/.dotnet/tools:$PATH
```

- Docker: if you plan to run SQL Server in Docker, ensure your user can access the Docker socket. Either run with `sudo` or add your user to the `docker` group and re-login:

```bash
# add user to docker group (requires sudo)
sudo usermod -aG docker $USER
# then log out and log back in for the group change to take effect
```

- Start SQL Server with Docker Compose (copy `.env.example` to `.env` and set a strong `SA_PASSWORD` first):

```bash
cp .env.example .env
# edit .env to set SA_PASSWORD
docker compose up -d
```

- If you can't run Docker, run the app in `Testing` environment (uses InMemory DB):

```bash
ASPNETCORE_ENVIRONMENT=Testing $HOME/.dotnet/dotnet run --no-launch-profile
```

- To build frontend CSS (Tailwind):

```bash
npm install
npm run build:css
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

### 2. Configure connection string

The default committed configuration targets SQL Server LocalDB for Windows development. On Linux, use Docker SQL Server or `Testing` mode. Edit `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=OkaforHospital;Trusted_Connection=True;"
  }
}
```

> **Note**: Do not commit real credentials. Use environment variables or user secrets in production.

To use user secrets locally:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-connection-string"
```

### 3. Apply migrations

Run all pending migrations to create the database schema:

```bash
dotnet ef database update
```

### 4. Run the application

```bash
dotnet run
```

The application will be available at `https://localhost:5001` (or the port shown in your terminal).

---

## Frontend Assets

The public site uses Tailwind CSS from a local compiled stylesheet, not the Tailwind CDN.

Build the stylesheet after changing Razor utility classes or `wwwroot/css/tailwind.input.css`:

```bash
npm install
npm run build:css
```

During active UI work you can use:

```bash
npm run watch:css
```

The generated file is `wwwroot/css/tailwind.css`, which is referenced by `Views/Shared/_Layout.cshtml`.

---

## Collaboration Docs

- [`docs/COLLABORATION_WORKFLOW.md`](docs/COLLABORATION_WORKFLOW.md) explains backend/frontend ownership boundaries.
- [`FRONTEND_BACKEND_INTEGRATION_CONTRACT.md`](FRONTEND_BACKEND_INTEGRATION_CONTRACT.md) describes the frontend/backend handoff contract.
- [`docs/FUNCTIONALITY_RECOVERY_PLAN.md`](docs/FUNCTIONALITY_RECOVERY_PLAN.md) defines the backend recovery phases and completion rules.
- [`docs/FUNCTIONALITY_LOOP.md`](docs/FUNCTIONALITY_LOOP.md) defines the repeatable Codex improvement loop.
- [`docs/FUNCTIONALITY_LOOP_BOARD.md`](docs/FUNCTIONALITY_LOOP_BOARD.md) separates Codex-lane work from owner-only tasks.
- [`docs/API_SIGNUP_CHECKLIST.md`](docs/API_SIGNUP_CHECKLIST.md) lists the external accounts and API keys needed for launch.
- [`docs/REPO_READINESS_AUDIT.md`](docs/REPO_READINESS_AUDIT.md) tracks cleanup, visual risks, and next-dev onboarding findings.
- [`docs/FEATURE_INVENTORY.md`](docs/FEATURE_INVENTORY.md) lists the implemented features and their verification status.
- [`docs/VERIFICATION_CHECKLIST.md`](docs/VERIFICATION_CHECKLIST.md) is the manual/automated checklist for proving functionality.
- [`docs/RECOVERY_STATUS.md`](docs/RECOVERY_STATUS.md) records the latest verified local result.
- [`docs/ENVIRONMENT_VARIABLES.md`](docs/ENVIRONMENT_VARIABLES.md) lists local and provider configuration keys.
- [`docs/DATA_CLEANING_WORKFLOW.md`](docs/DATA_CLEANING_WORKFLOW.md) defines how to clean CSV/spreadsheet exports without changing originals.
- [`docs/PROJECT_SHOWCASE.md`](docs/PROJECT_SHOWCASE.md) provides architecture highlights, engineering decisions, launch boundaries, and LinkedIn-ready talking points.
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

On first run, the application automatically seeds:

| Seed Class              | What it seeds                                                              |
|-------------------------|----------------------------------------------------------------------------|
| `IdentitySeed`          | Roles (`Admin`, `Staff`, `Patient`) and default admin user                 |
| `ClinicalDataSeed`      | 6 departments and 8 doctors with bios, qualifications, consultation hours  |
| `NewsDataSeed`          | 5 published posts, 1 featured, 1 draft                                     |
| `AppointmentDataSeed`   | 5 sample appointment requests (pending, approved, rejected)                |

All seed classes are idempotent — they skip seeding if data already exists.

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

| Default path | Contents | Max size |
|---|---|---|
| `wwwroot/uploads/posts/` | Public CMS thumbnail images | 5 MB |
| `App_Data/patient-documents/` | Private patient documents served only through authorized controller actions | 10 MB |

Patient document storage can be moved outside the application directory with `PatientDocuments:StorageRoot`. The storage service uses generated file names, validates size, extension, declared content type, and file signature, and does not expose the private storage directory through static-file middleware. Legacy `/uploads/patient-documents/` records remain readable during migration, but new files use private storage.

Allowed file types:
- **Post thumbnails**: `.jpg`, `.jpeg`, `.png`, `.webp`
- **Patient uploads**: `.pdf`, `.jpg`, `.jpeg`, `.png`, `.doc`, `.docx`
- **Admin patient-document uploads**: `.pdf`, `.jpg`, `.jpeg`, `.png`, `.webp`

> Uploaded files are excluded from source control. Production backup and restore plans must cover both SQL Server records and private document storage.

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
| `/Donation`                    | Donation receipt page              |
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
- File uploads validate both extension and MIME type server-side.
- Patients cannot access other patients' documents — the portal filters by the authenticated user's profile ID.
- Cookie security is set to `HttpOnly`, `Secure`, and `SameSite=Lax`.
- Account lockout is enabled (5 failed attempts, 15-minute lockout).
- Production HSTS is enabled in `Program.cs`; SSL/TLS certificates remain a hosting responsibility.
- Security headers are applied globally: `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, and Content Security Policy directives for the application's current script, style, font, image, map, and realtime dependencies.
- Alpine.js is currently loaded from jsDelivr. Its browser behavior under the deployed CSP remains a staging/browser verification item; self-hosting or the CSP-compatible Alpine build is the preferred hardening direction.
- Backup and recovery are operational deployment requirements. Back up SQL Server, private patient-document storage, and public CMS uploads before production launch.

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
| Connection string             | LocalDB                         | Environment variable or key vault  |
| Admin credentials             | From `appsettings.json`         | From environment variables         |

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

The default `IPaymentGateway` implementation is `MockPaymentGateway`, which records sandbox-approved transactions only.
Sandbox payments are clearly marked in the user flow, receipts, admin views, and email receipt content.

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

Paystack is the configured production-gateway implementation. It must remain disabled until sandbox initialization, callback, signed webhook, receipt, and failure paths are verified with owner-controlled credentials.

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

Run the non-smoke suite with:

```bash
./scripts/verify-backend.sh
```

Run hosted smoke tests, which start the application in `Testing` mode on port `5187`, with:

```bash
RUN_SMOKE=1 ./scripts/verify-backend.sh
```

Plain `dotnet test` includes externally hosted smoke tests and should only be used when `OKAFOR_BASE_URL` points to a running application. The suite covers scheduling, payment mapping, notifications, WhatsApp, PWA behavior, accessibility assertions, controllers, services, and critical HTTP routes.

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
