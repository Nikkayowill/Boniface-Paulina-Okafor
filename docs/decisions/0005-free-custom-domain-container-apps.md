# 0005: Free Custom-Domain Hosting on Container Apps

## Status

Accepted on 2026-07-24. Superseded 2026-09-01 — hosting moved to Supabase/Render; see `DEPLOYMENT.md` and `render.yaml` for the current setup. Kept for historical context.

## Context

The hospital needs a hosted preview before July 31, a purchased custom domain,
minimal operating cost, Azure SQL compatibility, and GitHub deployment without
requiring the owner to run Docker locally.

Azure App Service F1 cannot bind a custom domain. A paid App Service tier would
violate the zero-hosting-cost launch constraint.

## Decision

Use Azure Container Apps Consumption with scale-to-zero, Azure SQL Database's
free offer, GitHub Actions, and Azure Buildpacks source builds.

The initial deployment uses Azure's generated hostname. After testing, a `www`
domain is connected directly to Container Apps and secured with an Azure managed
certificate.

ASP.NET Core Data Protection keys are persisted in Azure SQL. Patient documents,
background reminders, online bill payment, and live donations remain disabled
until their external dependencies are explicitly approved and verified.

## Consequences

- No local Docker workflow is required.
- Azure still creates and runs an internal container image.
- Cold starts are accepted.
- In-process background work cannot be trusted while scaled to zero.
- A free-offer exhaustion event can make the database unavailable.
- Live donations require a later PayPal integration and separate launch approval.
