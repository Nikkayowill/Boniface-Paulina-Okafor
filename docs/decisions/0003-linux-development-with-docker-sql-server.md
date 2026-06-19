# 0003: Use Docker SQL Server for Linux Development

Date: 2026-05-31

## Status

Accepted

## Context

The original development setup referenced SQL Server LocalDB, which is Windows-only. On Linux, the app fails at startup if it uses `(localdb)\mssqllocaldb`.

## Decision

Use Docker Compose to run SQL Server for Linux development. Use the `Testing` environment with EF Core InMemory when a database is not needed.

## Consequences

- Linux developers can run the app without Windows LocalDB.
- Docker keeps the SQL Server dependency consistent across machines.
- `Testing` mode gives a fast no-database path for UI checks, smoke checks, and simple backend verification.
- Real development data still requires a SQL Server container and migrations.
