# 0002: Keep ASP.NET Backend Logic Separate From Public Design Work

Date: 2026-05-31

## Status

Accepted

## Context

The existing application already contains backend functionality for appointments, teleconsultations, payments, portals, notifications, uploads, and admin workflows. A design collaborator needs freedom to improve landing pages and public UI without taking ownership of C# business logic or secrets.

## Decision

Keep ASP.NET Core MVC as the backend and system of record. Use Razor views and static assets as the current frontend delivery layer, with a documented frontend/backend contract for future extraction or deeper frontend work.

Design work should focus on public-facing files such as `Views/Home`, `Views/Shared`, `wwwroot/css`, `wwwroot/js`, and `wwwroot/images`.

## Consequences

- Backend functionality is preserved instead of rewritten.
- Security-sensitive flows remain server-controlled.
- A designer can contribute UI safely with a clear boundary.
- If a richer frontend stack is introduced later, it should consume stable backend routes/API contracts rather than duplicating backend logic.
