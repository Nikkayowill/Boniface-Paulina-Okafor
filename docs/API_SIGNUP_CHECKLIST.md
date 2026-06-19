# API And Account Signup Checklist

This list is based on the code currently in the repo. Keep owner-only credentials out of git; use user secrets locally and deployment secrets in hosted environments.

## Required Before Real Launch

| Service | Why It Is Needed | Code/Config Touchpoints | Signup/Setup Needed | Status |
|---|---|---|---|---|
| Meta WhatsApp Cloud API | WhatsApp appointment scheduling, teleconsultation updates, webhook receive/status events | `MetaWhatsAppNotificationService`, `WhatsAppWebhooksController`, `Notifications:WhatsApp:*` | Meta developer app, WhatsApp Business phone number, phone number id, access token, app secret, webhook verify token, approved templates | Owner |
| Paystack | Online donations and bill payments in Nigeria | `PaymentGateway.cs`, `PaystackWebhooksController`, `Payments:Paystack:*` | Paystack account, public key, secret key, webhook URL, signed webhook test | Owner |
| SMTP email provider | Receipts, admin notifications, patient communication fallback | `SmtpEmailSender`, receipt sender services, `Email:*` | SMTP host, port, SSL mode, username, password, sender address | Owner |
| Public domain and HTTPS hosting | WhatsApp/Paystack webhooks and PWA install trust | Deployment config, provider dashboards | Domain, HTTPS certificate, public webhook URLs | Owner |

## Strongly Recommended

| Service | Why It Is Useful | Code/Config Touchpoints | Signup/Setup Needed | Status |
|---|---|---|---|---|
| Africa's Talking | SMS notifications for Nigerian users who miss email/WhatsApp | `AfricasTalkingNotificationService`, `Notifications:AfricasTalking:*` | Africa's Talking account, API key, username, approved sender id | Owner |
| VAPID web push keys | Browser push notifications for patient portal reminders | `WebPushNotificationService`, `PushNotificationsController`, `VapidKeys:*` | Generate VAPID public/private keys and set `VapidKeys:Subject` | Owner |
| Error/log monitoring | Faster production debugging | ASP.NET logging pipeline | Choose provider later, such as hosting logs, Application Insights, Sentry, or another logger | Owner/Dev |

## Optional Enhancements

| Service | Why It Is Optional | Code/Config Touchpoints | Signup/Setup Needed | Status |
|---|---|---|---|---|
| Scheduling AI provider | Better parsing of free-text WhatsApp scheduling requests | `AiSchedulingService`, `SchedulingAi:Endpoint`, `SchedulingAi:ApiKey`, `SchedulingAi:Model` | AI API endpoint and key; app already falls back to local rules if absent | Optional |
| Google Maps platform | Better embedded map control if the hospital wants a managed map | `Hospital:GoogleMapEmbedUrl` | Not required for current query-based embed; only needed for advanced Maps API usage | Optional |

## Not Currently Implemented

| Service | Notes |
|---|---|
| Flutterwave | Mentioned in older frontend handoff text, but no active backend provider is implemented. Current payment provider is Paystack plus Mock. |
| Twilio | Not used. WhatsApp integration is Meta WhatsApp Cloud API. |
| Firebase | Not used. Browser push uses VAPID/Web Push. |
| SendGrid | Not specifically used. Any SMTP-compatible provider can work. |

## Local Development Defaults

| Area | Local Default |
|---|---|
| Payments | `Payments:Provider=Mock` |
| Notifications | `Notifications:Provider=Lean` |
| Database | Docker SQL Server via `docker-compose.yml` |
| WhatsApp widget | Uses `Notifications:WhatsAppNumber`, no API key needed for click-to-chat |
| Scheduling AI | Falls back to local parsing rules when no endpoint/key is configured |

## Owner Secrets To Set

Use these names in user secrets or deployment secrets:

```text
SeedAdmin:Email
SeedAdmin:Password
ConnectionStrings:DefaultConnection
Notifications:WhatsApp:Enabled
Notifications:WhatsApp:PhoneNumberId
Notifications:WhatsApp:AccessToken
Notifications:WhatsApp:AppSecret
Notifications:WhatsApp:WebhookVerifyToken
Notifications:AfricasTalking:ApiKey
Notifications:AfricasTalking:Username
Notifications:AfricasTalking:SenderId
Payments:Paystack:PublicKey
Payments:Paystack:SecretKey
Email:SmtpHost
Email:Port
Email:EnableSsl
Email:FromAddress
Email:Username
Email:Password
VapidKeys:PublicKey
VapidKeys:PrivateKey
VapidKeys:Subject
SchedulingAi:Endpoint
SchedulingAi:ApiKey
SchedulingAi:Model
```
