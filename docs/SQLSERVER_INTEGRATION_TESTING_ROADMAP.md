# SQL Server Integration Testing Architecture and Three-Week Roadmap

Date: 2026-07-12
Target completion: 2026-07-31

## Provider Decision

This repository targets Azure SQL Server and SQL Server through `Microsoft.EntityFrameworkCore.SqlServer`. It does not use MySQL or SQLite. High-fidelity tests therefore run against SQL Server 2022 in Testcontainers. Adding MySQL would test a different provider and could hide the SQL Server-specific behavior this architecture is meant to prove.

The integration-test stack is:

- xUnit 2.9.2 for test discovery, fixtures, collections, and lifecycle hooks.
- Testcontainers.MsSql 4.13.0 for an isolated SQL Server 2022 container.
- Respawn 7.0.0 for dependency-aware data cleanup while preserving migrations.
- FluentAssertions 7.2.2 for readable assertions. Version 7 is intentionally used because it remains fully open source; FluentAssertions 8 requires a paid license for commercial use.
- EF Core SQL Server 10.0.7 and the application's real migrations.

## Generated Code Map and Student Study Guides

### `SqlServerIntegrationCollection.cs`

The collection definition tells xUnit to create one shared database fixture for every test class in this collection. `DisableParallelization` prevents two database tests from resetting or changing the same container simultaneously.

Student Study Guide: A collection is a classroom and the fixture is the shared laboratory. Every student uses the same laboratory, but only one experiment runs at a time so experiments cannot contaminate each other.

### `SqlServerIntegrationFixture.cs`

The fixture starts one pinned SQL Server 2022 image, applies the application's real EF Core migrations, opens one reset connection, and initializes Respawn from the live schema. It ignores `dbo.__EFMigrationsHistory`, so resets remove test data without deleting migration history or rebuilding the schema.

Student Study Guide — Container Lifecycle: The fixture rents a temporary SQL Server when the test collection starts, keeps it alive while related tests run, and disposes it at the end. Starting once is faster than starting a new server for every test.

Student Study Guide — Database State Reset: Respawn reads the real foreign-key graph and deletes rows in a safe dependency order. This is like clearing every completed form from an office while keeping the filing cabinets and office layout intact.

### `SqlServerIntegrationTestBase.cs`

The base class asks Respawn to reset the database before every test. Test classes inherit this behavior instead of copying reset code.

Student Study Guide: The container is shared for speed, but the data is not shared. Each test begins with the same empty business tables and the same migrated schema.

### `AppointmentTransactionWorkflowTests.cs`

The workflow seeds a doctor and availability, begins a real SQL Server transaction, creates an approved appointment and reserved slot, modifies existing availability, then throws a deliberate exception. The catch block explicitly rolls back. A new DbContext proves that no appointment or slot survived and that the earlier availability value was restored.

Student Study Guide — ACID Transaction Rollback: A transaction is an all-or-nothing envelope. If step four fails after steps one through three wrote data, rollback makes the database look as though none of those steps happened. The fresh verification context is important because it reads the database rather than trusting EF Core's in-memory change tracker.

### `verify-database-integration.sh`

The script checks that Docker and its daemon are available, restores and builds the test project, then runs only tests tagged `DatabaseIntegration`.

Student Study Guide: The script gives developers and CI the same front door. If the command succeeds locally and in CI, both environments followed the same sequence.

### `.github/workflows/ci.yml`

Fast unit and smoke jobs exclude container tests. A dedicated Linux job runs SQL Server integration tests because GitHub's Ubuntu runners provide Docker. This keeps ordinary feedback fast while making provider-fidelity tests required and visible.

Student Study Guide: Separating test lanes is like airport security lanes. Quick checks should not wait behind heavy database setup, but the flight cannot launch until every required lane passes.

### `ApplicationDbContext.cs` Identity key configuration

The first real-container run revealed that direct DbContext construction inferred four Identity login/token key columns at `nvarchar(450)`, while the committed application schema uses `nvarchar(128)`. The context now states the existing 128-character contract explicitly, making application DI, EF design-time tooling, and integration fixtures produce the same model.

Student Study Guide: An implicit default can change depending on how a framework object is created. Writing the existing schema rule explicitly is like putting the measurement on the blueprint instead of relying on each builder's memory.

## Sequential Execution Story

1. The test runner discovers the `DatabaseIntegration` category.
2. xUnit creates one `SqlServerIntegrationFixture` for the collection.
3. Testcontainers downloads the pinned image when necessary and starts an isolated SQL Server.
4. EF Core applies the application's production migrations to that SQL Server.
5. Respawn reads the migrated `dbo` schema and builds a safe deletion plan.
6. Before each test, Respawn clears business data and preserves `__EFMigrationsHistory`.
7. The test creates a new `ApplicationDbContext` using the container connection string.
8. The workflow executes real SQL Server inserts, constraints, and a transaction.
9. A deliberate mid-workflow exception triggers `RollbackAsync`.
10. A fresh context verifies that inserted rows disappeared and existing data returned to its original state.
11. At collection shutdown, xUnit disposes the fixture and Testcontainers removes the database container.

Non-technical explanation: we create a temporary copy of the real database engine, install the real schema, run a realistic hospital workflow, deliberately break it, prove the database recovered correctly, clean the data, and reuse the temporary server for the next scenario.

## Aggressive Three-Week Roadmap

### Week 1 — Foundation and transaction fidelity, July 13–19

- Merge the fixture, reset strategy, rollback example, script, and dedicated CI job.
- Confirm the pinned SQL Server image runs on every developer platform in use.
- Add database-integration coverage for appointment slot uniqueness, patient ownership queries, payment reference uniqueness, and delete behaviors.
- Record container startup time, reset time, and test duration as the initial performance baseline.
- Exit gate: repeatable green runs locally and in GitHub Actions with no shared external database.

### Week 2 — Critical workflow coverage, July 20–26

- Cover public appointment submission through admin approval and patient cancellation.
- Cover teleconsultation submission, confirmation, rescheduling, rejection, and cancellation.
- Cover patient document metadata authorization and message ownership at the database boundary.
- Cover mock payment persistence, Paystack verification mapping, webhook idempotency, and receipt isolation.
- Add concurrency tests for the unique doctor/time slot index using separate DbContexts.
- Exit gate: every launch-critical write workflow has at least one SQL Server success path and one rollback or constraint-failure path.

### Week 3 — CI hardening and launch evidence, July 27–31

- Make the SQL Server integration job a required pull-request check.
- Add failed-test logs and Testcontainers diagnostics as CI artifacts.
- Run the suite repeatedly to identify order dependence and flaky timing assumptions.
- Set an explicit duration budget and split slow scenarios only when measurement supports it.
- Update the feature inventory and verification checklist with SQL Server evidence.
- Document Docker troubleshooting, image-update policy, and the rollback path for test-infrastructure changes.
- Exit gate: required CI check is green, documented, repeatable, and understood by at least one additional contributor.

## Interview Questions and High-Confidence Answers

### 1. Why not use EF Core InMemory for these tests?

Perfect response: EF Core InMemory is useful for some fast tests, but it is not SQL Server. It cannot prove SQL Server constraints, transaction behavior, provider translations, indexes, or concurrency rules. I keep fast tests for application logic and add a separate SQL Server Testcontainers lane for workflows where database semantics are part of correctness.

### 2. How do you share one database container without tests leaking state?

Perfect response: xUnit creates one collection fixture, which amortizes container startup and migration cost. Before every test, Respawn uses the live SQL Server foreign-key graph to clear business tables while preserving `__EFMigrationsHistory`. The collection is non-parallel, so reset and workflow execution cannot race each other.

### 3. How do you know the rollback test is not only checking EF Core's local state?

Perfect response: the workflow executes and rolls back a real SQL Server transaction, then disposes that context. Verification uses a fresh DbContext and new queries against the container. It checks both that newly inserted appointment and slot rows are absent and that a pre-existing availability row retains its original value.

## Operational Notes

- Docker is required only for the `DatabaseIntegration` lane; ordinary unit and hosted smoke tests remain separate.
- The fixture fails with named migration operations if the runtime EF model differs from the committed migration snapshot; it never suppresses `PendingModelChangesWarning`.
- Container reuse is scoped to one test-process collection. Cross-process reusable containers are deliberately not enabled because CI isolation and deterministic cleanup are more important than saving one startup.
- The SQL Server image is pinned. Image upgrades should happen in a dedicated dependency PR with full verification.
- Respawn is not a migration replacement. Migrations create and evolve the schema; Respawn only clears test data after the schema exists.
- Do not point Respawn at a shared development, staging, or production database. The fixture connection string must come exclusively from Testcontainers.
