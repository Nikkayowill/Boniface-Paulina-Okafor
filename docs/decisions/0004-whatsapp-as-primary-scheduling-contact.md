# 0004: Provide WhatsApp as a Primary Scheduling Contact

Date: 2026-05-31

## Status

Accepted

## Context

WhatsApp is a familiar communication channel for many Nigerian patients and families. The hospital needs a low-friction way for visitors to ask about appointments immediately, especially from mobile devices.

## Decision

Add a fixed bottom-right WhatsApp click-to-chat widget on the public site. The widget uses the configured `Notifications:WhatsAppNumber` value and opens a prefilled scheduling message.

Backend WhatsApp webhook and notification services remain server-side so credentials, signatures, and delivery logs are not exposed in frontend code.

## Consequences

- Patients get a visible immediate contact option.
- The WhatsApp number can be changed through configuration.
- The frontend only creates a click-to-chat link; all automated WhatsApp API work stays in the backend.
- Floating UI must be checked against the PWA install button and mobile viewport spacing.
