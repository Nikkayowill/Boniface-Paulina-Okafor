# Environment Variables And Secrets

Do not commit real credentials. Local secrets should live in user secrets, shell environment variables, `.env`, or deployment secret storage.

Rules:

- Do not commit real secrets.
- Do not paste secrets into GitHub issues, PRs, screenshots, logs, or chat.
- Use ASP.NET Core user secrets for local developer secrets.
- Use hosting/GitHub environment secrets for staging and production.
- Rotate any secret that has ever been committed, screenshotted, or pasted into a ticket.

ASP.NET Core maps double underscores to nested config keys. For example:

```bash
DATABASE_URL="postgresql://postgres:<password>@localhost:5432/okafor_hospital?sslmode=Disable"
```

PowerShell:

```powershell
$env:DATABASE_URL="postgresql://postgres:<password>@localhost:5432/okafor_hospital?sslmode=Disable"
```

## Required Database And Admin Settings

| Key | Purpose | Local Example |
|---|---|---|
| `DATABASE_URL` | EF Core PostgreSQL connection; use Supabase Direct or Session port 5432 when hosted | `postgresql://postgres:<password>@localhost:5432/okafor_hospital?sslmode=Disable` |
| `SeedAdmin__Email` | Seeded admin email | `admin@example-hospital.local` |
| `SeedAdmin__Password` | Seeded admin password | Use a strong local-only password |

Docker Compose reads these from `.env`:

| Key | Purpose |
|---|---|
| `POSTGRES_DB` | Local PostgreSQL database name |
| `POSTGRES_USER` | Local PostgreSQL role |
| `POSTGRES_PASSWORD` | Local PostgreSQL password; URL-encode it in `DATABASE_URL` |

## Notifications

| Key | Purpose | Local Default |
|---|---|---|
| `Notifications__Provider` | Notification routing mode: `Lean`, `AfricasTalking`, `Composite`, `Auto` | `Lean` |
| `Notifications__AdminEmail` | Admin notification recipient | `admin@okaformemorial.org` |
| `Notifications__AdminPhone` | Admin SMS recipient | Placeholder |
| `Notifications__HospitalPhone` | Public hospital phone | `112` |
| `Notifications__WhatsAppNumber` | Click-to-chat widget number | Placeholder |

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
| `Payments__Provider` | Payment provider: `Disabled`, `Mock`, `Auto` | `Mock` |
| `Payments__Mock__ReferencePrefix` | Mock reference prefix | `SANDBOX` |

No live online payment provider is wired up yet; `Payments__Provider` only
supports `Disabled` and `Mock` today.

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
| `PORT` | Host-assigned listening port; Render supplies this automatically |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | Honor the hosting reverse proxy's scheme/host headers; use `true` on Render |
| `Database__ApplyMigrationsOnStartup` | Free single-instance preview only; paid hosting should use an explicit pre-deploy migration command |
| `PatientDocuments__StorageRoot` | Persistent, non-public patient-document directory |
| `PatientDocuments__PersistentStorageConfirmed` | Set `true` only after the host volume is mounted persistently |
| `LaunchFeatures__PatientDocuments` | Set `true` to expose uploads after persistent storage is confirmed |
| `DataProtection__PersistKeysToDatabase` | Persist cookie and antiforgery keys in PostgreSQL; use `true` on the scale-to-zero hosted preview |
| `DataProtection__KeysPath` | Optional filesystem key directory for a host with a confirmed persistent volume |

The hosted preview keeps patient-document uploads disabled and stores Data
Protection keys in Supabase PostgreSQL. It therefore does not require a persistent app volume.
Do not enable patient uploads until private durable storage is provisioned and
`PatientDocuments__PersistentStorageConfirmed=true`.

ASP.NET Data Protection encrypts authentication cookies and antiforgery tokens. If
every scale-from-zero restart creates new keys, existing cookies become unreadable
and users are signed out. Persisting the key ring keeps encrypted application state
valid across safe restarts.

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
| `Sentry__Debug` | Enables verbose Sentry SDK diagnostics only when troubleshooting; keep `false` for routine development and production logs. |

## Hospital Identity

| Key | Purpose |
|---|---|
| `Hospital__Name` | Public hospital name |
| `Hospital__Address` | Public address |
| `Hospital__Email` | Public email |
| `Hospital__EmergencyNumbers` | Public emergency numbers |
| `Hospital__GoogleMapEmbedUrl` | Public map iframe source |

## Ownership And Launch Requirements

These keys are required for a realistic PostgreSQL-backed local verification pass:

| Key | Required For Local Launch Testing | Owner |
|---|---:|---|
| `DATABASE_URL` | Yes | Backend/DevOps |
| `SeedAdmin__Email` | Yes | Owner/Backend |
| `SeedAdmin__Password` | Yes | Owner |
| `Payments__Provider` | Yes, can be `Mock` locally | Backend/DevOps |
| `Notifications__Provider` | Yes, can be `Lean` locally | Backend/DevOps |
| `Hospital__Name` | Yes | Owner |
| `Hospital__Address` | Yes | Owner |
| `Hospital__Email` | Yes | Owner |
| `Hospital__EmergencyNumbers` | Yes | Owner |

Production launch status by area:

| Area | Keys | Launch Status | Owner |
|---|---|---|---|
| Database | `DATABASE_URL` | Required; store the Supabase Direct or Session port-5432 string only in the hosting secret manager | Backend/DevOps |
| Seeded admin | `SeedAdmin__Email`, `SeedAdmin__Password` | Required before first production boot; rotate/remove seed password after admin access is confirmed | Owner |
| Online payments | `Payments__Provider` | No live provider is implemented yet; keep `Disabled` (or `Mock` for demos) until one is added | Owner |
| SMTP | `Email__SmtpHost`, `Email__Port`, `Email__EnableSsl`, `Email__FromAddress`, `Email__Username`, `Email__Password` | Required if email receipts/notifications are advertised | Owner |
| Public WhatsApp click-to-chat | `Notifications__WhatsAppNumber` | Required for public click-to-chat | Owner |
| Africa's Talking | `Notifications__AfricasTalking__ApiKey`, `Notifications__AfricasTalking__Username`, `Notifications__AfricasTalking__SenderId` | Optional unless SMS is in launch scope | Owner |
| Browser push | `VapidKeys__PublicKey`, `VapidKeys__PrivateKey`, `VapidKeys__Subject` | Required if push notifications are in launch scope | Owner |
| Monitoring | `SENTRY_DSN` or `Sentry__Dsn` | Strongly recommended | Backend/DevOps |

Owner-lane items that stay open until the owner confirms them: final production admin email/password, SMTP provider account access, Africa's Talking account access (if SMS stays in scope), VAPID keys (if push stays in scope), final production phone numbers and public WhatsApp number, and privacy/patient-data-handling wording.

### Safe Validation Checklist

Use this checklist without printing secret values:

1. Confirm `dotnet user-secrets` is configured for the project.
2. Confirm Docker PostgreSQL starts with the `.env` values.
3. Start the app in `Development` mode.
4. Confirm migrations run without errors.
5. Confirm `/health` returns `Healthy`.
6. Confirm admin seed email/password are set by signing in manually. Do not paste the password anywhere.
7. Confirm provider integrations in sandbox dashboards, not in GitHub comments.

Docker Compose remains an optional local PostgreSQL tool; it is not part of the hosted deployment path. Render builds the hosted preview from the Dockerfile — see [`DEPLOYMENT.md`](../DEPLOYMENT.md).
