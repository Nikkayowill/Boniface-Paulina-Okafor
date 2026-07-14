# Environment Variables And Secrets

Do not commit real credentials. Local secrets should live in user secrets, shell environment variables, `.env`, or deployment secret storage.

For launch ownership, provider status, and issue #9 closure rules, see `docs/SECRET_CONFIGURATION_RUNBOOK.md`.

ASP.NET Core maps double underscores to nested config keys. For example:

```bash
ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=OkaforHospitalDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

PowerShell:

```powershell
$env:ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=OkaforHospitalDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

## Required For Local SQL Server Development

| Key | Purpose | Local Example |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | EF Core SQL Server connection | `Server=localhost,1433;Database=OkaforHospitalDb;User Id=sa;Password=<password>;TrustServerCertificate=True;MultipleActiveResultSets=true` |
| `SeedAdmin__Email` | Seeded admin email | `admin@example-hospital.local` |
| `SeedAdmin__Password` | Seeded admin password | Use a strong local-only password |

Docker Compose reads these from `.env`:

| Key | Purpose |
|---|---|
| `SA_PASSWORD` | SQL Server `sa` password |
| `ACCEPT_EULA` | Required by Microsoft SQL Server image |

## Notifications

| Key | Purpose | Local Default |
|---|---|---|
| `Notifications__Provider` | Notification routing mode: `Lean`, `AfricasTalking`, `Composite`, `Auto` | `Lean` |
| `Notifications__AdminEmail` | Admin notification recipient | `admin@okaformemorial.org` |
| `Notifications__AdminPhone` | Admin SMS/WhatsApp recipient | Placeholder |
| `Notifications__HospitalPhone` | Public hospital phone | `112` |
| `Notifications__WhatsAppNumber` | Click-to-chat widget number | Placeholder |

## WhatsApp Cloud API

Required only for live WhatsApp API/webhook testing:

| Key | Purpose |
|---|---|
| `Notifications__WhatsApp__Enabled` | `true`, `false`, or `Auto` |
| `Notifications__WhatsApp__ApiVersion` | Meta Graph API version |
| `Notifications__WhatsApp__PhoneNumberId` | WhatsApp business phone number id |
| `Notifications__WhatsApp__AccessToken` | Meta access token |
| `Notifications__WhatsApp__AppSecret` | Meta app secret for signature verification |
| `Notifications__WhatsApp__WebhookVerifyToken` | Webhook verification token |
| `Notifications__WhatsApp__LanguageCode` | Template language code |
| `Notifications__WhatsApp__ReceivedTemplate` | Request received template name |
| `Notifications__WhatsApp__StatusTemplate` | Status update template name |

## SMS: Africa's Talking

| Key | Purpose |
|---|---|
| `Notifications__AfricasTalking__ApiKey` | Africa's Talking API key |
| `Notifications__AfricasTalking__Username` | Africa's Talking username |
| `Notifications__AfricasTalking__SenderId` | Approved sender id |

## Push Notifications

| Key | Purpose |
|---|---|
| `VapidKeys__PublicKey` | Browser push public key |
| `VapidKeys__PrivateKey` | Browser push private key |
| `VapidKeys__Subject` | Contact subject, usually `mailto:...` |

## Payments

| Key | Purpose | Local Default |
|---|---|---|
| `Payments__Provider` | Payment provider: `Mock`, `Paystack`, `Auto` | `Mock` |
| `Payments__Mock__ReferencePrefix` | Mock reference prefix | `SANDBOX` |
| `Payments__Paystack__BaseUrl` | Paystack API URL | `https://api.paystack.co` |
| `Payments__Paystack__PublicKey` | Paystack public key | Secret |
| `Payments__Paystack__SecretKey` | Paystack secret key | Secret |
| `Payments__Paystack__WebhookUrl` | Paystack webhook route | `/webhooks/paystack` |

## Email

| Key | Purpose |
|---|---|
| `Email__SmtpHost` | SMTP host |
| `Email__Port` | SMTP port |
| `Email__EnableSsl` | `true` or `false` |
| `Email__FromAddress` | Sender address |
| `Email__Username` | SMTP username |
| `Email__Password` | SMTP password |

## Hosting and Persistent Storage

| Key | Purpose |
|---|---|
| `ASPNETCORE_HTTP_PORTS` | Container listening port; use `8080` for the included image |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | Honor managed reverse-proxy scheme/host headers; use `true` on Azure hosting |
| `PatientDocuments__StorageRoot` | Persistent, non-public patient-document directory |
| `DataProtection__KeysPath` | Persistent directory for cookie and antiforgery encryption keys |

The container defaults both private paths beneath `/data`. Mount persistent storage at `/data`. If CMS thumbnail uploads must survive container revisions, also mount persistent storage at `/app/wwwroot/uploads`.

Student Study Guide: ASP.NET Data Protection encrypts authentication cookies and antiforgery tokens. If every container restart creates new keys, existing cookies become unreadable and users are signed out. Persisting the key ring keeps encrypted application state valid across safe restarts.

## Background Tasks

| Key | Purpose | Default |
|---|---|---|
| `BackgroundTasks__AppointmentRemindersEnabled` | Enables the in-process appointment reminder loop | `true` |
| `BackgroundTasks__AppointmentReminderIntervalMinutes` | Minutes between reminder scans; values are constrained to 5–1440 | `60` |
| `BackgroundTasks__PushSubscriptionCleanupEnabled` | Enables daily removal of repeatedly failing push subscriptions | `true` |

These settings control whether a running application performs the jobs. They do not wake a sleeping or scale-to-zero host. If reminders must be guaranteed at a specific time, use always-on compute or move the job to an external scheduler.

Student Study Guide: an ASP.NET hosted service lives inside the web process. Configuration can turn it on or off, but it cannot run while that process is stopped. This is why hosting behavior is part of feature correctness for time-based work.

Brevo free-tier SMTP values:

```bash
Email__SmtpHost=smtp-relay.brevo.com
Email__Port=587
Email__EnableSsl=true
Email__FromAddress=info@okaformemorial.org
Email__Username=<brevo-smtp-login>
Email__Password=<brevo-smtp-key>
```

## Optional Error Monitoring

| Key | Purpose |
|---|---|
| `SENTRY_DSN` | Enables Sentry error tracking when set. Leave blank to disable. |
| `Sentry__Dsn` | Alternative ASP.NET Core nested config key for the same DSN. |

## Hospital Identity

| Key | Purpose |
|---|---|
| `Hospital__Name` | Public hospital name |
| `Hospital__Address` | Public address |
| `Hospital__Email` | Public email |
| `Hospital__EmergencyNumbers` | Public emergency numbers |
| `Hospital__GoogleMapEmbedUrl` | Public map iframe source |
