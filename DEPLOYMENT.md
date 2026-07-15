# Deployment Guide - Okafor Hospital Management System

## Overview

This guide covers deploying Okafor to staging and production environments.

**Deployment Flow**:
1. Code merge to main branch
2. GitHub Actions CI runs (build + tests pass)
3. Manual promotion to Staging
4. Smoke tests validate staging
5. Promotion to Production
6. Post-deployment verification

---

## Pre-Deployment Checklist

Before deploying to any environment, verify:

```bash
# 1. Build succeeds
dotnet build Okafor-.NET.sln -c Release

# 2. All tests pass
dotnet test Okafor-.NET.sln -c Release

# 3. No vulnerabilities
dotnet list package --vulnerable --include-transitive

# 4. Code changes committed
git status  # Should be clean

# 5. Migrations reviewed
dotnet ef migrations list
```

For the current free-hosting decision and verified 2026 limits, read `docs/FREE_HOSTING_READINESS.md`.

---

## Staging Deployment

### Container Build

The included multi-stage `Dockerfile` publishes the .NET 10 application and runs it as the image's non-root user on port 8080.

```bash
docker build -t okafor-hospital:staging .
docker run --rm \
  -e ASPNETCORE_ENVIRONMENT=Staging \
  -e ConnectionStrings__DefaultConnection="<azure-sql-connection-string>" \
  -v okafor-private-data:/data \
  -p 8080:8080 \
  okafor-hospital:staging
```

For production-like container revisions, persist `/data` and `/app/wwwroot/uploads`. The first contains patient documents and Data Protection keys; the second contains CMS thumbnails.

Student Study Guide: a multi-stage image uses the large SDK only to compile the application, then copies the published output into the smaller runtime image. Running as a non-root user limits damage if application code is compromised.

### Option 1: Local Testing with Staging Profile

Run locally with staging configuration:

```bash
# Build for staging environment
dotnet build Okafor-.NET.sln -c Release

# Run with Staging environment
set ASPNETCORE_ENVIRONMENT=Staging
dotnet run
# or
dotnet run --launch-profile https --environment Staging
```

### Option 2: Publish to Staging Server

**On build machine:**

```bash
# Create release package
dotnet publish -c Release -o ./dist/staging

# Create deployment archive
tar -czf Okafor-Staging-$(date +%Y%m%d-%H%M%S).tar.gz ./dist/staging
```

**On staging server:**

```bash
# Prerequisites
# - .NET 10 runtime installed
# - SQL Server 2019+ (or Azure SQL Database)
# - HTTPS certificate
# - Database created: OkaforHospital_Staging

# 1. Stop current application
systemctl stop okafor  # Linux
# or
net stop Okafor  # Windows

# 2. Extract deployment package
mkdir -p /opt/okafor/staging
tar -xzf Okafor-Staging-*.tar.gz -C /opt/okafor/staging

# 3. Update configuration
# Edit appsettings.Staging.json with actual values:
# - Server name: replace STAGING_SERVER
# - Database password: use secrets manager
# - SMTP credentials: use secrets manager
# - Africa's Talking credentials: use secrets manager

# 4. Run database migrations
cd /opt/okafor/staging
export ASPNETCORE_ENVIRONMENT=Staging
dotnet Okafor-.NET.dll --migrate-db

# 5. Start application
systemctl start okafor  # Linux
# or
net start Okafor  # Windows

# 6. Verify health
curl https://staging.okaformemorial.org/health
```

---

## Running Smoke Tests Against Staging

**From local machine or CI:**

```bash
# Run only smoke tests
export OKAFOR_BASE_URL=https://staging.okaformemorial.org
dotnet test Okafor-.NET.sln --filter "Category=Smoke" -c Release

# Or with specific test runner
dotnet test Okafor-.NET.sln -c Release -l "console;verbosity=detailed" --filter "Category=Smoke"
```

**Expected output:**
```
✓ HealthCheck_Endpoint_Returns200
✓ HomePage_Loads_Successfully
✓ Doctors_Page_Loads_Successfully
✓ AppointmentRequests_Page_Accessible
✓ Css_Files_Load_Successfully
✓ ResponseHeaders_Include_Security_Basics
✓ No_500_Errors_On_Home
✓ Timeout_Handling_Reasonable
✓ Static_Content_Returns_Correct_Content_Types
```

If all pass ✓, staging is healthy.

---

## Production Deployment

### Pre-Production Requirements

- [ ] Staging validation passed (all smoke tests ✓)
- [ ] Database backups taken
- [ ] Rollback plan documented
- [ ] SSL/TLS certificates valid
- [ ] Production secrets configured (Africa's Talking, payment gateway, SMTP)
- [ ] Production database created
- [ ] Monitoring configured (Application Insights, or similar)

### Production Release Steps

**1. Create Release Build**

```bash
git checkout main
git pull origin main

dotnet build Okafor-.NET.sln -c Release
dotnet test Okafor-.NET.sln -c Release
dotnet publish -c Release -o ./dist/production
```

**2. Backup Production Database**

```bash
# SQL Server backup (via T-SQL or SSMS)
BACKUP DATABASE [OkaforHospital] 
TO DISK = 'S:\Backups\OkaforHospital_$(date +%Y%m%d_%H%M%S).bak'
```

**3. Deploy to Production**

```bash
# On production server:
systemctl stop okafor

# Backup current deployment
cp -r /opt/okafor/production /opt/okafor/production.backup.$(date +%Y%m%d-%H%M%S)

# Extract new release
tar -xzf Okafor-Production-*.tar.gz -C /opt/okafor/production

# Update configuration (use production secrets)
export ASPNETCORE_ENVIRONMENT=Production
dotnet ef database update

# Start application
systemctl start okafor
```

**4. Post-Deployment Validation**

```bash
# Health check
curl https://okaformemorial.org/health

# Run smoke tests
export OKAFOR_BASE_URL=https://okaformemorial.org
dotnet test Okafor-.NET.sln --filter "Category=Smoke"

# Check application logs
journalctl -u okafor -f  # Linux
Get-EventLog -LogName Application -Source Okafor | Select -First 50  # Windows
```

---

## Environment Variables & Secrets

**Never commit secrets to version control.** Use one of:

### Option 1: User Secrets (Development)
```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Password=..."
dotnet user-secrets set "Notifications:AfricasTalking:ApiKey" "..."
```

### Option 2: Environment Variables (Docker/VM)
```bash
export ConnectionStrings__DefaultConnection="..."
export Notifications__AfricasTalking__ApiKey="..."
dotnet Okafor-.NET.dll
```

### Option 3: appsettings.Staging/Production (with external secrets injection)
Use Azure Key Vault, AWS Secrets Manager, or similar to inject secrets at deployment time.

---

## Rollback Procedure

If production deployment fails:

**Option 1: Revert to previous application version**

```bash
systemctl stop okafor
rm -rf /opt/okafor/production
mv /opt/okafor/production.backup.TIMESTAMP /opt/okafor/production
systemctl start okafor
```

**Option 2: Database rollback**

```sql
-- If migrations caused issues
RESTORE DATABASE [OkaforHospital] 
FROM DISK = 'S:\Backups\OkaforHospital_TIMESTAMP.bak'
WITH REPLACE
```

**Option 3: Git rollback (if needed)**
```bash
git revert <commit-hash>  # Creates new commit that undoes changes
git push origin main
# Re-deploy with previous version
```

---

## Monitoring & Alerting

### Health Endpoint
- **Combined endpoint**: `GET /health`
- **Process liveness**: `GET /health/live`
- **SQL readiness**: `GET /health/ready`
- **Expected Response**: `200 OK`
- **Frequency**: Monitor every 30 seconds

### Application Logs
Monitor for errors:
```bash
# Linux
tail -f /var/log/okafor/app.log

# Windows
Get-Content C:\Logs\Okafor\app.log -Tail 100 -Wait
```

### Key Metrics to Watch
1. **Request latency**: Appointment/payment APIs should respond <500ms
2. **Error rate**: Should stay <0.1%
3. **Database connections**: Monitor active connections
4. **Disk space**: Monitor `wwwroot/uploads/posts/` and the configured private `PatientDocuments:StorageRoot` volume

---

## Troubleshooting

### Application won't start

```bash
# Check logs
journalctl -u okafor -n 50

# Verify .NET runtime
dotnet --version

# Check database connectivity
# Edit appsettings to test connection string directly
```

### Database migration failed

```bash
# Check pending migrations
dotnet ef migrations list

# View migration history in database
SELECT * FROM __EFMigrationsHistory ORDER BY MigrationId DESC

# Rollback specific migration if needed
dotnet ef migrations remove
```

### Health check failing

```bash
# Test endpoint directly
curl -v https://staging.okaformemorial.org/health

# Check logs for errors
grep -i "health" /var/log/okafor/app.log
```

---

## CI/CD Integration

The GitHub Actions workflow (`.github/workflows/ci.yml`) automatically:

1. Builds on every push to main
2. Runs full test suite
3. Scans for vulnerabilities
4. Uploads test results

**To enable auto-deployment to staging:**

Add step to `.github/workflows/ci.yml`:
```yaml
- name: Deploy to Staging
  if: github.ref == 'refs/heads/main' && success()
  run: |
    # Your deployment script here
    ./scripts/deploy-staging.sh
```

---

## Support & Questions

For deployment issues:
1. Check application logs
2. Run smoke tests to isolate problems
3. Review pre-deployment checklist
4. Consult rollback procedures

---

**Last Updated**: May 1, 2026  
**Version**: 1.0
