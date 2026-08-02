# Local Windows Setup

Use this when a Windows teammate clones the repo.

## Required Tools

- .NET 10 SDK
- Git
- Node.js LTS and npm
- Docker Desktop, for the included PostgreSQL 16 container

## First Run

From the repo root:

```powershell
dotnet restore
npm install
npm run build:css
dotnet build Okafor-.NET.csproj
dotnet build .\tests\Okafor.NET.Tests\Okafor.NET.Tests.csproj
dotnet test .\tests\Okafor.NET.Tests\Okafor.NET.Tests.csproj --filter "Category!=Smoke"
```

## PostgreSQL With Docker Desktop

Create `.env` from `.env.example`, replace both matching PostgreSQL password
placeholders, then start PostgreSQL:

```powershell
docker compose up -d
```

Run the app:

```powershell
dotnet run --project Okafor-.NET.csproj
```

## Fast In-Memory Run

Use this when PostgreSQL is not available and you only need to verify startup/routes:

```powershell
$env:ASPNETCORE_ENVIRONMENT="Testing"
$env:ASPNETCORE_URLS="http://localhost:5187"
dotnet run --project Okafor-.NET.csproj --no-launch-profile
```

## Smoke Verification

In one terminal:

```powershell
$env:ASPNETCORE_ENVIRONMENT="Testing"
$env:ASPNETCORE_URLS="http://localhost:5187"
dotnet run --project Okafor-.NET.csproj --no-launch-profile
```

In another terminal:

```powershell
$env:OKAFOR_BASE_URL="http://localhost:5187"
dotnet test .\tests\Okafor.NET.Tests\Okafor.NET.Tests.csproj --filter "Category=Smoke"
```

Or run the helper:

```powershell
.\scripts\verify-backend.ps1 -Smoke
```

## Git Hooks

Husky installs during `npm install`. It blocks direct local pushes to `main` and `master`.

Normal work should happen on feature branches:

```powershell
git switch -c feature/my-change
git push -u origin feature/my-change
```

GitHub branch protection should still be enabled because local hooks can be bypassed.
