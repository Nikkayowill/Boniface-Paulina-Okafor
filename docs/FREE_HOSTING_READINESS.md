# Free Hosting Readiness — July 2026

## Decision

Use Azure because the application already targets ASP.NET Core and Azure SQL Server.

- **Free public preview:** Azure App Service F1 with its generated `azurewebsites.net` hostname plus an Azure SQL Database free-offer database. This does not replace the final production-like rehearsal.
- **Preferred custom-domain launch path:** Azure Container Apps Consumption with a free managed certificate, Azure SQL Database free offer, and a small persistent Azure Files mount for patient documents and data-protection keys.
- **Simplest paid launch fallback:** Upgrade App Service from F1 before attaching the custom domain.

The final staging rehearsal must use the same hosting model, persistent mounts,
health probes, and revision behavior selected for Production. Otherwise it cannot
prove deployment or rollback readiness.

Do not describe the production system as guaranteed zero-cost. A hospital workload needs persistent private files, dependable email, backups, monitoring, and time-based reminders. Some of those requirements can exceed free grants or require always-on compute.

## Verified 2026 Limits

- Azure App Service Free is shared compute intended for development/testing. Its documented quota includes 60 CPU minutes per day and 1 GB storage. A custom domain requires a paid App Service plan.
- Azure SQL's free offer currently provides up to 10 databases per subscription, each with 100,000 vCore-seconds, 32 GB data storage, and 32 GB backup storage per month. If configured to stop at the free limit, the database becomes unavailable until the next month after the allowance is exhausted.
- Azure Container Apps Consumption includes a monthly grant of 180,000 vCPU-seconds, 360,000 GiB-seconds, and two million requests, and can scale to zero.
- Azure Container Apps supports custom domains and automatically renewed managed certificates at no certificate charge.
- Container-local files are not an acceptable persistence plan. Private documents and data-protection keys need a persistent mount or external storage.

Official references:

- [Azure SQL free-offer FAQ](https://learn.microsoft.com/azure/azure-sql/database/free-offer-faq)
- [Azure SQL free offer](https://learn.microsoft.com/azure/azure-sql/database/free-offer)
- [Azure Container Apps pricing](https://azure.microsoft.com/pricing/details/container-apps/)
- [Container Apps custom domains and free managed certificates](https://learn.microsoft.com/azure/container-apps/custom-domains-managed-certificates)
- [App Service custom-domain requirements](https://learn.microsoft.com/azure/app-service/app-service-web-tutorial-custom-domain)
- [Azure service quotas](https://learn.microsoft.com/azure/azure-resource-manager/management/azure-subscription-service-limits)

## Required Hosted Settings

Store these as hosting secrets or environment variables, never in the repository:

```text
ASPNETCORE_ENVIRONMENT=Staging
ConnectionStrings__DefaultConnection=<azure-sql-connection-string>
Authentication__RequireConfirmedAccount=true
Email__SmtpHost=<smtp-host>
Email__Port=587
Email__EnableSsl=true
Email__FromAddress=<verified-sender>
Email__Username=<smtp-login>
Email__Password=<smtp-secret>
BackgroundTasks__AppointmentRemindersEnabled=true
BackgroundTasks__AppointmentReminderIntervalMinutes=60
BackgroundTasks__PushSubscriptionCleanupEnabled=true
PatientDocuments__StorageRoot=<persistent-private-path>/patient-documents
PatientDocuments__PersistentStorageConfirmed=true
LaunchFeatures__PatientDocuments=true
DataProtection__KeysPath=<persistent-private-path>/data-protection-keys
```

For a Linux container, also use:

```text
ASPNETCORE_HTTP_PORTS=8080
ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
```

Student Study Guide: environment variables separate deploy-time facts and secrets from compiled code. The same application build can run locally, in staging, or in production without committing passwords or rebuilding for each environment.

## Staging Sequence

1. Create the Azure SQL free-offer database and choose the limit behavior deliberately.
2. Restrict SQL firewall access; do not enable broad public access longer than setup requires.
3. Set deployment secrets.
4. Run the explicit migration command once against staging.
5. Deploy the application using the generated host name.
6. Check `/health/live` and `/health/ready`.
7. Complete the manual patient/admin/payment/PWA verification checklist.
8. Monitor free SQL usage and hosting quotas.

Student Study Guide: schema migration is a release step, not an accidental side effect of starting every production replica. Running it explicitly prevents two new instances from racing to change the same database.

## Domain Cutover at the End of July

1. Finish staging verification before changing DNS.
2. Create the custom-domain binding and managed certificate.
3. Add the provider-supplied DNS verification record.
4. Add the `www` CNAME or apex A record.
5. Keep HTTPS-only behavior enabled.
6. Update sitemap, structured data, Paystack callback/webhook settings, WhatsApp webhook settings, and public contact links to the final hostname.
7. Verify account confirmation, password reset, payment callbacks, webhooks, PWA installation, and logout on the final domain.

Student Study Guide: DNS points people to a host, while TLS proves the host's identity and encrypts traffic. A domain cutover is not complete until external providers also know the final callback URLs.

## Free-Tier Risks to Explain Clearly

- A sleeping or scale-to-zero app does not execute ASP.NET hosted reminder services.
- An exhausted SQL free allowance can make the application unavailable.
- Container restarts delete unmounted local files and invalidate cookies if data-protection keys are not persistent.
- Free staging quotas are not a capacity promise for clinical production traffic.
- Backups and recovery must include SQL data and private document storage.

## Owner Decisions Still Required

- Azure subscription and region.
- App Service paid upgrade versus Container Apps for the custom-domain launch.
- Persistent storage location and backup retention.
- SMTP, Paystack, WhatsApp, Africa's Talking, and VAPID credentials.
- Final domain and DNS access.
- Whether reminders must run while the web app has zero traffic.
- Production approval after staging rehearsal.
