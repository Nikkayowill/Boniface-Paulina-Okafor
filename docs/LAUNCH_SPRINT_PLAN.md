# Five-Week Backend And DevOps Launch Sprint

Date: 2026-06-25

## Launch Standard

Treat this like a professional hospital launch, not a hobby website. The public landing page can be handled by the frontend team, but backend, DevOps, security, privacy, provider integrations, data integrity, and operational readiness must be handled with the seriousness expected from a major healthcare institution.

This is a rural hospital in Nigeria, so the launch standard must account for:

- Low-bandwidth and intermittent connectivity.
- Mobile-first public and patient workflows.
- Real patient data, medical context, payments, notifications, and uploaded documents.
- A small operational team that needs simple, recoverable workflows.
- Privacy and data protection obligations under Nigeria's data protection regime.

References:

- Nigeria Data Protection Commission: https://ndpc.gov.ng/
- NDPC services include privacy breach reporting, data controller/processor registration, audit filing, and related compliance services: https://ndpc.gov.ng/
- OWASP ASVS provides a basis for testing web application security controls and secure development requirements: https://owasp.org/www-project-application-security-verification-standard/

This is an engineering launch plan, not legal advice. The owner should confirm final privacy/data protection obligations with counsel or a licensed data protection compliance adviser.

## Team Scope

Our lane:

- Backend workflows.
- Admin and patient portal reliability.
- Database, migrations, seed data, and production configuration.
- Authentication, authorization, data access, file uploads, PWA/offline safety, notifications, payments, webhooks, monitoring, CI/CD, backups, and launch runbooks.

Frontend-team lane:

- Public landing-page redesign.
- Public marketing visuals and content polish.
- Frontend visual QA for the redesigned homepage.

Shared handoff:

- Navigation links and route contracts.
- Public CTA destinations.
- Analytics/SEO metadata if needed.
- Visual regression checks for routes touched by both teams.

## Operating Rules

- `master` is protected. Work lands through PRs.
- Every issue must have an owner, priority, test plan, and launch risk.
- Every backend PR must include at least one of: automated test, smoke/manual verification note, or explicit reason why neither applies.
- No feature is launch-ready until it is verified in a production-like staging environment with SQL Server.
- Do not mark provider integrations launch-ready until credentials, webhook URLs, callback URLs, and failure paths are tested.
- Never commit secrets. Use environment variables, user secrets, GitHub environment secrets, or host-level secret management.
- Patient data and uploaded documents are treated as sensitive. Logs must not contain raw medical details, uploaded document contents, passwords, or full payment secrets.

## Issue Labels

Recommended labels:

- `launch-blocker`
- `backend`
- `devops`
- `security`
- `privacy`
- `payments`
- `notifications`
- `appointments`
- `teleconsultation`
- `patient-portal`
- `admin`
- `pwa-offline`
- `observability`
- `staging`
- `manual-qa`
- `frontend-dependency`

## Definition Of Done

For code issues:

- Build passes.
- Non-smoke test suite passes.
- Relevant route or workflow smoke/manual check is documented.
- No secrets or environment-specific values committed.
- Error handling and logging are deliberate.
- Role/authorization behavior is checked if the change touches private data.
- Data migration impact is documented if schema changes.

For launch issues:

- Staging verification complete.
- Rollback path documented.
- Owner has confirmed required external account/config.
- Any remaining risk is accepted explicitly.

## Week 1: Baseline, Board, And Production Boundaries

Goal: make the repo and issue tracker trustworthy. No guessing.

Backend/dev tasks:

- Confirm `master` branch protection, CODEOWNERS, and required PR reviews.
- Fix or quarantine incomplete package changes such as missing screenshot scripts.
- Create GitHub milestones for Week 1 through Week 5.
- Convert highest-risk items from this plan into tracker issues.
- Run baseline verification: restore, build, non-smoke tests, smoke tests.
- Verify Development SQL Server startup and migrations.
- Confirm seed data: roles, admin seed, departments, doctors, posts, sample appointment data.
- Update `docs/FEATURE_INVENTORY.md` statuses after verification.

DevOps tasks:

- Confirm CI works on PRs and `master`.
- Add CI artifact/log guidance for failed smoke tests.
- Decide staging host, production host, domain, TLS, and database hosting path.
- Draft backup and restore runbook.

Security/privacy tasks:

- Create privacy/security launch checklist.
- Review patient-upload paths and authorization.
- Review service worker private-route cache exclusions.
- Review logs for sensitive data exposure.
- Identify NDPC/data-protection owner tasks: privacy notice, data controller/processor registration decision, breach reporting process, data retention policy.

Exit criteria:

- Issue tracker has the launch board populated.
- Baseline tests are green or known failures are tracked.
- Local SQL Server path is documented and verified.
- No unknown dirty changes remain in the launch branch.

## Week 2: Core Care Workflows

Goal: prove the hospital can receive, review, and act on care requests.

Backend/dev tasks:

- Add integration coverage for public appointment request submission.
- Add admin appointment approval/rejection coverage where practical.
- Centralize or harden appointment slot collision behavior.
- Add integration coverage for teleconsultation submission.
- Add admin teleconsultation status update tests.
- Verify SignalR booking notifications manually.
- Confirm appointment scheduling rules with owner.

Manual QA:

- Public appointment request to admin queue.
- Admin approval and rejection.
- Teleconsultation request to admin update.
- Patient-facing submitted pages.
- Staff/admin role access.

Exit criteria:

- Appointment and teleconsultation flows are proven in SQL-backed Development.
- Any remaining manual-only checks are tracked with owners.

## Week 3: Patient Portal, Admin Operations, And Data Safety

Goal: private workflows are secure, usable, and recoverable.

Backend/dev tasks:

- Add patient authorization tests for portal routes.
- Add profile create/edit tests.
- Add document upload validation tests.
- Add message send/list tests.
- Add appointment cancellation tests.
- Add admin contact submission workflow coverage.
- Add admin patient profile/document workflow verification.
- Review document upload limits, allowed file types, storage path, and delete behavior.

DevOps/security tasks:

- Ensure uploaded files are excluded from public/static caching where needed.
- Confirm database backup strategy includes patient portal data and upload metadata.
- Confirm real uploaded files are backed up or intentionally stored outside repo/static deployment.

Manual QA:

- Patient registration/login/profile.
- Patient documents upload/delete.
- Patient messages.
- Patient appointment history/cancel.
- Admin patient profile and document access.
- Unauthorized access checks.

Exit criteria:

- Patient portal private-data workflows are verified.
- Upload/document risks have explicit mitigation or owner acceptance.

## Week 4: Payments, Notifications, PWA, And Staging

Goal: external integrations work in staging and failure paths are safe.

Payments:

- Add mock donation flow tests.
- Add mock bill payment flow tests.
- Add Paystack webhook signature negative test.
- Test Paystack sandbox initialization, callback, webhook, and receipts.
- Confirm receipt pages do not expose unrelated records.

Notifications:

- Verify SMTP credentials and failure logging.
- Verify WhatsApp Cloud API webhook challenge and inbound message handling.
- Verify WhatsApp outbound template behavior for opted-in patients.
- Verify Africa's Talking only if SMS is still in launch scope.
- Generate and configure VAPID keys for push notifications.

PWA/offline:

- Verify manifest and service worker in staging.
- Verify offline fallback behavior.
- Verify private/admin/payment/upload/hub routes are not cached for offline replay.
- Verify install prompt on mobile widths.

DevOps:

- Deploy staging.
- Configure staging secrets.
- Configure health check and smoke test target.
- Add monitoring/error reporting with Sentry DSN or equivalent.
- Confirm TLS and domain settings.

Exit criteria:

- Staging exists and smoke tests pass against it.
- Payments and notifications are either launch-ready or explicitly out of launch scope.

## Week 5: Hardening, Freeze, And Launch Readiness

Goal: stop adding scope, prove launch, and prepare rollback.

Backend/dev tasks:

- Fix launch blockers only.
- Run full verification checklist in staging.
- Run smoke tests against staging.
- Validate migrations against staging database.
- Freeze schema unless launch blocker.
- Confirm production seed/admin strategy.
- Confirm final environment variables.

DevOps tasks:

- Production deployment runbook.
- Rollback runbook.
- Backup restore drill or documented dry run.
- Incident contact list.
- Post-launch monitoring checklist.
- GitHub release/tag plan.

Security/privacy tasks:

- Privacy notice final owner approval.
- Data retention policy owner approval.
- Breach reporting process owner approval.
- Admin/staff access review.
- Confirm no sample credentials or test accounts in production.

Manual launch rehearsal:

- Appointment request to admin approval.
- Teleconsultation request to admin status update.
- Patient registration/login/profile/documents/messages.
- Mock or sandbox payment receipt flow.
- WhatsApp/contact route.
- PWA install/offline check.
- Admin dashboard and CMS post workflow.

Exit criteria:

- Launch checklist signed off.
- Rollback plan exists.
- Known risks are documented and accepted.
- Production deployment is approved by owner.

## Launch Blockers

These block launch until resolved or formally removed from launch scope:

- App cannot start in production-like staging.
- Database migrations cannot be applied repeatably.
- Admin cannot sign in securely.
- Public appointment request cannot be submitted and reviewed.
- Teleconsultation request cannot be submitted and reviewed if teleconsultation is marketed at launch.
- Patient portal exposes another patient's data.
- Uploads allow unsafe file types or unauthorized access.
- Payments are marketed but Paystack/mock payment flow is not verified.
- Notifications are marketed but provider credentials/webhooks are not verified.
- Service worker caches private/admin/payment/upload routes.
- No backup and rollback plan.
- Privacy notice/data handling wording has an implementation draft; final owner/privacy adviser approval is still required.

## Immediate Issue Tracker Seeds

Create these first:

1. Verify SQL Server Development startup, migrations, and seed data.
2. Clean or complete package changes for Playwright screenshot script.
3. Add integration test for public appointment request submission.
4. Add integration test for public teleconsultation submission.
5. Add patient portal authorization tests.
6. Add document upload validation tests.
7. Add mock donation and bill payment tests.
8. Add Paystack webhook invalid signature test.
9. Verify service worker private route exclusions in browser.
10. Create staging deployment runbook.
11. Create backup and restore runbook.
12. Create privacy/security launch checklist.
13. Verify SMTP, WhatsApp, Paystack, and VAPID configuration matrix.
14. Run full manual browser verification on staging.
