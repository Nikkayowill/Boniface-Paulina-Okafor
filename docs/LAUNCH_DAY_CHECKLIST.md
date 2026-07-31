# Launch Day Checklist — 31 July 2026

Scope: the free custom-domain hosting path in `docs/FREE_HOSTING_READINESS.md`
(Azure Container Apps + Azure SQL free offer, deployed by the
**Hosted preview release** workflow).

Status key: `[x]` done in the repo, `[ ]` still needs you.

---

## 0. Code fixes already applied on this branch

- [x] Demo clinical/news/appointment seeds no longer run on the public host.
      The hosted preview runs as `ASPNETCORE_ENVIRONMENT=Staging`, and the old
      rule seeded demo data in Staging — it would have published 8 fabricated
      clinicians and 5 fake patient records with phone numbers on the real
      hospital site. Demo data is now Development-only unless `DemoData:Enabled`
      is explicitly set, and `Production`/`E2E`/`Testing` refuse the opt-in.
- [x] Seeded-admin placeholder detection fixed. The guard only matched
      `CHANGE_ME_USE_USER_SECRETS`, but `appsettings.Staging.json` ships
      `CHANGE_VIA_USER_SECRETS`. A missing `SeedAdmin__Password` secret would
      have created a live Admin account with a password published in this repo.
      The guard now matches the whole `CHANGE_`/`REPLACE_WITH` convention.
- [x] Patient registration survives a failing confirmation email. The deploy
      sets `RequireConfirmedAccount=false` specifically to launch without SMTP,
      but registration still deleted the new account when the email send threw —
      signup was broken end to end on the hosted preview.

Verification: `dotnet build` clean; 289 non-smoke tests pass; 4 E2E pass.

> Note: a bare `dotnet test` shows 20 failures in `SmokeTests`. Those are
> post-deploy tests that require a running server on `OKAFOR_BASE_URL`; CI and
> `scripts/verify-backend.sh` correctly filter them with
> `--filter "Category!=Smoke&Category!=DatabaseIntegration"`. Not a defect.

---

## 1. Hard blockers — launch fails without these

### Release branch
- [ ] Merge this branch into the release ref. The workflow's `release_ref`
      defaults to `launch/july31`; you are on
      `launch/15-free-custom-domain-hosting`. Either merge, or pass the exact
      branch/SHA when dispatching.

### Azure resources
- [ ] Azure subscription + resource group created.
- [ ] Container Apps environment (Consumption) with log destination `none`
      — the workflow aborts if logs go to a billed Log Analytics workspace.
- [ ] Azure SQL created with the **free offer** (`useFreeLimit=true`) and
      exhaustion behaviour **AutoPause** — the workflow asserts both.
- [ ] Container App permitted to reach Azure SQL (firewall / "Allow Azure
      services").
- [ ] **Azure budget alert configured.** The workflow guards cost but cannot
      guarantee zero spend.

### GitHub `hosted-preview` environment
- [ ] Protected environment named `hosted-preview` created.
- [ ] Secrets: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`,
      `AZURE_SQL_CONNECTION_STRING`, `SEED_ADMIN_EMAIL`, `SEED_ADMIN_PASSWORD`.
- [ ] Variables: `AZURE_RESOURCE_GROUP`, `AZURE_LOCATION`,
      `AZURE_CONTAINER_APP_ENVIRONMENT`, `AZURE_CONTAINER_APP`,
      `AZURE_SQL_SERVER`, `AZURE_SQL_DATABASE`, `CUSTOM_DOMAIN`.
- [ ] `SEED_ADMIN_PASSWORD` is a real strong password. It must **not** start
      with `CHANGE_` or `REPLACE_WITH` — the seed now skips those, which leaves
      you with no admin and a failing `/health/ready`, aborting the deploy.

### Real content (new consequence of the demo-data fix)
- [ ] The site now launches with **zero departments, clinicians, and news**.
      Sign in as admin and enter owner-approved departments and clinicians
      before announcing the site, or the "Our Doctors" page and the booking
      form will be empty. The booking view degrades gracefully
      (`ViewBag.HasDoctors`) but is not usable without real clinicians.

---

## 2. Decisions to make before you dispatch

- [ ] **Email.** `appsettings.Staging.json` still has `STAGING_SMTP_HOST` and
      placeholder credentials. Choose one:
      - configure real SMTP (`Email__SmtpHost`, `Port`, `EnableSsl`,
        `FromAddress`, `Username`, `Password`) — enables password reset; or
      - launch without SMTP, accepting that **password reset and email
        confirmation will not work**. Registration and login still work.
- [ ] Confirm final hospital contact details, emergency numbers, and the public
      WhatsApp number in `appsettings` (`Hospital`, `Notifications`).
- [ ] Confirm payment wording. Checkout is the labelled **mock** provider;
      bill payments and patient documents are disabled for the free tier.
- [ ] Confirm privacy / patient-data wording on the public pages.

---

## 3. Deploy sequence

- [ ] Dispatch **Hosted preview release** with custom-domain binding **off**:
      schema compatibility confirmed, confirmation `DEPLOY HOSTED PREVIEW`.
- [ ] Test on the generated `*.azurecontainerapps.io` hostname before touching DNS.
- [ ] Read the workflow summary for the exact CNAME and TXT records.
- [ ] Create at the registrar as **DNS-only** records (do **not** proxy the
      `www` CNAME through Cloudflare — managed-certificate validation needs a
      direct CNAME).
- [ ] After DNS propagates, rerun the workflow with binding **on**.
- [ ] Confirm HTTPS certificate issued and the domain serves the site.

---

## 4. Manual verification (owner, in a browser)

- [ ] Seeded admin can sign in.
- [ ] Appointment request → admin approval.
- [ ] Teleconsultation request → admin update.
- [ ] Patient registration → login → profile.
- [ ] Patient messages.
- [ ] Mock donation checkout is visibly labelled as a sandbox/mock.
- [ ] PWA install prompt appears.
- [ ] Offline page behaves correctly (devtools → offline).
- [ ] Push notification subscription and a test notification.
- [ ] Home, About, Services, Doctors, News, Contact all render on mobile and desktop.
- [ ] First request after idle is slow (scale-from-zero) — expected, not a bug.

---

## 5. Accepted risks for launch day

- Cold start after inactivity (scale-to-zero).
- Azure SQL may pause for the month if the free allowance is exhausted; no SLA.
- Appointment reminder background jobs do not run (disabled at zero replicas).
- Patient document upload and bill payments are disabled.
- Password reset unavailable if you launch without SMTP.

---

## 6. Not blocking today, do soon after

- [ ] Branch protection on `master`: require PRs and passing CI.
- [ ] Add teammate as collaborator.
- [ ] Backup/restore drill with measured RPO/RTO
      (`docs/BACKUP_RESTORE_RUNBOOK.md`).
- [ ] Production-like staging rehearsal recorded in `docs/loop-runs/`.
- [ ] Add `launch/15-free-custom-domain-hosting` (or the new release branch) to
      the `push` triggers in `.github/workflows/ci.yml` — currently only `main`,
      `master`, and `launch/july31` run CI on push.
- [ ] Rotate the seeded admin password after first sign-in.
