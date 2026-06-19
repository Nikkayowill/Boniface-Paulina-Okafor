param(
    [switch]$Smoke,
    [string]$DotNet = "dotnet",
    [int]$Port = 5187,
    [string]$BaseUrl = ""
)

$ErrorActionPreference = "Stop"

$Project = "Okafor-.NET.csproj"
$TestProject = "tests/Okafor.NET.Tests/Okafor.NET.Tests.csproj"

if ([string]::IsNullOrWhiteSpace($BaseUrl)) {
    if ([string]::IsNullOrWhiteSpace($env:OKAFOR_BASE_URL)) {
        $BaseUrl = "http://localhost:$Port"
    } else {
        $BaseUrl = $env:OKAFOR_BASE_URL
    }
}

Write-Host "Restoring packages..."
& $DotNet restore $Project

Write-Host "Building test project..."
& $DotNet build $TestProject --no-restore --verbosity minimal

Write-Host "Running non-smoke tests..."
& $DotNet test $TestProject --no-build --filter "Category!=Smoke" --verbosity minimal

if (-not $Smoke) {
    Write-Host "Smoke tests skipped. Pass -Smoke to start the app and run smoke tests."
    exit 0
}

if ([string]::IsNullOrWhiteSpace($env:ASPNETCORE_ENVIRONMENT)) {
    $env:ASPNETCORE_ENVIRONMENT = "Testing"
}

if ([string]::IsNullOrWhiteSpace($env:ASPNETCORE_URLS)) {
    $env:ASPNETCORE_URLS = $BaseUrl
}

$env:OKAFOR_BASE_URL = $BaseUrl
$stdout = Join-Path $env:TEMP "okafor-verify-backend.out.log"
$stderr = Join-Path $env:TEMP "okafor-verify-backend.err.log"

Write-Host "Starting app for smoke tests at $env:ASPNETCORE_URLS..."
$process = Start-Process -FilePath $DotNet `
    -ArgumentList @("run", "--project", $Project, "--no-build", "--no-launch-profile") `
    -RedirectStandardOutput $stdout `
    -RedirectStandardError $stderr `
    -PassThru

try {
    $healthy = $false
    for ($i = 0; $i -lt 60; $i++) {
        try {
            $response = Invoke-WebRequest -Uri "$BaseUrl/health" -UseBasicParsing -TimeoutSec 2
            if ($response.StatusCode -eq 200) {
                $healthy = $true
                break
            }
        } catch {
            Start-Sleep -Seconds 1
        }
    }

    if (-not $healthy) {
        Write-Host "App did not become healthy. Recent stdout:"
        if (Test-Path $stdout) {
            Get-Content $stdout -Tail 80
        }
        Write-Host "Recent stderr:"
        if (Test-Path $stderr) {
            Get-Content $stderr -Tail 80
        }
        exit 1
    }

    Write-Host "Running smoke tests..."
    & $DotNet test $TestProject --no-build --filter "Category=Smoke" --verbosity minimal
    Write-Host "Backend verification complete."
}
finally {
    if ($process -and -not $process.HasExited) {
        Stop-Process -Id $process.Id -Force
    }
}
