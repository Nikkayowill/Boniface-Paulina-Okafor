# End-to-End Testing Methodology

## Purpose

The E2E suite proves a small number of launch-critical patient journeys through the real browser, ASP.NET Core middleware, Razor/JavaScript UI, EF Core SQL Server provider, and migrated SQL Server schema.

It does not call live email, WhatsApp, SMS, Paystack, or monitoring accounts. Those integrations remain in safe local modes until credentials are supplied and are verified separately in staging.

## Test Pyramid

1. Unit tests cover isolated rules and edge cases quickly.
2. SQL Server integration tests cover transactions, constraints, and data workflows without a browser.
3. E2E tests cover only critical cross-layer journeys in a real browser.
4. Staging checks verify real provider credentials, callbacks, DNS, TLS, and operational ownership.

Student Study Guide: putting every edge case in a browser suite makes feedback slow and fragile. E2E tests are most valuable when they prove that independently tested layers work together for the few journeys that must not fail at launch.

## Architecture

- **xUnit** owns test discovery and the shared collection lifecycle.
- **Playwright Chromium** drives semantic labels, roles, and visible UI instead of implementation-specific timing sleeps.
- **Kestrel** exposes the real ASP.NET application on a dynamic local port.
- **Testcontainers SQL Server** creates an isolated production-provider database.
- **EF Core migrations** create the schema exactly as application releases do.
- **Respawn** clears business and identity data between scenarios while preserving `__EFMigrationsHistory`.
- A new browser context gives every scenario fresh cookies, storage, and cache state.
- Failed scenarios save a screenshot and trace under `artifacts/e2e`; successful scenarios discard traces.

Student Study Guide: the SQL container and browser are expensive to start, so the suite shares those processes. Respawn and browser contexts reset the mutable state between tests, preserving isolation without paying the full startup cost for every scenario.

## Current Critical Journeys

1. A mobile patient selects a department and doctor, loads real availability, books a slot, receives the success UI, and leaves matching appointment and reserved-slot rows in SQL Server.
2. A mobile visitor opens the responsive navigation, uses scoped hospital search, and finds a seeded doctor.
3. A mobile visitor opens Father Toochukwu's provider profile and enters a teleconsultation form with his specialty and provider selection preserved.

The appointment test verifies both the visible result and the database post-condition. A green success message alone is not enough evidence that the workflow persisted correctly.

## Local Execution

Requirements:

- .NET 10 SDK
- Docker daemon accessible to the current user
- Chromium installed for the Playwright version referenced by the E2E project

On Linux, build, install Chromium once, and run:

```bash
E2E_INSTALL_BROWSERS=1 ./scripts/verify-e2e.sh
./scripts/verify-e2e.sh
```

Student Study Guide: Playwright browser builds are versioned with the NuGet package. Re-run browser installation after upgrading Playwright so the driver and browser remain compatible.

To watch the browser locally:

```bash
HEADED=1 ./scripts/verify-e2e.sh
```

To redirect failure evidence:

```bash
E2E_ARTIFACTS_PATH=/tmp/okafor-e2e ./scripts/verify-e2e.sh
```

## API-Key Boundary

Before API keys are connected, E2E uses:

- mock payments;
- notification logging without SMTP delivery;
- disabled reminder and push-cleanup loops;
- fictional `.test` email addresses and telephone numbers;
- isolated SQL Server data that is deleted with the container.

After credentials are available, keep this deterministic suite unchanged. Add a small staging-only provider suite for Paystack sandbox callbacks/webhooks, SMTP delivery, WhatsApp templates/webhooks, Africa's Talking if retained, VAPID push, and error monitoring. Never run destructive provider checks against production patient records.

Student Study Guide: browser E2E and provider verification answer different questions. E2E asks whether our product layers work together; staging contract tests ask whether an external account and its current configuration work with us.

## Expansion Order

Add journeys in risk order:

1. Patient registration, confirmation, login, and password reset using a captured test-email transport.
2. Patient-owned profile, appointment, message, and private-document access.
3. Admin review and status transition for appointment and teleconsultation requests.
4. Mock bill-payment and donation recovery flows.
5. Authorization negatives proving one patient cannot read another patient's records.
6. Offline/PWA installation and logout cleanup in a dedicated service-worker-enabled environment.

Do not add broad screenshot snapshots as the primary assertion. Prefer role/label-based actions, visible outcomes, and database or provider post-conditions.
