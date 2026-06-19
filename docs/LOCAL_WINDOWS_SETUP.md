# Local Windows Setup

Use this when a Windows teammate clones the repo.

## Required Tools

- .NET 10 SDK
- Git
- Node.js LTS and npm
- One SQL Server option:
  - SQL Server LocalDB from Visual Studio, or
  - Docker Desktop with SQL Server container, or
  - A local SQL Server Developer instance

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

## Database Option A: LocalDB

LocalDB works well on Windows if installed with Visual Studio.

Use a connection string like:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\mssqllocaldb;Database=OkaforHospitalDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

Run the app:

```powershell
dotnet run --project Okafor-.NET.csproj
```

## Database Option B: Docker Desktop

Create `.env` from `.env.example`, set `SA_PASSWORD`, then start SQL Server:

```powershell
docker compose up -d
```

Set the connection string:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=OkaforHospitalDb;User Id=sa;Password=<your-password>;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

Run the app:

```powershell
dotnet run --project Okafor-.NET.csproj
```

## Fast InMemory Run

Use this when SQL Server is not available and you only need to verify startup/routes:

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
