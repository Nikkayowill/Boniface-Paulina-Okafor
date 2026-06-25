# Functionality Loop Board

This board separates work Codex can keep improving from work the owner must personally complete or confirm. Owner tasks stay unchecked until the owner says they are done.

## Codex-Lane Backlog

### Five-Week Backend And DevOps Launch Sprint

Source plan: `docs/LAUNCH_SPRINT_PLAN.md`.

- [ ] Week 1: Establish launch board, baseline verification, SQL Server Development verification, CI confidence, and privacy/security checklist.
- [ ] Week 2: Prove appointment and teleconsultation workflows end to end with SQL-backed persistence and admin review.
- [ ] Week 3: Harden patient portal, admin operations, authorization, document uploads, and sensitive-data handling.
- [ ] Week 4: Verify payments, notifications, PWA/offline behavior, staging deployment, secrets, TLS, and monitoring.
- [ ] Week 5: Freeze scope, run staging launch rehearsal, validate rollback/backup plans, and prepare production release.

### Landing Page Redesign

Goal: frontend-team lane. Redesign only the public landing page. Backend/DevOps should support route contracts and verification, but should not own the visual redesign unless a backend route or data contract blocks the frontend team.

- [ ] Redesign the landing-page hero so the first viewport feels polished, trustworthy, and clearly hospital-specific.
- [ ] Rework the care shortcuts and primary calls to action so appointment, teleconsultation, emergency/contact, services, and patient info paths are easy to scan.
- [ ] Replace the current long linear homepage flow with a more professional section rhythm using real hospital imagery, concise copy, and clearer visual hierarchy.
- [ ] Add stronger trust and service proof using existing seeded departments, doctors, posts, hospital contact config, and available hospital images.
- [ ] Improve mobile landing-page spacing, typography, button wrapping, and care shortcut behavior.
- [ ] Normalize the landing-page visual system in `wwwroot/css/public-site.css` without spilling styles into admin, patient portal, identity, payment, or form pages.
- [ ] Verify landing-page accessibility basics: semantic headings, useful alt text, visible focus states, contrast, reduced-motion behavior, and no text overlap.
- [ ] Run focused route/style verification after redesign and record manual checks for desktop and mobile landing-page review.

### Verification Automation

- [x] Add backend verification script for build, non-smoke tests, and smoke tests.
- [x] Add Linux `dotnet watch` helper for inotify/polling issues.
- [ ] Add Development SQL smoke script that starts/checks SQL Server and verifies `/health`.
- [ ] Add a script or test helper for seeded admin existence once local secrets are set.
- [ ] Add CI artifact/log guidance for failed smoke tests.

### Public Workflows

- [ ] Add integration test for appointment request form submission in `Testing` mode.
- [ ] Add integration test for contact form submission in `Testing` mode.
- [x] Add route/content smoke coverage for About, Services, News, and Contact pages.
- [x] Add regression test for WhatsApp floating widget link generation.

### Appointment And Scheduling

- [ ] Add integration coverage for appointment request creation and persistence.
- [ ] Add service/controller coverage for doctor-by-department lookup.
- [ ] Add tests for slot booking collision prevention.
- [ ] Add admin approval/rejection workflow tests where practical.

### Teleconsultations

- [x] Add smoke/integration coverage for teleconsultation create page.
- [ ] Add integration coverage for teleconsultation submission.
- [ ] Add admin teleconsultation status update coverage.
- [ ] Add patient teleconsultation history coverage.

### Patient Portal

- [ ] Add patient authorization tests for portal routes.
- [ ] Add profile create/edit tests.
- [ ] Add document upload validation tests.
- [ ] Add message send/list tests.
- [ ] Add appointment cancellation tests.

### Payments

- [ ] Add mock donation flow tests.
- [ ] Add mock bill payment flow tests.
- [ ] Add Paystack webhook signature negative test.
- [ ] Add receipt page route tests.

### Notifications

- [ ] Add notification provider selection tests.
- [ ] Add WhatsApp webhook invalid signature/invalid token tests.
- [ ] Add push subscription controller tests for invalid payloads.
- [ ] Add email fallback/logging tests for receipt senders.

### PWA And Offline

- [x] Keep service worker and PWA registration tests passing.
- [x] Add smoke coverage for `offline.html`, `offline-appointments.html`, and `site.webmanifest`.
- [ ] Add test coverage for private/admin route cache exclusions.
- [ ] Add browser manual checklist for install prompt and offline appointment sync.

### Documentation And Collaboration

- [x] Add collaboration workflow documentation.
- [x] Add feature inventory.
- [x] Add recovery plan and verification checklist.
- [x] Add Linux and Windows local setup docs.
- [ ] Add teammate read-through checklist for first PR.
- [ ] Add API/route contract updates if React frontend work begins.

## Owner-Lane Checklist

These stay unchecked until the owner confirms them.

### Local Credentials And Access

- [ ] Set local `SeedAdmin:Email` with user secrets.
- [ ] Set local `SeedAdmin:Password` with user secrets.
- [ ] Confirm seeded admin can sign in.
- [ ] Confirm Staff and Patient test accounts needed for manual workflow checks.

### GitHub And Collaboration Settings

- [ ] Create remote repository or confirm existing remote.
- [ ] Push current branch to GitHub.
- [ ] Enable branch protection for `main` or `master`.
- [ ] Require pull requests before merge.
- [ ] Require CI checks before merge.
- [ ] Add teammate as collaborator with the right access level.

### Provider Accounts

- [ ] Confirm Paystack sandbox account access.
- [ ] Confirm WhatsApp Business/Meta app access.
- [ ] Confirm Africa's Talking account access.
- [ ] Confirm SMTP provider/account.
- [ ] Generate VAPID keys for browser push.
- [ ] Confirm production phone numbers and public WhatsApp number.

### Manual Browser Verification

- [ ] Verify appointment request to admin approval in browser.
- [ ] Verify teleconsultation request to admin update in browser.
- [ ] Verify patient registration/login/profile in browser.
- [ ] Verify patient document upload/delete in browser.
- [ ] Verify patient messages in browser.
- [ ] Verify mock donation and bill payment in browser.
- [ ] Verify PWA install prompt in browser.
- [ ] Verify offline page behavior in browser devtools.
- [ ] Verify push notification subscription and test notification in browser.

### Business And Launch Decisions

- [ ] Confirm final hospital contact info.
- [ ] Confirm appointment scheduling rules.
- [ ] Confirm teleconsultation intake wording.
- [ ] Confirm payment/donation wording.
- [ ] Confirm privacy and patient data handling wording.
- [ ] Confirm launch/staging domain plan.
- [ ] Approve production deployment.
