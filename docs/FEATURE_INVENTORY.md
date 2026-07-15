# Feature Inventory

This document tracks implemented functionality from the codebase so restoration work is based on evidence, not memory.

Status meanings:

- `Verified`: covered by automated tests or a direct local verification step.
- `Code-present`: controllers, views, models, services, or assets exist, but the feature still needs full manual verification in `Development` with SQL Server.
- `External-config`: code exists, but full live behavior depends on provider credentials or webhook setup.
- `Manual`: must be verified by using the running app.

## Platform And Infrastructure

| Feature | Primary Files | Route/Entry Point | Status | Verification |
|---|---|---|---|---|
| ASP.NET Core MVC app startup | `Program.cs`, `Okafor-.NET.csproj` | `dotnet run --project Okafor-.NET.csproj` | Verified | `dotnet build tests/Okafor.NET.Tests/Okafor.NET.Tests.csproj` |
| Testing-mode startup with InMemory DB | `Program.cs` | `ASPNETCORE_ENVIRONMENT=Testing dotnet run --project Okafor-.NET.csproj --no-launch-profile` | Verified | `ApplicationIntegrationTests`, smoke tests when live server is running |
| Development-mode SQL Server startup | `Program.cs`, `docker-compose.yml`, `appsettings.Development.json` | `docker compose up -d`, then `dotnet run` | Code-present | Manual SQL Server verification required |
| EF Core migrations | `Data/Migrations/*`, `Data/ApplicationDbContext.cs` | Development startup calls `Database.MigrateAsync()` | Code-present | Start with SQL Server and confirm schema/seed data |
| Health check | `Program.cs` | `/health` | Verified | `SmokeTests.HealthCheck_Endpoint_Returns200` |
| Security headers | `Program.cs` | middleware on all responses | Verified | `SmokeTests.ResponseHeaders_Include_Security_Basics` |
| SignalR booking hub | `Hubs/BookingHub.cs`, `Program.cs` | `/hubs/bookings` | Code-present | Manual admin/public booking realtime check |
| CI build/test | `.github/workflows/ci.yml` | GitHub Actions | Code-present | Verify after first push/PR |
| Husky pre-push guard | `.husky/pre-push`, `package.json` | `git push` | Verified locally | Direct push from `master` blocked locally |

## Public Site

| Feature | Primary Files | Route/Entry Point | Status | Verification |
|---|---|---|---|---|
| Homepage | `Controllers/HomeController.cs`, `Views/Home/Index.cshtml` | `/` | Verified | `ApplicationIntegrationTests.HomePage_ReturnsOk`, smoke tests |
| About page | `HomeController`, `Views/Home/About.cshtml` | `/Home/About` | Code-present | Manual page check |
| Services page | `HomeController`, `Views/Home/Services.cshtml` | `/Home/Services` | Code-present | Manual page check |
| Doctors public listing | `HomeController`, `Views/Home/Doctors.cshtml` | `/Home/Doctors` | Verified by smoke route `/Doctors` | Manual content check |
| Team page | `HomeController`, `Views/Home/Team.cshtml` | `/Home/Team` | Code-present | Manual page check |
| Founder psychotherapy profile | `ClinicalDataSeed`, `Views/Home/Team.cshtml`, `Views/Home/DoctorProfile.cshtml` | `/doctors/rev-fr-dr-toochukwu-bartholomew-okafor` | Code-present | Verify profile and preselected teleconsultation request |
| Doctor public profile | `HomeController`, `Views/Home/DoctorProfile.cshtml` | `/doctors/{slug}` | Code-present | Manual route check with seeded doctor slug |
| News listing | `HomeController`, `Views/Home/News.cshtml` | `/Home/News` | Code-present | Manual page check |
| News detail by slug | `HomeController`, `Views/Home/NewsDetail.cshtml` | `/news/{slug}` | Code-present | Manual route check with seeded post slug |
| Patient information hub | `HomeController`, `Views/Home/PatientInformationHub.cshtml` | `/Home/PatientInformationHub` | Verified partial | Accessibility tests |
| Contact form | `HomeController`, `Models/ContactSubmission.cs`, admin contact views | `/Home/Contact` | Code-present | Manual POST + admin review |
| Site search | `HomeController` | `/Home/Search?query=...` | Code-present | Manual route check |
| Privacy page | `HomeController`, `Views/Home/Privacy.cshtml` | `/Home/Privacy` | Launch draft present | Owner/privacy adviser wording approval |
| Friendly error and status pages | `HomeController`, `Views/Shared/Error.cshtml`, `Views/Home/HttpStatus.cshtml` | error pipeline | Code-present | Production-mode 404/500 route check |
| WhatsApp floating click-to-chat | `Views/Shared/_Layout.cshtml`, `wwwroot/css/site.css` | Public layout | Verified render check | Link renders with configured `Notifications:WhatsAppNumber` |

## Appointments And Scheduling

| Feature | Primary Files | Route/Entry Point | Status | Verification |
|---|---|---|---|---|
| Appointment request page | `Controllers/AppointmentRequestsController.cs`, `Views/AppointmentRequests/Create.cshtml` | `/AppointmentRequests/Create` | Verified | smoke test |
| Appointment request submit | `AppointmentRequestsController`, `Models/AppointmentRequest.cs`, `LeanNotificationService` | `POST /AppointmentRequests/Create` | Code-present | Manual submit with SQL Server |
| Appointment submitted page | `Views/AppointmentRequests/Submitted.cshtml` | `/AppointmentRequests/Submitted` | Code-present | Manual route after submit |
| Available slots lookup | `AppointmentRequestsController`, `AvailabilityService` | `/AppointmentRequests/GetAvailableSlots` | Code-present | `AppointmentSchedulingTests` cover service logic |
| Slot booking endpoint | `AppointmentRequestsController`, `AppointmentSlot.cs`, `BookSlotViewModel.cs` | `POST /AppointmentRequests/BookSlot` | Code-present | `AppointmentSchedulingTests` cover slot reservation logic |
| Doctor availability admin page | `Areas/Admin/Controllers/AvailabilityController.cs`, `Areas/Admin/Views/Availability/Index.cshtml` | `/Admin/Availability` | Code-present | Manual admin check |
| Availability save/generate slots | `AvailabilityController`, `DoctorAvailability.cs`, `AppointmentSlot.cs` | `SaveAvailability`, `GenerateSlots` | Verified service | `AppointmentSchedulingTests` |
| Reminder background service | `AppointmentReminderService.cs`, `BackgroundTaskOptions.cs` | hosted service | Configurable service | SQL-backed reminder check; always-on hosting decision |
| Booking realtime notifications | `BookingHub.cs`, `booking-realtime.js` | `/hubs/bookings` | Code-present | Manual browser/admin check |

## Teleconsultations

| Feature | Primary Files | Route/Entry Point | Status | Verification |
|---|---|---|---|---|
| Teleconsultation request page | `Controllers/TeleconsultationsController.cs`, `Views/Teleconsultations/Create.cshtml` | `/Teleconsultations/Create` | Verified | `ApplicationIntegrationTests.TeleconsultationCreatePage_ReturnsOk` |
| Teleconsultation submit | `TeleconsultationsController`, `TeleconsultationRequest.cs` | `POST /Teleconsultations/Create` | Code-present | Manual submit with SQL Server |
| Phone-call teleconsultation removal | `TeleconsultationsController`, `Views/Teleconsultations/Create.cshtml` | `/Teleconsultations/Create` | Code-present | Public form omits Phone; server rejects posted Phone requests |
| Teleconsultation submitted page | `Views/Teleconsultations/Submitted.cshtml` | `/Teleconsultations/Submitted?reference={protected-reference}` | Code-present | Numeric record IDs are protected with ASP.NET Core Data Protection before redirect |
| Admin teleconsultation queue | `Areas/Admin/Controllers/TeleconsultationsController.cs` | `/Admin/Teleconsultations` | Code-present | Manual admin check |
| Admin teleconsultation status edit | `Areas/Admin/Views/Teleconsultations/Edit.cshtml` | `/Admin/Teleconsultations/Edit/{id}` | Code-present | Manual admin check |
| Patient teleconsultation history | `Areas/Patient/Controllers/TeleconsultationsController.cs` | `/Portal/Teleconsultations` | Code-present | Manual patient check |
| WhatsApp opt-in support | `TeleconsultationRequest.cs`, `MetaWhatsAppNotificationService.cs` | request form + notification service | Verified service | `WhatsAppIntegrationTests` |

## Patient Portal

| Feature | Primary Files | Route/Entry Point | Status | Verification |
|---|---|---|---|---|
| Patient area authorization | `Areas/Patient/Controllers/PatientBaseController.cs` | `/Portal/*` | Code-present | Manual auth check |
| Patient dashboard | `DashboardController`, `Areas/Patient/Views/Dashboard/Index.cshtml` | `/Portal/Dashboard` | Code-present | Manual patient check |
| Patient profile create/edit | `ProfileController`, `PatientProfile.cs` | `/Portal/Profile` | Code-present | Manual patient check |
| Patient appointment list | `AppointmentsController`, `PatientAppointment.cs` | `/Portal/Appointments` | Code-present | Manual patient check |
| Calendar download | `AppointmentsController.DownloadCalendar` | `/Portal/Appointments/DownloadCalendar` | Code-present | Manual download check |
| Patient cancellation | `AppointmentsController.Cancel` | `/Portal/Appointments/Cancel` | Code-present | Manual patient check |
| Patient document upload/list/delete | `DocumentsController`, `PatientDocument.cs` | `/Portal/Documents` | Code-present | Manual upload/delete check |
| Offline patient document access | `DocumentsController`, `service-worker.js`, `encrypted-offline-store.js` | `/Portal/Documents` | Manual | Not implemented as true offline document vault; private routes/uploads are intentionally excluded from generic service-worker caching |
| Patient messages | `MessagesController`, `PatientMessage.cs` | `/Portal/Messages` | Code-present | Manual send/list check |
| Push notification component | `ViewComponents/PushNotificationsViewComponent.cs` | patient dashboard component | Verified partial | `PushNotificationCoreTests` |

## Admin And Staff Operations

| Feature | Primary Files | Route/Entry Point | Status | Verification |
|---|---|---|---|---|
| Admin area authorization | `AdminBaseController.cs`, controller attributes | `/Admin/*` | Code-present | Manual role check |
| Admin dashboard | `Areas/Admin/Controllers/DashboardController.cs` | `/Admin/Dashboard` | Code-present | Manual admin check |
| Admin appointment queue | `Areas/Admin/Controllers/AppointmentRequestsController.cs` | `/Admin/AppointmentRequests` | Code-present | Manual admin/staff check |
| Appointment approval/edit/delete | `Admin/AppointmentRequestsController` | details/edit/delete actions | Code-present | Manual workflow check |
| Admin patient profiles | `PatientProfilesController` | `/Admin/PatientProfiles` | Code-present | Manual admin check |
| Admin patient appointments | `PatientAppointmentsController` | `/Admin/PatientAppointments` | Code-present | Manual admin check |
| Admin patient document upload/delete | `PatientProfilesController` | upload/delete document actions | Code-present | Manual upload/delete check |
| Admin user management | `UsersController` | `/Admin/Users` | Code-present | Manual admin check |
| Admin CMS/news posts | `PostsController`, `Post.cs` | `/Admin/Posts` | Code-present | Manual create/edit/publish check |
| Admin contact submissions | `ContactSubmissionsController` | `/Admin/ContactSubmissions` | Code-present | Manual check after contact POST |
| Admin bill payment review | `Areas/Admin/Controllers/BillPaymentsController.cs` | `/Admin/BillPayments` | Code-present | Manual admin/staff check |

## Doctors And Departments Management

| Feature | Primary Files | Route/Entry Point | Status | Verification |
|---|---|---|---|---|
| Admin doctors CRUD | `Controllers/DoctorsController.cs`, `Views/Doctors/*` | `/Doctors/*`, admin route mapping | Verified partial | `DoctorsControllerTests` |
| Doctor slug generation | `DoctorsController` | create/edit | Verified | `DoctorsControllerTests` |
| Department CRUD | `Controllers/DepartmentsController.cs`, `Views/Departments/*` | `/Departments/*`, admin route mapping | Code-present | Manual admin check |
| Department doctor lookup | `DoctorsController.GetByDepartment` | `/Doctors/GetByDepartment` | Code-present | Manual booking form check |

## Payments And Donations

| Feature | Primary Files | Route/Entry Point | Status | Verification |
|---|---|---|---|---|
| Donation form and program designation | `DonationController`, `Donation.cs` | `/Donation` | Code-present | Manual mock payment and designation check |
| Donation callback/receipt | `DonationController`, `DonationReceiptEmailSender.cs` | `/Donation/Callback`, `/Donation/Receipt/{id}` | Code-present | Manual callback, designation, and receipt check |
| Donation admin review and purpose filtering | `Areas/Admin/Controllers/DonationsController.cs` | `/Admin/Donations` | Code-present | Manual admin review and designation filter check |
| Bill payment form | `BillPaymentsController`, `BillPayment.cs` | `/BillPayments` | Code-present | Manual mock payment check |
| Bill payment callback/receipt | `BillPaymentsController`, `BillPaymentReceiptEmailSender.cs` | `/BillPayments/Callback`, `/BillPayments/Receipt/{id}` | Code-present | Manual callback/receipt check |
| Mock payment provider | `PaymentGateway.cs` | `Payments:Provider=Mock` | Code-present | Manual mock flow check |
| Paystack payment provider | `PaymentGateway.cs` | `Payments:Provider=Paystack` | External-config | Requires Paystack keys |
| Paystack webhook | `PaystackWebhooksController.cs` | `/webhooks/paystack` | External-config | Requires signed webhook test |

## Notifications And Integrations

| Feature | Primary Files | Route/Entry Point | Status | Verification |
|---|---|---|---|---|
| Admin integration readiness | `Areas/Admin/Controllers/IntegrationsController.cs`, `IntegrationConfiguration.cs` | `/Admin/Integrations` | Code-present | Admin configuration review, then controlled staging provider checks |
| Email notification service | `LeanNotificationService.cs`, `SmtpEmailSender.cs` | notification service | Verified failure path | `NotificationDeliveryTests` |
| SMS notification service | `AfricasTalkingNotificationService.cs` | notification service | External-config | Requires Africa's Talking credentials |
| Composite notification routing | `CompositeNotificationService.cs`, `IntegrationConfiguration.cs` | `Notifications:Provider` | Code-present | Manual config matrix check |
| WhatsApp Cloud API send | `MetaWhatsAppNotificationService.cs` | notification service | Verified service | `WhatsAppIntegrationTests` |
| WhatsApp webhook verify/receive | `WhatsAppWebhooksController.cs` | `/webhooks/whatsapp`, `/api/whatsapp/webhook`, `/api/whatsapp/receive` | Verified service | `WhatsAppIntegrationTests` |
| WhatsApp scheduling assistant | `AiSchedulingService.cs`, `WhatsAppScheduling*` services | inbound WhatsApp pipeline | Code-present | Service tests partial, manual webhook conversation check |
| Scheduling AI fallback rules | `AiSchedulingService.cs` | service fallback | Code-present | Manual/unit coverage to expand |
| Push subscription save/delete/test | `PushNotificationsController.cs`, `WebPushNotificationService.cs` | `/PushNotifications/*` | Verified partial | `PushNotificationCoreTests`; manual browser check |
| Push cleanup service | `PushSubscriptionCleanupService.cs` | hosted service | Code-present | Manual/log check |

## PWA And Offline

| Feature | Primary Files | Route/Entry Point | Status | Verification |
|---|---|---|---|---|
| Web manifest | `wwwroot/site.webmanifest`, layout link | `/site.webmanifest` | Verified | curl/manual asset check |
| Service worker | `wwwroot/service-worker.js`, `pwa-register.js` | `/service-worker.js` | Verified | `ServiceWorkerTests`, curl/manual asset check |
| Offline fallback page | `wwwroot/offline.html` | `/offline.html` | Verified | curl/manual asset check |
| Offline appointments page | `wwwroot/offline-appointments.html`, `pwa-appointments.js` | `/offline-appointments.html` | Verified tests | `PWAAppointmentsTests` |
| Encrypted offline store | `encrypted-offline-store.js` | browser JS | Code-present | Manual browser check |
| Offline state UI | `offline-state.js` | browser JS | Code-present | Manual browser check |
| PWA install flow | `pwa-register.js` | browser install prompt | Verified tests | `PWARegistrationTests` |
| Sensitive route cache exclusions | `service-worker.js` | service worker fetch logic | Verified | `ServiceWorkerTests` |

## File Uploads And Static Assets

| Feature | Primary Files | Route/Entry Point | Status | Verification |
|---|---|---|---|---|
| Patient document uploads | `DocumentsController`, `PatientProfilesController`, `PatientDocumentStorageService`, `App_Data/patient-documents` | patient/admin upload forms | Code-present | Manual upload/download/delete check |
| CMS post thumbnails | `PostsController`, `wwwroot/uploads/posts` | admin post create/edit | Code-present | Manual upload check |
| Image fallback service | `ImageService.cs` | public pages | Verified | `ImageServiceTests` |
| Tailwind CSS build | `package.json`, `wwwroot/css/tailwind.input.css` | `npm run build:css` | Verified | command completed successfully |

## Highest Priority Gaps To Verify Next

1. SQL Server Development mode: container health, database creation, migrations, seed data.
2. Admin login and seeded admin credentials using user secrets or local config.
3. Full appointment request to admin approval workflow with SQL Server.
4. Full teleconsultation request to admin status update workflow with SQL Server.
5. Patient registration/profile/documents/messages with SQL Server.
6. Mock donation and bill payment flows end to end.
7. Provider-specific live configs: SMTP, Africa's Talking, WhatsApp Cloud API, Paystack, VAPID.
