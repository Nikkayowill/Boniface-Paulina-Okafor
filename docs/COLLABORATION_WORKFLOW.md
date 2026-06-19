# Collaboration Workflow

This repository keeps the ASP.NET Core backend as the system of record while giving frontend contributors a clear, low-friction design surface.

## Ownership Boundaries

Backend owns:

- Authentication and roles
- Admin and patient portal workflows
- Appointments, teleconsultations, donations, bill payments, uploads, and database writes
- Payment provider calls and webhook verification
- Email, SMS, WhatsApp Cloud API, push notifications, and other secrets
- EF Core models and migrations

Frontend/design owns:

- Public landing pages and content sections
- Layout, visual system, responsive behavior, and static assets
- PWA public shell/offline experience
- Client-side interaction for public forms, as long as secure writes still go through backend routes

## Practical Rules

- Do not put provider secret keys in JavaScript, static JSON, or public config files.
- Use backend routes for secure actions such as payments, patient data, admin actions, and notifications.
- Keep public frontend changes mostly in `Views/Home`, `Views/Shared`, `wwwroot/css`, `wwwroot/js`, and `wwwroot/images`.
- Keep admin and patient portal screens conservative and workflow-focused.
- If a frontend route or API shape changes, update `FRONTEND_BACKEND_INTEGRATION_CONTRACT.md`.

## Recommended Branch Flow

1. Create a short-lived feature branch.
2. Keep backend and frontend changes separated when possible.
3. Run project-level build commands on Linux if the repo path contains `&`.
4. Open a pull request with screenshots for public UI changes.
5. Wait for CI to pass before merging.

## Protected Main Branch

This repo uses Husky to block direct local pushes from or to `main` and `master`.

After cloning, install frontend dependencies once so Git hooks are activated:

```bash
npm install
```

Then create work branches before pushing:

```bash
git switch -c feature/public-home-redesign
git push -u origin feature/public-home-redesign
```

Husky is a local guardrail. GitHub branch protection should also be enabled for `main`/`master` so pull requests are required even if someone bypasses local hooks.

## Verification Commands

Preferred one-command backend verification:

```bash
./scripts/verify-backend.sh
```

```bash
$HOME/.dotnet/dotnet build tests/Okafor.NET.Tests/Okafor.NET.Tests.csproj
$HOME/.dotnet/dotnet test tests/Okafor.NET.Tests/Okafor.NET.Tests.csproj --filter "Category!=Smoke"
```

For smoke tests, start the app first:

```bash
ASPNETCORE_ENVIRONMENT=Testing ASPNETCORE_URLS=http://localhost:5187 \
  $HOME/.dotnet/dotnet run --project Okafor-.NET.csproj --no-launch-profile
```

Then run:

```bash
OKAFOR_BASE_URL=http://localhost:5187 \
  $HOME/.dotnet/dotnet test tests/Okafor.NET.Tests/Okafor.NET.Tests.csproj --filter "Category=Smoke"
```

The full feature inventory and recovery checklist live in:

- [`docs/FEATURE_INVENTORY.md`](FEATURE_INVENTORY.md)
- [`docs/FUNCTIONALITY_RECOVERY_PLAN.md`](FUNCTIONALITY_RECOVERY_PLAN.md)
- [`docs/VERIFICATION_CHECKLIST.md`](VERIFICATION_CHECKLIST.md)
