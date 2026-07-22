# Azure Backup, Restore, And Disaster-Recovery Runbook

Last updated: 2026-07-20

This application has three persistent data sets that must be protected together:

| Data set | Azure service | Application path |
|---|---|---|
| Application, Identity, payment, and document metadata | Azure SQL Database | `ConnectionStrings:DefaultConnection` |
| Private patient documents and Data Protection keys | Azure Files | `/data` |
| Public CMS thumbnails | Azure Files | `/app/wwwroot/uploads` |

A SQL-only backup is incomplete. Database document metadata without its file, or
a restored file without matching authorization metadata, is not a valid recovery.

Microsoft references:

- [Azure SQL automated backups](https://learn.microsoft.com/azure/azure-sql/database/automated-backups-overview)
- [Restore Azure SQL Database from backup](https://learn.microsoft.com/azure/azure-sql/database/recovery-using-backups)
- [Azure Files backup](https://learn.microsoft.com/azure/backup/azure-file-share-backup-overview)
- [Restore Azure Files](https://learn.microsoft.com/azure/backup/restore-afs)

## Required Protection Before Launch

- Confirm Azure SQL point-in-time restore retention and backup redundancy. Record
  the chosen retention; do not assume the default meets hospital policy.
- Configure Azure Backup for every mounted Azure Files share.
- Enable Azure Files soft delete and protect the storage account/resource group
  from accidental deletion.
- Configure private alerts for failed SQL/File backup jobs.
- Restrict backup and restore permissions to named operators.
- Agree recovery point objective (RPO), recovery time objective (RTO), retention,
  incident contacts, and deletion/legal-hold rules with the owner/privacy adviser.
- Complete one isolated restore drill before Production approval.

Long-term retention is a separate owner/privacy decision and is not enabled by
the application.

## Pre-Deployment Recovery Point

Before a Production deployment that changes schema, storage, or file behavior:

1. Record the current UTC time, database name, migration ID, file-share names,
   active revision, image digest, and operator.
2. Prevent or minimize writes during the coordinated recovery-point window. If
   writes continue, record that SQL and file recovery points may not represent
   one exact transaction boundary.
3. Verify the Azure SQL database is inside its point-in-time retention window.
4. Start on-demand Azure Files backup jobs for `/data` and CMS uploads.
5. Wait for both file backup jobs to succeed and record their job/recovery-point
   identifiers.
6. Record the intended Azure SQL restore time in UTC after the last accepted
   write and before migration execution.
7. Do not proceed if any protection step fails.

Azure SQL Database manages its backups. Do not use `BACKUP DATABASE ... TO DISK`
or copy a local `.bak` file for this Azure SQL path.

## Recovery Decision Matrix

| Incident | First response | Data action |
|---|---|---|
| New application revision is unhealthy; schema remains compatible | Route traffic to the previous revision | None |
| Secret/provider configuration is wrong | Route traffic back or restore the last known-good configuration | None unless data was mutated |
| Migration fails before completion | Stop promotion; preserve logs; assess migration state | Do not guess or run a down migration |
| Application writes corrupt SQL data | Stop writes and identify the last safe UTC time | Restore Azure SQL to a new database |
| Patient/CMS files are deleted or corrupted | Stop affected writes | Restore files/folders to an alternate location first |
| SQL metadata and files are inconsistent | Stop writes and coordinate both restore points | Restore both data sets, then verify ownership links |
| Credentials may be compromised | Disable/rotate credentials and preserve audit evidence | Restore only if data integrity was affected |

## Azure SQL Point-In-Time Restore

Azure SQL point-in-time restore creates a new database and cannot overwrite the
source database.

1. Stop application writes and record the incident timeline in UTC.
2. Select the last known-good restore time within the configured retention.
3. Restore to a new, clearly named database such as
   `OkaforHospital-restore-YYYYMMDDHHMM`.
4. Wait for the Azure operation to complete; restoration time varies with size
   and activity.
5. Connect through a restricted operator path and verify without exporting
   patient data:
   - Latest migration ID is compatible with the selected application image.
   - Identity roles and the expected admin account exist.
   - Record counts are plausible for appointments, teleconsultations, profiles,
     documents, messages, donations, and payments.
   - No post-incident records appear beyond the accepted recovery point.
6. Point an isolated, zero-traffic revision at the restored database.
7. Use `/health/ready` and private workflow checks to validate it.
8. Switch Production configuration/traffic only after the recovery owner signs
   off.
9. Retain the original database read-only or isolated until evidence and the
   retention decision are complete.

Do not delete, rename, or overwrite the original database as the first recovery
step.

## Azure Files Restore

Restore to an alternate location first whenever practical so the current share
remains available for comparison and evidence.

1. Select a recovery point aligned as closely as possible with the SQL restore
   time.
2. Restore the full share or selected folders to an isolated alternate share.
3. Verify directory structure and file counts without opening unrelated patient
   documents.
4. Confirm the application identity can read/write the restored target.
5. For private documents, verify several authorized metadata-to-file links and
   confirm an unrelated patient remains denied.
6. For Data Protection keys, confirm expected keys exist before using the share;
   restoring an old key ring can affect cookies and antiforgery tokens.
7. Mount the restored share only on a zero-traffic revision and run controlled
   checks.
8. Promote the restored mount only after approval.

Do not copy private patient documents into `wwwroot`, local developer storage,
CI artifacts, tickets, or chat.

## Coordinated Recovery

When both SQL and files must be restored:

1. Keep public traffic stopped or on a safe maintenance response.
2. Restore SQL and each file share to alternate resources.
3. Select compatible recovery points and document any time difference.
4. Deploy the last compatible image digest as a zero-traffic revision.
5. Attach the restored database and file mounts.
6. Verify health, authorization boundaries, document links, admin access, and
   critical care workflows with providers disabled or sandboxed.
7. Record expected data loss against the agreed RPO.
8. Obtain recovery-owner approval before restoring traffic.
9. Monitor closely and preserve the old resources until incident closure.

## Mandatory Pre-Launch Restore Drill

The drill must use isolated Azure resources and production-equivalent access
controls. If real Production data is used, the drill environment is still a
Production-class data environment and must not be publicly accessible.

Record:

```text
Drill date/time UTC:
Operator and observer:
Source SQL database and restore time:
Restored SQL database:
Source/recovered file shares:
Application image digest:
Migration ID:
SQL restore duration:
File restore duration:
Application recovery duration:
Measured RPO:
Measured RTO:
Health/readiness result:
Admin access result:
Authorization/document-link result:
Critical workflow result:
Problems found:
Corrective actions and owners:
Owner approval:
Resource cleanup approval/status:
```

The drill is complete only when the restored application is usable, access
boundaries still hold, evidence is recorded without secrets/patient details, and
the owner confirms the result. Creating a restore resource alone is not a
successful recovery test.

## Incident Evidence Rules

- Use UTC timestamps.
- Record resource IDs, revision names, image digests, migration IDs, and job IDs.
- Never paste connection strings, credentials, document contents, or raw patient
  details into the incident record.
- Preserve relevant logs under access control.
- Record every traffic, secret, database, and mount change with operator and
  approver.
- Do not destroy original resources or recovery evidence until the incident and
  privacy owners authorize it.
