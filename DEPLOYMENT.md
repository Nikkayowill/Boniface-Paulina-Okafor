# Supabase + Render Deployment Runbook

Last updated: 2026-08-02

This is the active hosting path for the application: an ASP.NET Core Docker web
service on Render backed by hosted Supabase PostgreSQL. The repository includes a
`render.yaml` Blueprint, a production Dockerfile, PostgreSQL migrations, health
checks, and safe launch defaults.

## What Is Automated

- Render builds the included `Dockerfile` and starts the published application.
- The app binds to Render's `PORT` on `0.0.0.0`.
- `/health/live` is the Render liveness endpoint; `/health/ready` also verifies
  PostgreSQL, the seeded Admin account, and enabled patient-document storage.
- The free Blueprint enables single-instance startup migrations because Render
  does not provide pre-deploy commands or one-off jobs on its free web service.
- ASP.NET Data Protection keys are stored in PostgreSQL so logins survive a
  restart or scale-to-zero cycle.
- Online payments, donations, and patient-document uploads remain disabled until
  their live providers or persistent storage are explicitly configured.

## 1. Get The Correct Supabase Connection String

In the Supabase Dashboard, open the project and select **Connect**.

Use one of these exact dashboard-provided connection strings:

- **Direct connection, port 5432** when the host can reach IPv6. This is the best
  migration and persistent-backend connection.
- **Shared Pooler / Session mode, port 5432** when the host is IPv4-only. This is
  the practical Render connection and supports the application and migrations.

Do not use Transaction mode on port `6543` for this persistent ASP.NET service or
for EF migrations. Transaction mode is intended for short-lived/serverless work
and does not support prepared statements.

The session-pooler username has the form `postgres.<project-reference>`. That
username is how Supavisor identifies the tenant; do not replace it with plain
`postgres`, and do not add undocumented `external_id` or `sni_hostname` query
parameters. Copy the complete Session string from **Connect** so its host, region,
project reference, database, and password all match.

If the database password contains URL-reserved characters, use the already
encoded dashboard URL or URL-encode the password. The application accepts either
a `postgresql://...` URL or an Npgsql key/value connection string in
`DATABASE_URL` and always requires TLS for URL-form hosted connections.

Before deployment, verify the string from a machine that can reach the endpoint:

```bash
psql "$DATABASE_URL" -c 'select current_database(), current_user;'
```

Never commit the connection string or paste it into logs, issues, or screenshots.

## 2. Create The Render Service

1. Push the reviewed repository to GitHub or GitLab.
2. In Render, choose **New > Blueprint** and select the repository.
3. Render reads `render.yaml` and asks for each value marked `sync: false`.
4. Enter these secrets:

   - `DATABASE_URL`: the Supabase Direct or Session port-5432 string above.
   - `SeedAdmin__Email`: the real initial administrator email.
   - `SeedAdmin__Password`: a unique strong bootstrap password.

5. Create the Blueprint and watch the first deploy logs. The initial start applies
   the PostgreSQL baseline migration and seeds the Admin role/account.
6. Open `https://<service>.onrender.com/health/ready`. It must return HTTP 200.
7. Sign in as the seeded Admin, change the bootstrap password, and replace or
   remove `SeedAdmin__Password` in Render after access is confirmed.

`PORT` is assigned by Render; do not set it manually. Keep
`ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` so HTTPS redirects and generated links
honor Render's proxy headers.

## 3. First Hosted Verification

Run the non-destructive checks against the Render URL:

```bash
curl --fail --show-error https://<service>.onrender.com/health/live
curl --fail --show-error https://<service>.onrender.com/health/ready
OKAFOR_BASE_URL=https://<service>.onrender.com \
  dotnet test tests/Okafor.NET.Tests/Okafor.NET.Tests.csproj \
  --filter 'Category=Smoke'
```

Then verify the owner-visible workflows in `docs/VERIFICATION_CHECKLIST.md`. Use
fictional data during preview checks and do not test live payments against real
patient records.

## Free-Host Boundaries

The committed Blueprint uses Render's free web-service plan to make the first
hosted preview inexpensive. Its boundaries are important:

- The service sleeps after inactivity, so the first request can be slow.
- Its filesystem is ephemeral. Patient documents are therefore disabled, and
  CMS images uploaded at runtime can disappear after a restart or redeploy.
- In-process appointment reminders do not run while the service is asleep.
- Render free services block outbound SMTP ports, so SMTP confirmation and receipt
  mail require a paid service or a future HTTPS email-provider integration.
- Free hosting is suitable for a preview, not patient care or production health
  data. Confirm contracts, privacy obligations, retention, backups, monitoring,
  and paid service levels before accepting real patient information.

Supabase backups and point-in-time recovery depend on the selected Supabase plan.
Confirm and test the project's recovery capability before collecting real data.

## Paid/Production Migration Mode

For a paid Render web service, migrations should run before the new web instance
receives traffic:

1. Change the Blueprint plan to the selected paid instance.
2. Set `Database__ApplyMigrationsOnStartup=false`.
3. Add this Render pre-deploy command:

   ```bash
   dotnet Okafor-.NET.dll --migrate-db
   ```

4. Keep the web command as the Dockerfile default.

The migration command exits after `Database.MigrateAsync()`. Run only one
migration task at a time, and never delete or rewrite a migration already applied
to a shared hosted database.

## Supabase Security Boundary

The baseline migration enables PostgreSQL row-level security on all application
tables and defines no browser-facing policies, so Supabase's `anon` and
`authenticated` roles cannot access their rows. The ASP.NET backend owns database
access; browsers do not use Supabase Data API credentials.

After applying migrations to the real project, run Supabase's Security Advisor
and Performance Advisor. Any future migration that creates a table must keep this
same private-by-default boundary or add a reviewed RLS policy before browser/API
access is granted.

## Release And Rollback

Before each release:

```bash
dotnet build -m:1
dotnet test
dotnet list package --vulnerable --include-transitive
docker build --tag okafor-hospital:release .
```

Record the Git SHA, the last successful migration, the deployed image, and the
Supabase backup/recovery point. For an application-only failure with a compatible
schema, roll Render back to the previous healthy deploy. A schema/data incident
requires a coordinated Supabase restore; do not attempt to repair production by
removing an applied EF migration.
