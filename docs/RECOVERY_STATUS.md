# Recovery Status

Last updated: 2026-07-20

This file records what has actually been verified in the current Linux workspace.

## Latest Verified Evidence

| Area | Result | Evidence |
|---|---|---|
| Project restore | Passed | `./scripts/verify-backend.sh` |
| Backend/test build | Passed | `Okafor-.NET` and `Okafor.NET.Tests` built successfully |
| Historical non-smoke baseline | Passed | 168 passed, 0 failed at the original recovery checkpoint |
| Testing-mode app startup | Passed | Smoke verifier started app at `http://localhost:5187` |
| HTTP smoke tests | Passed | 20 passed, 0 failed |
| Tailwind CSS build | Passed | `npm run build:css` completed |
| Linux `dotnet` discovery | Passed | `verify-backend.sh` falls back to `$HOME/.dotnet/dotnet` |
| Docker SQL Server startup | Passed | `docker compose up -d` started `okafor-mssql` |
| Development SQL connection | Passed | App connected to `localhost:1433` |
| EF Core migrations | Passed | Database reported no pending migrations |
| Development app startup | Passed | App listened on `http://localhost:5187` |
| Functionality loop script | Passed | `./scripts/functionality-loop.sh` created evidence logs |
| Docker SQL healthcheck | Passed | `okafor-mssql` reports healthy after compose healthcheck fix |
| Public page smoke coverage | Passed | About, Services, News, and Contact load in the smoke suite |
| PWA asset smoke coverage | Passed | `offline.html`, `offline-appointments.html`, `site.webmanifest`, and `service-worker.js` load in the smoke suite |
| WhatsApp floating widget smoke coverage | Passed | Home page smoke test verifies the floating WhatsApp link renders |
| Week 1 launch baseline restore/build/tests | Passed | `./scripts/verify-backend.sh` |
| Week 1 launch baseline smoke tests | Passed | `RUN_SMOKE=1 ./scripts/verify-backend.sh` |
| Payment verification cleanup | Passed | `PaymentVerificationApplicatorTests` included in non-smoke suite |
| Week 1 SQL Server container health | Passed | `docker compose up -d`, `docker compose ps` showed `okafor-mssql` healthy |
| Week 1 Development app startup | Passed | `ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5190 dotnet run --no-launch-profile` |
| Week 1 Development health check | Passed | `curl -fsS http://localhost:5190/health` returned `Healthy` |
| Week 1 seeded admin existence | Passed | `./scripts/check-seeded-admin.sh` confirmed an Admin role assignment exists without printing secrets |
| Patient email confirmation gate | Passed | SQL-backed curl flow confirmed unconfirmed login is blocked and confirmed login succeeds |
| Patient self-registration role assignment | Passed | Fresh self-registered patient could access `/Portal/*` after confirmation |
| Patient profile creation | Passed | SQL-backed curl flow created a patient profile after confirmed login |
| In-person appointment booking | Passed | SQL-backed curl flow booked a live slot through `/AppointmentRequests/BookSlot` |
| Teleconsultation phone-call removal | Passed | Live POST with `ConsultationType=Phone` was rejected; form offers video/follow-up only |
| Video teleconsultation request | Passed | SQL-backed curl flow submitted video request and loaded submitted page |
| Patient portal history pages | Passed | Confirmed patient could load appointment, teleconsultation, and documents pages |
| July 12 solution/project build | Passed | App and test projects built with 0 warnings and 0 errors |
| July 12 non-smoke automated tests | Passed | 182 passed, 0 failed |
| July 12 hosted smoke tests | Passed | Testing-mode app started at `http://localhost:5187`; 20 passed, 0 failed |
| July 12 Tailwind CSS build | Passed | `npm run build:css` completed successfully |
| July 12 JavaScript syntax check | Passed | Node parsed every first-party file in `wwwroot/js` successfully |
| July 18 non-smoke baseline | Passed | `RUN_SMOKE=0 ./scripts/functionality-loop.sh`: 227 passed, 0 failed before this pass |
| July 18 SQL Server integration baseline | Passed | `./scripts/verify-database-integration.sh`: 32 passed, 0 failed |
| Production demo-data guard | Passed | `DemoDataSeedTests` restrict fictional clinical, news, and appointment seeds to Development by default, with an explicit `DemoData:Enabled` opt-in that `Production`/`E2E`/`Testing` still refuse |
| July 20 functionality baseline | Passed | `RUN_SMOKE=0 ./scripts/functionality-loop.sh`: build passed; 232 passed, 0 failed |
| July 20 hosted smoke baseline | Passed | `RUN_SMOKE=1 ./scripts/verify-backend.sh`: build passed; 232 non-smoke and 20 smoke tests passed |
| July 25 launch-checklist hardening | Passed | `RUN_SMOKE=1 ./scripts/verify-backend.sh`: build passed; 266 non-smoke and 20 smoke tests passed (net -42 vs. July 20: four PWA/accessibility/responsive test files that asserted hardcoded literals against themselves were rewritten to check the real shipped files, which removed dozens of tautological `[Theory]` cases; net +48 real tests were added across the rewrite plus new coverage for `/Admin/Availability`, the homepage care-team section, and real CSP header values) |
| Deployment and recovery runbooks | Documented | Azure revision rollback, Azure SQL point-in-time restore, coordinated Azure Files recovery, and drill evidence are defined |

## Current Automated Baseline

```text
Non-smoke, non-container tests: 266 passed, 0 failed
Smoke tests:                    20 passed, 0 failed
SQL Server integration tests:  32 passed, 0 failed (last measured July 18; not re-run this pass)
Browser E2E journeys:            3 passed, 0 failed (last measured July 18; not re-run this pass)
```

The counts above describe this branch's latest recorded verification (2026-07-25). Feature branches may add coverage; each pull request should report its own build and test evidence rather than silently overwriting historical results. The non-smoke/smoke counts were re-run and confirmed on 2026-07-25; the SQL Server integration and E2E counts are carried over from the July 18 baseline and should be re-verified against a live SQL Server/browser environment before relying on them.

Latest loop evidence:

- `docs/loop-runs/20260615T190947Z.md`
- `docs/loop-runs/20260615T191137Z.md`
- `docs/loop-runs/20260617T200804Z.md`
- `docs/loop-runs/20260617T201029Z.md`
- `docs/loop-runs/20260715T183629Z.md`
- `docs/loop-runs/20260718T203837Z.md`
- `docs/loop-runs/20260720T033712Z.md`
- `docs/loop-runs/20260725T145206Z.md`

Latest direct Week 1 baseline evidence:

- `./scripts/verify-backend.sh`: restore passed, build passed, 172 non-smoke tests passed.
- `RUN_SMOKE=1 ./scripts/verify-backend.sh`: restore passed, build passed, 172 non-smoke tests passed, Testing-mode app started at `http://localhost:5187`, 20 smoke tests passed.
- `docker compose up -d`: SQL Server container started.
- `docker compose ps`: `okafor-mssql` reported healthy.
- `ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5190 dotnet run --no-launch-profile`: app connected to SQL Server, migrations reported no pending updates, seed checks ran, and app listened on `http://localhost:5190`.
- `curl -fsS http://localhost:5190/health`: returned `Healthy`.
- `./scripts/check-seeded-admin.sh`: confirmed at least one SQL-backed user has the Admin role.
- `ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5191 dotnet run --no-launch-profile`: app connected to SQL Server, migrations reported no pending updates, and app listened on `http://localhost:5191`.
- SQL-backed curl flow: fresh patient registration, confirmation-link page, unconfirmed login rejection, confirmed login success, profile creation, in-person appointment slot booking, phone teleconsultation rejection, video teleconsultation submission, and portal history/document pages.

Latest July 12 baseline evidence:

- `dotnet build Okafor-.NET.csproj --no-restore`: passed with 0 warnings and 0 errors.
- `dotnet build tests/Okafor.NET.Tests/Okafor.NET.Tests.csproj --no-restore`: passed with 0 warnings and 0 errors.
- `dotnet test tests/Okafor.NET.Tests/Okafor.NET.Tests.csproj --no-build --filter "Category!=Smoke"`: 182 passed, 0 failed.
- `RUN_SMOKE=1 ./scripts/verify-backend.sh`: restore and build passed, 182 non-smoke tests passed, and 20 hosted smoke tests passed.
- `npm run build:css`: Tailwind CSS compiled successfully.
- `node --check` over first-party `wwwroot/js/*.js`: all files parsed successfully.

Latest July 18 baseline evidence:

- `RUN_SMOKE=1 ./scripts/verify-backend.sh`: build passed with 0 warnings and 0 errors, 232 non-smoke/non-container tests passed, and 20 hosted smoke tests passed.
- `./scripts/verify-database-integration.sh`: 32 SQL Server Testcontainers tests passed.
- `dotnet test`: 264 self-contained app tests and 3 browser E2E journeys passed; the raw command's 20 smoke cases require the repository smoke harness above to start the app.
- `DemoDataSeedTests`: environment-policy and placeholder-secret cases passed, proving fictional content seeds default to Development only and that the hosted preview (which runs as `Staging`) cannot publish demo clinicians, news, or appointment records.

## Not Fully Verified Yet

These require browser interaction, local credentials, or real provider credentials:

- Seeded admin browser login against SQL Server. SQL-backed Admin role assignment exists, but browser sign-in still needs the owner-controlled local password.
- Full appointment request to admin approval workflow.
- Full teleconsultation request to admin status update workflow.
- Patient document upload/delete, messages, and appointment cancellation.
- Mock donation and bill payment flows against SQL Server.
- Paystack sandbox payment flow and signed webhook.
- SMTP live email delivery.
- Africa's Talking SMS delivery.
- WhatsApp Cloud API outbound templates and live webhook conversation.
- Browser push notification delivery.
- Browser PWA install/offline checks.

## Next Verification Move

Set a local `SeedAdmin:Password` with user secrets, restart the app, then confirm the admin login. After that, walk `docs/VERIFICATION_CHECKLIST.md` from top to bottom. Move feature statuses in `docs/FEATURE_INVENTORY.md` from `Code-present` to `Verified` only after the checklist item passes.

```bash
dotnet user-secrets set "SeedAdmin:Password" "use-a-local-strong-password"
dotnet user-secrets set "SeedAdmin:Email" "admin@example-hospital.local"
```
