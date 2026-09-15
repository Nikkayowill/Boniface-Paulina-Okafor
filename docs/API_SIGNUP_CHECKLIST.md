# API And Account Signup Checklist

This list is based on the code currently in the repo. Keep owner-only credentials out of git; use user secrets locally and deployment secrets in hosted environments.

## Required Before Real Launch

| Service | Why It Is Needed | Code/Config Touchpoints | Signup/Setup Needed | Status |
|---|---|---|---|---|
| SMTP email provider | Receipts, admin notifications, patient communication fallback | `SmtpEmailSender`, receipt sender services, `Email:*` | SMTP host, port, SSL mode, username, password, sender address | Owner |
| Public domain and HTTPS hosting | Payment provider webhooks and PWA install trust, once an online payment provider is added | Deployment config, provider dashboards | Domain, HTTPS certificate, public webhook URLs | Owner |

## Strongly Recommended

| Service | Why It Is Useful | Code/Config Touchpoints | Signup/Setup Needed | Status |
|---|---|---|---|---|
| Africa's Talking | SMS notifications for Nigerian users who miss email | `AfricasTalkingNotificationService`, `Notifications:AfricasTalking:*` | Africa's Talking account, API key, username, approved sender id | Owner |
| VAPID web push keys | Browser push notifications for patient portal reminders | `WebPushNotificationService`, `PushNotificationsController`, `VapidKeys:*` | Generate VAPID public/private keys and set `VapidKeys:Subject` | Owner |
| Error/log monitoring | Faster production debugging | ASP.NET logging pipeline | Choose provider later, such as hosting logs, Application Insights, Sentry, or another logger | Owner/Dev |

## Optional Enhancements

| Service | Why It Is Optional | Code/Config Touchpoints | Signup/Setup Needed | Status |
|---|---|---|---|---|
| Google Maps platform | Better embedded map control if the hospital wants a managed map | `Hospital:GoogleMapEmbedUrl` | Not required for current query-based embed; only needed for advanced Maps API usage | Optional |

## Not Currently Implemented

| Service | Notes |
|---|---|
| A live online payment provider | The `Payments:Provider` setting only supports `Disabled` and `Mock` today. Add a real gateway integration when the hospital is ready to take live online donations/bill payments. |
| An automated WhatsApp messaging provider | The hospital only uses a WhatsApp click-to-chat link (`Notifications:WhatsAppNumber`) today. No backend WhatsApp API integration exists. |
| Flutterwave | Mentioned in older frontend handoff text, but no active backend provider is implemented. |
| Twilio | Not used. |
| Firebase | Not used. Browser push uses VAPID/Web Push. |
| SendGrid | Not specifically used. Any SMTP-compatible provider can work. |

## Local Development Defaults

| Area | Local Default |
|---|---|
| Payments | `Payments:Provider=Mock` |
| Notifications | `Notifications:Provider=Lean` |
| Database | Docker PostgreSQL via `docker-compose.yml` |
| WhatsApp widget | Uses `Notifications:WhatsAppNumber`, no API key needed for click-to-chat |

## Owner Secrets To Set

Use these names in user secrets or deployment secrets:

```text
SeedAdmin:Email
SeedAdmin:Password
ConnectionStrings:DefaultConnection
Notifications:AfricasTalking:ApiKey
Notifications:AfricasTalking:Username
Notifications:AfricasTalking:SenderId
Email:SmtpHost
Email:Port
Email:EnableSsl
Email:FromAddress
Email:Username
Email:Password
VapidKeys:PublicKey
VapidKeys:PrivateKey
VapidKeys:Subject
```
