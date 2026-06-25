# Patient Flow Scenarios

These scenarios describe the launch-critical patient journeys for backend and DevOps verification. They are written from the patient point of view so issues can be created against real behavior, not isolated routes.

## Scenario 1: Patient Creates A Verified Account

Persona: A rural patient with unreliable mobile data wants access to the patient portal before needing care urgently.

Expected flow:

1. Patient visits the public website and opens patient registration.
2. Patient creates an account with a unique email and strong password.
3. System sends an email confirmation link.
4. Patient cannot sign in until the email is confirmed.
5. Patient confirms the email from the valid email inbox.
6. Patient signs in and lands in the patient portal.
7. Patient creates or updates their profile.

Launch checks:

- `Authentication:RequireConfirmedAccount` must be enabled outside automated test mode.
- Production and staging must have working SMTP credentials before public registration is advertised.
- The development-only direct confirmation link must not appear in staging or production.

## Scenario 2: Patient Needs Documents During Weak Connectivity

Persona: The same patient wants emergency access to records when connectivity is unreliable.

Current behavior:

- The portal has authenticated document listing and download routes.
- Generic service-worker caching intentionally excludes `/Portal`, `/Patient`, `/uploads`, and private API routes.
- This protects sensitive files from accidental browser cache exposure, but it does not yet provide a true offline document vault.

Required launch decision:

- If offline document access is advertised, build an explicit patient-controlled encrypted offline document vault with opt-in save/remove controls, clear logout cleanup, and browser verification.
- Until that vault exists, copy and support docs should say documents are available after sign-in while online, not guaranteed offline.

## Scenario 3: Patient Requests An In-Person Appointment

Persona: Patient logs in, chooses a department and doctor, selects an available slot, and submits an in-person appointment request.

Expected flow:

1. Patient opens appointment booking.
2. Patient selects department, doctor, date, and slot.
3. System validates the slot is still available.
4. System creates an `AppointmentRequest` and reserves an `AppointmentSlot`.
5. Patient receives confirmation reference and WhatsApp click-to-chat option.
6. Admin queue receives the request.
7. Staff verifies contact before approving.
8. Approved appointment appears in the patient portal appointment list.

Launch checks:

- Slot reservation must be the source of truth.
- Notification and realtime failures must not cause duplicate patient submissions after the request is saved.
- Staff approval must require contact confirmation.

## Scenario 4: Patient Requests A Teleconsultation

Persona: Patient wants remote care with a clinician and has enough data for video, but can call the hospital directly for voice support.

Expected flow:

1. Patient opens teleconsultation booking.
2. Patient can choose `Video consultation` or `Follow-up consultation`.
3. Patient cannot book a phone-call appointment online.
4. Patient provides consent, department, preferred date/time, reason, phone, and optional WhatsApp updates.
5. System creates a pending `TeleconsultationRequest`.
6. Submitted page loads by request id.
7. Admin queue receives the request.
8. Staff confirms, reschedules, completes, or rejects with safe next-step notes.
9. Patient sees the request history and meeting link/status in the portal.

Launch checks:

- The public form must not show phone-call teleconsultation.
- Server-side validation must reject posted `Phone` teleconsultation requests.
- If the patient needs voice support, the public hospital number is the route.
