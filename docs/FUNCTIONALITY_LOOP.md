# Continuous Functionality Loop

This is the operating loop for Codex-assisted improvement. Use it when you want the app to keep getting more reliable without losing track of what still requires your manual action.

## Trigger Phrase

When the user says:

```text
Run the functionality loop
```

Codex should follow this document before making changes.

## Loop Goals

- Keep the backend functional while frontend/design work continues.
- Convert assumptions into evidence through builds, tests, smoke checks, and manual verification notes.
- Improve one small, valuable area per pass.
- Leave owner-only tasks unchecked until the owner confirms them.
- Keep docs, tests, and implementation aligned after every pass.

## Source Order

Read these sources in this order:

1. `docs/RECOVERY_STATUS.md`
2. `docs/FUNCTIONALITY_LOOP_BOARD.md`
3. `docs/FEATURE_INVENTORY.md`
4. `docs/VERIFICATION_CHECKLIST.md`
5. `docs/FUNCTIONALITY_RECOVERY_PLAN.md`
6. Recent test output from `./scripts/functionality-loop.sh`
7. Relevant controllers, services, models, views, and tests for the selected task

## Source Map

| Source | Use |
|---|---|
| Repo code | Source of truth for implemented behavior |
| Automated tests | Source of truth for repeatable proof |
| SQL Server Development run | Source of truth for persistence and EF migrations |
| Browser manual checks | Source of truth for PWA, uploads, push, and UI workflows |
| Provider dashboards | Source of truth for Paystack, WhatsApp, SMS, email, and push delivery |
| User confirmation | Source of truth for credentials, production accounts, business wording, and launch decisions |

## Codex Lane

Codex may do these without owner confirmation:

- Run build/test/smoke verification.
- Start local Docker Compose services when required for local verification.
- Add or improve automated tests.
- Fix code bugs found by tests, logs, or source review.
- Improve docs, setup scripts, CI, and local developer workflow.
- Add manual checklist items when a workflow cannot be safely automated.
- Update feature status only when supported by evidence.

## Owner Lane

Codex must leave these unchecked until the owner confirms completion:

- Create or share real credentials.
- Confirm admin password and admin login.
- Confirm patient-facing wording, hospital policy, and business rules.
- Verify live Paystack, WhatsApp, SMS, SMTP, VAPID, and production provider accounts.
- Confirm browser/device behavior that requires local interaction.
- Enable GitHub branch protection and repo settings.
- Approve production deployment and launch readiness.

## Pass Structure

Each loop pass should follow this sequence:

1. **Collect signal**: run `./scripts/functionality-loop.sh`.
2. **Choose one target**: select the highest-value unchecked Codex-lane task from `docs/FUNCTIONALITY_LOOP_BOARD.md`.
3. **Inspect evidence**: read the relevant implementation and tests.
4. **Improve**: make a small scoped code/test/docs change.
5. **Verify**: run the narrowest useful test, then the backend verifier if needed.
6. **Record**: update `docs/RECOVERY_STATUS.md`, `docs/FEATURE_INVENTORY.md`, or checklist status only when evidence supports it.
7. **Stop cleanly**: summarize what changed, what passed, and what remains owner-only.

## Status Rules

Use these status labels:

| Status | Meaning |
|---|---|
| `Pending` | Not done yet |
| `In Progress` | Currently being worked |
| `Blocked: Owner` | Needs owner action or credentials |
| `Blocked: External` | Needs external provider or infrastructure |
| `Verified` | Evidence exists in tests, logs, or manual owner confirmation |

## Evidence Rules

- Automated evidence must include the command and result.
- Manual evidence must name who verified it and what environment was used.
- Provider evidence must name the provider environment, such as sandbox or live.
- Do not mark owner-lane work as `Verified` based only on code existing.

## Recommended First Loop Targets

1. Expand integration tests for public appointment submission using Testing mode.
2. Expand integration tests for teleconsultation submission using Testing mode.
3. Add tests for mock donation and mock bill payment behavior.
4. Add a development startup smoke script that checks SQL Server plus `/health`.
5. Add controller tests around patient document upload validation.

## Stop Conditions

Stop and report clearly when:

- Build or tests fail and the failure is not fixed in the current pass.
- A task requires credentials or browser action.
- Docker, SQL Server, or provider infrastructure is unavailable.
- The next change would be broad enough to deserve a separate branch or owner decision.
