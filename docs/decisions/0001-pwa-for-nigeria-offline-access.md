# 0001: Use a PWA for Offline-Friendly Hospital Access

Date: 2026-05-31

## Status

Accepted

## Context

The hospital serves patients in Nigeria, where mobile-first usage, unstable connectivity, and slower rural networks are realistic operating constraints. Patients may need basic hospital information, appointment guidance, and saved offline pages even when the network is unreliable.

## Decision

Keep the application as a Progressive Web App.

The backend remains ASP.NET Core, while public assets include a web manifest, service worker, offline pages, and cache logic for safe public/offline resources.

## Consequences

- Patients can install the site and get a more app-like experience without app-store distribution.
- Public pages and appointment guidance can remain available during weak connectivity.
- Sensitive patient/admin data must not be broadly cached.
- Service worker changes need deliberate testing because stale caches can affect real users.
