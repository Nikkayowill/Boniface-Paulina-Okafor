# Project Showcase and LinkedIn Talking Points

Last updated: 2026-07-12

## One-Sentence Project Story

I am building and launch-hardening an offline-aware hospital web platform for a rural Nigerian care context, combining public access, appointment and teleconsultation workflows, patient records, staff operations, payments, notifications, and PWA resilience in one ASP.NET Core system.

## Problem and Product Context

The engineering decisions are shaped by a real operating environment:

- Patients may use mobile devices on slow or intermittent connections.
- A small hospital team needs workflows that are understandable, recoverable, and easy to operate.
- Appointments, teleconsultations, payments, messages, and uploaded documents cross public and private trust boundaries.
- Provider integrations can fail independently, so core records must remain usable when email, SMS, WhatsApp, push, or payment services are unavailable.
- Sensitive patient and operational data must not be cached broadly or written into logs.

## What the Platform Includes

- Public hospital pages, doctor directory, news, contact, search, appointment booking, teleconsultation intake, donations, and bill payments.
- ASP.NET Core Identity with `Admin`, `Staff`, and `Patient` roles.
- Patient portal for profiles, appointment history, calendar downloads, documents, messages, teleconsultations, and push preferences.
- Admin workflows for scheduling, patient profiles, documents, users, content, contact submissions, availability, and payment review.
- SQL Server and Entity Framework Core code-first migrations.
- SignalR updates for booking queues and patient status changes.
- Configurable Paystack, SMTP, WhatsApp Cloud API, Africa's Talking, and web-push integrations.
- PWA installation, offline public fallbacks, and explicit cache exclusions for private, admin, payment, upload, webhook, and realtime routes.
- Health checks, Sentry-ready observability, CI, repeatable verification scripts, and documented launch gates.

## Engineering Decisions Worth Discussing

### Offline without compromising private data

The service worker supports public/offline resources but excludes authenticated portal pages, admin pages, payments, uploads, webhooks, and SignalR traffic. The design treats “available offline” and “safe to cache” as separate decisions.

### Auditable workflows instead of destructive shortcuts

Appointment and teleconsultation lifecycles are being hardened around explicit statuses, slot-collision protection, record preservation, role checks, and deliberate notification failure handling. This is important because operational history matters more than a superficially simple CRUD flow.

### Provider failure does not erase core hospital state

The application persists core workflow state before optional notification delivery. Email, SMS, WhatsApp, and realtime failures are logged deliberately without turning a successfully saved care request into a failed request.

### Private document handling

New patient documents default to private `App_Data` storage and are downloaded through authorization-checked controller actions. Upload validation checks size, extension, declared content type, and file signature, and stored names are generated rather than trusted from the browser.

### Evidence-driven launch preparation

The repository separates automated tests, hosted smoke checks, SQL-backed development verification, browser QA, provider verification, and owner-only production decisions. A feature is not described as production-ready merely because its controller exists.

## Verified Evidence You Can Mention

- The solution builds with zero compiler warnings and errors at the latest recorded checkpoints.
- The current verification workflow contains more than 200 passing non-smoke and hosted smoke checks across the active launch branches.
- Testing-mode startup and critical HTTP/PWA routes are exercised through a hosted smoke workflow.
- SQL Server Development startup, migrations, health checks, seed behavior, patient registration, profile creation, booking, and teleconsultation submission have recorded local evidence.
- Tailwind assets are compiled locally rather than depending on a runtime styling CDN.
- Feature work is isolated by branch and reviewed with explicit build/test evidence before push.

Do not publicly claim that production deployment, live provider delivery, legal compliance, penetration testing, or final hospital approval is complete until those gates are actually confirmed.

## Skills Demonstrated

- ASP.NET Core MVC and Razor Views
- C# asynchronous application development
- Entity Framework Core and SQL Server
- ASP.NET Core Identity, role authorization, antiforgery, and secure cookies
- Progressive Web Apps and service-worker cache policy
- SignalR realtime updates
- Payment callback and signed-webhook design
- File-upload validation and private storage
- External provider integration and graceful degradation
- Automated tests, smoke tests, CI, health checks, and launch runbooks
- Product scoping for low-bandwidth and small-team operational environments

## Short LinkedIn Post Draft

I have been working on the launch hardening of an ASP.NET Core hospital platform designed for a rural Nigerian care context.

The interesting part has not been adding pages—it has been reasoning about the boundaries between public access, patient privacy, unreliable connectivity, staff operations, and external providers.

The platform includes appointment and teleconsultation workflows, a role-based patient/admin portal, private document handling, SQL Server persistence, SignalR updates, configurable payments and notifications, and a PWA layer that supports public offline access without broadly caching sensitive routes.

One principle has guided the work: code-present is not the same as launch-ready. I separated automated tests, hosted smoke checks, SQL-backed verification, browser QA, provider credentials, and owner approvals into explicit evidence gates. The active launch branches currently carry more than 200 passing automated and smoke checks.

This project has strengthened my experience with ASP.NET Core MVC, EF Core, Identity, PWA cache safety, realtime workflows, provider failure handling, and the less glamorous—but essential—work of preparing software for real operations.

#dotnet #aspnetcore #csharp #healthtech #webdevelopment #pwa #sqlserver #softwareengineering

## Interview Talking Points

1. Why the PWA caches public guidance but deliberately excludes private portal and payment traffic.
2. How unique slot constraints and reservation logic protect appointment capacity under concurrent booking.
3. Why notification failures are handled separately from successful database persistence.
4. How role and ownership checks protect patient documents and portal records.
5. Why provider credentials and browser behavior remain manual launch gates even with a green automated suite.
6. How feature inventories, recovery evidence, and small branches reduce hallucination and regression risk during AI-assisted development.

## Before Publishing Screenshots

- Use fictional seed data only.
- Hide email addresses, phone numbers, payment references, document names, and admin account details.
- Do not show secrets, connection strings, provider dashboards, or browser storage containing tokens.
- Prefer public pages, fictional booking examples, architecture diagrams, test summaries, or carefully sanitized admin screens.
- Describe the application as “in launch hardening” until production deployment and owner approval are complete.
