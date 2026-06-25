# Recovery Status

Last updated: 2026-06-25

This file records what has actually been verified in the current Linux workspace.

## Verified Today

| Area | Result | Evidence |
|---|---|---|
| Project restore | Passed | `./scripts/verify-backend.sh` |
| Backend/test build | Passed | `Okafor-.NET` and `Okafor.NET.Tests` built successfully |
| Non-smoke automated tests | Passed | 168 passed, 0 failed |
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

## Current Automated Baseline

```text
Non-smoke tests: 172 passed, 0 failed
Smoke tests:     20 passed, 0 failed
Total observed:  192 passed, 0 failed
```

Latest loop evidence:

- `docs/loop-runs/20260615T190947Z.md`
- `docs/loop-runs/20260615T191137Z.md`
- `docs/loop-runs/20260617T200804Z.md`
- `docs/loop-runs/20260617T201029Z.md`

Latest direct Week 1 baseline evidence:

- `./scripts/verify-backend.sh`: restore passed, build passed, 172 non-smoke tests passed.
- `RUN_SMOKE=1 ./scripts/verify-backend.sh`: restore passed, build passed, 172 non-smoke tests passed, Testing-mode app started at `http://localhost:5187`, 20 smoke tests passed.

## Not Fully Verified Yet

These require browser interaction, local credentials, or real provider credentials:

- Seeded admin login against SQL Server.
- Full appointment request to admin approval workflow.
- Full teleconsultation request to admin status update workflow.
- Patient registration, profile, documents, messages, and appointment cancellation.
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
