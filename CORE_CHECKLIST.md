# Core Implementation Checklist

Updated: May 3, 2026

## Locked Down
- Backend startup: identity, roles, security headers, health checks, SignalR, background reminders, push cleanup.
- Patient portal: dashboard, profile, appointments, documents, messages, teleconsultation history.
- Public flows: appointment requests, teleconsultation requests, donations, bill payments, doctors, departments, news.
- Admin flows: patient records, appointment approvals, teleconsultation review, availability, users, content, payments.
- PWA: service worker, offline public pages, offline appointment fallback, install flow, push subscription lifecycle.
- Push notifications: endpoint hashing, subscription failure tracking, stale cleanup, reusable dashboard component.
- Notifications: email baseline, Africa's Talking SMS provider, WhatsApp-ready teleconsultation channel.
- WhatsApp webhooks: verification endpoint, delivery status ingestion, inbound reply logging.
- Teleconsultation admin timeline: email, SMS, WhatsApp, delivery, read, failure, and inbound reply events.

## Current Focus
- Teleconsultations should feel simple for Nigerian patients: phone-first, WhatsApp-friendly, low-friction.
- Patients can opt in to WhatsApp updates when requesting a teleconsultation.
- Staff status changes can trigger WhatsApp template updates when Meta Cloud API credentials and templates are configured.
- The submitted page gives patients a one-tap WhatsApp handoff to message the hospital with their request reference.
- Admin status updates require safer next-step notes when the request is rejected, rescheduled, or confirmed without a meeting link.

## Next Hardening Targets
- Add approved Meta WhatsApp templates in Business Manager:
  - `teleconsultation_received`
  - `teleconsultation_update`
- Add patient-side edit/cancel request rules before clinical review.
- Add a staff inbox for inbound WhatsApp replies that need follow-up.
