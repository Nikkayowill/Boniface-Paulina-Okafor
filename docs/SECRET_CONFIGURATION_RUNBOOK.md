# Secret Configuration Runbook

Issue: https://github.com/Nikkayowill/Boniface-Paulina-Okafor/issues/9

## Purpose

This runbook defines how local, staging, and production secrets should be handled before launch. It intentionally does not contain real credentials.

Rules:

- Do not commit real secrets.
- Do not paste secrets into GitHub issues, PRs, screenshots, logs, or chat.
- Use ASP.NET Core user secrets for local developer secrets.
- Use hosting/GitHub environment secrets for staging and production.
- Rotate any secret that has ever been committed, screenshotted, or pasted into a ticket.

## Local Development

Use user secrets for app-level local settings:

```bash
dotnet user-secrets set "SeedAdmin:Email" "admin@example-hospital.local"
dotnet user-secrets set "SeedAdmin:Password" "<local-strong-password>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=OkaforHospitalDb;User Id=sa;Password=<local-sql-password>;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

Use `.env` only for Docker Compose values such as the local SQL Server `SA_PASSWORD`. The committed `.env.example` should show required names without real values.

Never commit local `.env`, user secrets, database backups, exported logs with patient data, or uploaded patient files.

## Minimum Local Keys

These are required for a realistic SQL-backed local verification pass:

| Key | Required for local launch testing | Owner |
|---|---:|---|
| `ConnectionStrings:DefaultConnection` | Yes | Backend/DevOps |
| `SeedAdmin:Email` | Yes | Owner/Backend |
| `SeedAdmin:Password` | Yes | Owner |
| `Payments:Provider` | Yes, can be `Mock` locally | Backend/DevOps |
| `Notifications:Provider` | Yes, can be `Lean` locally | Backend/DevOps |
| `Hospital:Name` | Yes | Owner |
| `Hospital:Address` | Yes | Owner |
| `Hospital:Email` | Yes | Owner |
| `Hospital:EmergencyNumbers` | Yes | Owner |

## Production Secret Matrix

| Area | Keys | Launch status |
|---|---|---|
| Database | `ConnectionStrings:DefaultConnection` | Required |
| Seeded admin | `SeedAdmin:Email`, `SeedAdmin:Password` | Required before first production boot; rotate/remove seed password after admin access is confirmed |
| Paystack | `Payments:Provider`, `Payments:Paystack:PublicKey`, `Payments:Paystack:SecretKey`, `Payments:Paystack:BaseUrl` | Required if online payments are advertised at launch |
| Paystack webhook | Paystack dashboard webhook URL pointing to `/webhooks/paystack` | Required if Paystack is live |
| SMTP | `Email:SmtpHost`, `Email:Port`, `Email:EnableSsl`, `Email:FromAddress`, `Email:Username`, `Email:Password` | Required if email receipts/notifications are advertised |
| WhatsApp Cloud API | `Notifications:WhatsApp:Enabled`, `Notifications:WhatsApp:PhoneNumberId`, `Notifications:WhatsApp:AccessToken`, `Notifications:WhatsApp:AppSecret`, `Notifications:WhatsApp:WebhookVerifyToken` | Required if WhatsApp API notifications/scheduling are advertised |
| WhatsApp templates | `Notifications:WhatsApp:LanguageCode`, `Notifications:WhatsApp:ReceivedTemplate`, `Notifications:WhatsApp:StatusTemplate` | Required for outbound templates |
| Public WhatsApp click-to-chat | `Notifications:WhatsAppNumber` | Required for public click-to-chat |
| Africa's Talking | `Notifications:AfricasTalking:ApiKey`, `Notifications:AfricasTalking:Username`, `Notifications:AfricasTalking:SenderId` | Optional unless SMS is in launch scope |
| Browser push | `VapidKeys:PublicKey`, `VapidKeys:PrivateKey`, `VapidKeys:Subject` | Required if push notifications are in launch scope |
| Monitoring | `SENTRY_DSN` or `Sentry:Dsn` | Strongly recommended |
| Scheduling AI | `SchedulingAi:Endpoint`, `SchedulingAi:ApiKey`, `SchedulingAi:Model` | Optional; app has fallback parsing |

## Safe Validation Checklist

Use this checklist without printing secret values:

1. Confirm `dotnet user-secrets` is configured for the project.
2. Confirm Docker SQL Server starts with the `.env` values.
3. Start the app in `Development` mode.
4. Confirm migrations run without errors.
5. Confirm `/health` returns `Healthy`.
6. Confirm admin seed email/password are set by signing in manually. Do not paste the password anywhere.
7. Confirm provider integrations in sandbox dashboards, not in GitHub comments.

Week 1 evidence already recorded in `docs/RECOVERY_STATUS.md`:

- Docker SQL Server container starts and reports healthy.
- Development app connects to SQL Server.
- Migrations report no pending updates.
- `/health` returns `Healthy`.

## Blocked Until Owner Confirms

These are owner-lane items and should stay open until the owner confirms them:

- Final production admin email.
- Production admin bootstrap password.
- Paystack sandbox/live account access.
- WhatsApp Business/Meta app access.
- SMTP provider account access.
- Africa's Talking account access, if SMS stays in launch scope.
- VAPID keys, if browser push stays in launch scope.
- Final production phone numbers and public WhatsApp number.
- Privacy notice and patient data handling wording.

## Issue 9 Closure Criteria

Issue #9 can close when:

- Local SQL-backed app startup is verified.
- Local seeded admin login is verified under issue #10.
- Production secret matrix is documented.
- Owner has confirmed which provider secrets are available and which are deferred.
- No real secrets are committed or pasted into GitHub.

