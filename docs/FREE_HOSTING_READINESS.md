# Free Custom-Domain Hosting — July 2026

## Locked Architecture

The launch hosting path is:

```text
Purchased domain
    -> Azure Container Apps Consumption
    -> Azure SQL Database free offer

GitHub
    -> GitHub Actions
    -> Azure Buildpacks source build
    -> Azure Container Apps revision
```

Azure App Service F1 is not the launch host because it cannot bind the purchased
custom domain. Local Docker and Docker Compose are not part of the deployment
workflow. Azure still runs an internally generated container image because that
is the Container Apps execution model.

## Cost Guardrails

The hosted-preview workflow refuses deployment unless:

- Azure SQL has `useFreeLimit` enabled.
- Azure SQL uses `AutoPause` when the monthly free allowance is exhausted.
- the Container Apps environment does not send logs to a billed Log Analytics workspace.
- the app scales from zero to one replica.
- the app uses 0.25 CPU and 0.5 GiB memory.
- background workers, patient documents, and bill payments remain disabled.
- payment checkout is the visibly labelled mock provider.

These controls target zero hosting charges but cannot promise that every Azure
account or future usage pattern will always cost zero. The owner must still
configure Azure budget alerts and inspect the Azure cost analysis page.

## Scale-to-Zero Safety

Authentication and antiforgery encryption keys are stored in Azure SQL through
ASP.NET Core Data Protection. They are not stored on ephemeral container disk,
so scaling to zero or deploying a new revision does not invalidate every active
session.

Background appointment reminders are disabled because an app at zero replicas
cannot run in-process timers. Patient uploads remain disabled because persistent
private file storage is not part of the free launch.

## GitHub Environment Contract

Create a protected GitHub environment named `hosted-preview`.

Secrets:

```text
AZURE_CLIENT_ID
AZURE_TENANT_ID
AZURE_SUBSCRIPTION_ID
AZURE_SQL_CONNECTION_STRING
SEED_ADMIN_EMAIL
SEED_ADMIN_PASSWORD
```

Variables:

```text
AZURE_RESOURCE_GROUP
AZURE_LOCATION
AZURE_CONTAINER_APP_ENVIRONMENT
AZURE_CONTAINER_APP
AZURE_SQL_SERVER
AZURE_SQL_DATABASE
CUSTOM_DOMAIN
```

`CUSTOM_DOMAIN` is optional until the domain is purchased. When present it must
be a `www` hostname, such as `www.okaformemorial.org`.

## First Hosted Preview

1. Create the Azure subscription and resource group.
2. Create a Consumption-only Container Apps environment with log destination `none`.
3. Create Azure SQL using the free offer and choose `AutoPause`.
4. Permit the Container App to connect to Azure SQL.
5. Create the `hosted-preview` GitHub environment and add the values above.
6. Merge the release commit into `launch/july31` or provide its exact branch/SHA.
7. Run **Hosted preview release** with:
   - schema compatibility confirmed;
   - custom-domain binding disabled;
   - confirmation `DEPLOY HOSTED PREVIEW`.
8. Use the generated Azure hostname to test the application before changing DNS.

The workflow applies pending migrations once, verifies readiness, disables
startup migration, restricts allowed hosts, and records release evidence.

## Domain Cutover

The first workflow run prints the exact CNAME and TXT verification records.
Create those records at the registrar as DNS-only records. Do not proxy the
`www` CNAME through Cloudflare or another intermediate service because Azure's
managed-certificate validation requires a direct CNAME.

After DNS propagates, rerun the workflow with custom-domain binding enabled.
Azure then binds the hostname, provisions its managed HTTPS certificate, and
verifies the domain health endpoint.

## Accepted Free-Tier Risks

- The first request after inactivity can be slow because the app scales from zero.
- Azure SQL can become unavailable until the next month if its free allowance is exhausted.
- The free SQL offer has no service-level agreement.
- In-process reminders do not run while the app has zero replicas.
- Paid monitoring, private file storage, and always-on compute are intentionally excluded.

## Official References

- [Azure Container Apps pricing](https://azure.microsoft.com/pricing/details/container-apps/)
- [Container Apps custom domains and free managed certificates](https://learn.microsoft.com/azure/container-apps/custom-domains-managed-certificates)
- [Container Apps code-to-cloud options](https://learn.microsoft.com/azure/container-apps/code-to-cloud-options)
- [Azure SQL free-offer FAQ](https://learn.microsoft.com/azure/azure-sql/database/free-offer-faq)
- [App Service custom-domain requirements](https://learn.microsoft.com/azure/app-service/manage-custom-dns-buy-domain)
