# Azure Deployment Runbook

Last updated: 2026-07-20

This is the release procedure for the Okafor Memorial Hospital application. It
targets the confirmed ASP.NET Core, Azure SQL Database, and Azure hosting stack.
It does not authorize a deployment; production promotion remains an owner-lane
decision.

Related documents:

- `docs/FREE_HOSTING_READINESS.md`
- `docs/BACKUP_RESTORE_RUNBOOK.md`
- `docs/SECRET_CONFIGURATION_RUNBOOK.md`
- `docs/VERIFICATION_CHECKLIST.md`

## Hosting Boundary

Use Azure App Service F1 only for an inexpensive public preview. The final
staging rehearsal must use the same hosting model selected for production.

The preferred launch model is:

- An immutable container image built from the included `Dockerfile` and tagged
  with the Git commit SHA.
- Azure Container Apps in multiple-revision mode for staging and production.
- Azure SQL Database for application and Identity data.
- Azure Files mounted at `/data` for private patient documents and Data
  Protection keys.
- A persistent Azure Files mount at `/app/wwwroot/uploads` for CMS uploads.
- Hosting secret storage or environment secrets for all credentials.
- HTTP startup/liveness probe at `/health/live` and readiness probe at
  `/health/ready`, targeting port `8080`.

Container-local storage is temporary. A revision must not receive traffic if
either persistent mount is absent or read-only.

Microsoft references:

- [Container Apps revisions and traffic splitting](https://learn.microsoft.com/azure/container-apps/traffic-splitting)
- [Container Apps health probes](https://learn.microsoft.com/azure/container-apps/health-probes)
- [Container Apps Azure Files mounts](https://learn.microsoft.com/azure/container-apps/storage-mounts)

## Stop Gates

Do not deploy when any applicable gate is unresolved:

- The release commit is not reviewed, committed, and green in required CI jobs.
- The target image is identified only by a mutable tag such as `latest`.
- Database migrations have not been reviewed for forward and rollback impact.
- Staging and production share a database, file share, or provider credentials.
- Azure SQL point-in-time retention is not confirmed.
- Azure Files backup is not configured for both persistent shares.
- The previous healthy container revision or image digest is unknown.
- Production secrets, final host name, TLS, monitoring, or admin access are
  unconfirmed.
- Production still contains unapproved demonstration clinicians, posts, or
  appointment data.

## Release Record

Create a private release record before deployment. Do not include secrets or
patient data.

```text
Release version/tag:
Git commit SHA:
Container image and digest:
Target environment:
Operator:
Approver:
Start time UTC:
Previous healthy revision:
Previous image and digest:
Azure SQL database name:
Migration before/after IDs:
Azure SQL pre-deploy recovery time UTC:
Azure Files backup job/restore-point IDs:
Known risks:
Rollback decision owner:
```

## Build And Test The Release Candidate

Run from a clean worktree. Smoke tests must use the repository harness because
the raw `dotnet test` command does not start the smoke-test host.

```bash
./scripts/verify-backend.sh
RUN_SMOKE=1 ./scripts/verify-backend.sh
./scripts/verify-database-integration.sh
./scripts/verify-e2e.sh
dotnet list package --vulnerable --include-transitive
dotnet ef migrations list
git status --short
git rev-parse HEAD
```

Build once and promote the same image digest through staging and production:

```bash
export IMAGE_TAG="$(git rev-parse --short=12 HEAD)"
docker build --pull --tag "$REGISTRY/okafor-hospital:$IMAGE_TAG" .
docker push "$REGISTRY/okafor-hospital:$IMAGE_TAG"
docker inspect --format='{{index .RepoDigests 0}}' "$REGISTRY/okafor-hospital:$IMAGE_TAG"
```

Record the digest. Do not rebuild between environments.

## Required Hosted Configuration

Configure values through Azure secrets/environment variables. Never edit a
deployed `appsettings` file or commit real values.

At minimum:

```text
ASPNETCORE_ENVIRONMENT=Staging or Production
ASPNETCORE_HTTP_PORTS=8080
ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
ConnectionStrings__DefaultConnection=<secret>
Authentication__RequireConfirmedAccount=true
SeedAdmin__Email=<secret/config>
SeedAdmin__Password=<secret, bootstrap only>
Email__SmtpHost=<secret/config>
Email__Port=587
Email__EnableSsl=true
Email__FromAddress=<verified sender>
Email__Username=<secret>
Email__Password=<secret>
PatientDocuments__StorageRoot=/data/patient-documents
DataProtection__KeysPath=/data/data-protection-keys
```

Provider keys are required only for integrations approved for that environment;
see `docs/SECRET_CONFIGURATION_RUNBOOK.md`. Production must not use `Mock`
payments or `Lean` notifications while the corresponding live feature is
advertised.

## Database Migration

The application deliberately migrates automatically only in Development.
Staging and Production require the explicit application command:

```bash
dotnet Okafor-.NET.dll --migrate-db
```

Run that exact image as a one-off, manually triggered Container Apps Job or an
equivalent controlled release task with access to the target Azure SQL Database.
The job must:

1. Use the candidate image digest and target environment secrets.
2. Pass `--migrate-db` to the existing image entry point.
3. Allow only one execution at a time.
4. Finish successfully before a candidate web revision receives traffic.
5. Store logs privately without connection strings or patient data.

Never run `dotnet ef database update` from an arbitrary workstation against
Production, remove an already-applied migration, or migrate two replicas at the
same time.

## Staging Rehearsal

1. Confirm staging has isolated SQL, Azure Files, secrets, and sandbox providers.
2. Confirm both file mounts are writable without printing their contents.
3. Run the migration job and record the final migration ID.
4. Deploy the candidate image as a new revision with zero public traffic.
5. Wait for `/health/live` and `/health/ready` to pass.
6. Use the revision-specific URL to run `docs/VERIFICATION_CHECKLIST.md`.
7. Verify admin login, appointments, teleconsultations, patient ownership,
   documents, messages, payments, provider failure paths, and PWA behavior.
8. Restart or replace the candidate replica and confirm patient documents remain
   available and existing authentication remains decryptable.
9. Shift staging traffic to the candidate and monitor logs, readiness, latency,
   and provider dashboards.
10. Record results and unresolved risks. Staging success does not itself approve
    Production.

## Production Promotion

1. Obtain explicit owner approval and identify the rollback decision owner.
2. Freeze schema and feature scope except for launch blockers.
3. Confirm the previous healthy revision is active and retained.
4. Complete the pre-deploy protection steps in
   `docs/BACKUP_RESTORE_RUNBOOK.md` and record their IDs/timestamps.
5. Confirm the release uses owner-approved clinicians, content, contact details,
   scheduling rules, privacy wording, and provider configuration.
6. Run the migration job with the already-tested image digest.
7. Deploy a new Production revision with zero traffic.
8. Wait for both health endpoints and review startup logs.
9. Perform a private revision check without creating real patient/payment data.
10. Move traffic to the candidate in a controlled step and monitor closely.
11. Complete final-domain checks for TLS, login/email confirmation, callbacks,
    webhooks, PWA, sitemap, and logout.
12. Record the outcome, end time, and post-deploy monitoring owner.

## Application Rollback

For an application-only defect with a compatible database, route 100 percent of
traffic back to the previous healthy revision. Do not rebuild the old commit and
do not mutate the database merely because application traffic moved.

Before declaring rollback complete:

1. Confirm `/health/live` and `/health/ready` on the previous revision.
2. Recheck the affected workflow.
3. Confirm no provider callbacks still target a disabled host or revision.
4. Preserve candidate logs and evidence privately.
5. Record who made the rollback decision and when.

If a migration is incompatible with the previous application or data is
corrupted, follow the coordinated restore path in
`docs/BACKUP_RESTORE_RUNBOOK.md`. Azure SQL restore creates a new database; it
does not overwrite the existing one.

## Post-Deploy Monitoring

For at least the agreed observation window, monitor:

- Container revision health and restart count.
- `/health/live` and `/health/ready` separately.
- HTTP 5xx rate and request latency.
- SQL connectivity, capacity, and free-offer usage.
- Azure Files capacity and backup job status.
- Failed login/lockout patterns without logging credentials.
- Appointment, teleconsultation, payment, email, WhatsApp, and push failures.
- Background reminder execution; scale-to-zero does not run hosted services.

Production approval, DNS cutover, provider activation, and deletion of an old
revision remain owner-controlled actions.
