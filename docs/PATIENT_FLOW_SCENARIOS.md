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

## Scenario 5: Staff Reviews And Approves An In-Person Appointment

Persona: A staff member receives a new appointment request and needs to confirm it safely before the patient shows up.

Business logic present:

- Admin/staff appointment queue lists appointment requests.
- Staff can edit status, assigned doctor, contact method, contact notes, and contact confirmation.
- Approval requires contact confirmation and a valid contact method.
- Approval checks doctor slot conflicts and creates a linked patient portal appointment when a matching patient profile exists.
- Status notifications and realtime updates are best-effort side effects and should not undo the saved decision.

Expected flow:

1. Staff opens the admin appointment queue.
2. Staff opens a pending request.
3. Staff calls or emails the patient.
4. Staff records contact confirmation, method, and notes.
5. Staff approves the request.
6. System reserves or confirms the doctor slot.
7. Patient sees the approved appointment in the portal when linked by email/profile.
8. Staff can reject the request instead when the hospital cannot honor it.

Launch checks:

- Approval without contact confirmation must fail.
- Approval into a conflicting doctor/time slot must fail.
- Rejection must persist and notify safely.

Issue candidates:

- Verify admin appointment approval against SQL Server.
- Verify doctor slot conflict handling during appointment approval.
- Verify patient portal appointment linking after approval.

## Scenario 6: Staff Manages A Teleconsultation Request

Persona: A staff member reviews remote-care requests and needs to give the patient a safe next step.

Business logic present:

- Admin/staff teleconsultation queue lists requests.
- Staff can confirm, reschedule, complete, or reject a request.
- Confirmed/rescheduled requests require either a meeting link or clear next-step notes.
- Rejected requests require admin notes.
- Patient history can show status, meeting link, and notes.

Expected flow:

1. Patient submits a video or follow-up teleconsultation request.
2. Staff opens the admin teleconsultation queue.
3. Staff reviews reason, department, preferred date/time, and WhatsApp opt-in.
4. Staff confirms with a meeting link or reschedules with notes.
5. Patient sees updated status and next steps in the portal.
6. Staff can mark the request completed after care is delivered.

Launch checks:

- Confirm/reschedule without meeting link or notes must fail.
- Reject without safer next-step notes must fail.
- Notification failures must not roll back status changes.

Issue candidates:

- Verify admin teleconsultation status workflow.
- Verify patient teleconsultation history reflects admin decisions.
- Verify WhatsApp opt-in status is visible and persisted.

## Scenario 7: Patient Cancels A Pending Booking Or Future Appointment

Persona: A patient can no longer attend and wants to cancel without calling the hospital.

Business logic present:

- Patient portal appointment list merges confirmed portal appointments with public booking requests by email.
- Patient can cancel a pending booking request.
- Patient can cancel a future scheduled appointment.
- Approved or rejected booking requests cannot be cancelled by the patient.
- Past or completed appointments cannot be cancelled by the patient.

Expected flow:

1. Patient signs in and opens `My Appointments`.
2. Patient sees both pending booking requests and confirmed scheduled appointments.
3. Patient cancels a pending request before staff approval.
4. System removes the pending request.
5. Patient cancels a future scheduled appointment.
6. System marks the scheduled appointment as cancelled.

Launch checks:

- Cancellation must be scoped to the signed-in patient's profile/email.
- Cancelling someone else's appointment id must return `NotFound` or access denial.
- Cancelled appointments should no longer look active to the patient.

Issue candidates:

- Verify patient cancellation rules.
- Add cancellation confirmation UX if missing.
- Verify cancelled slots are handled correctly by staff workflows.

## Scenario 8: Patient Uploads Documents While Online

Persona: A patient uploads lab results or referral documents before an appointment.

Business logic present:

- Patient document upload/list/delete exists under `/Portal/Documents`.
- Upload requires a patient profile.
- Allowed file types are PDF, JPG, PNG, DOC, and DOCX.
- Max upload size is 10 MB.
- Delete is scoped to the signed-in patient's profile.

Expected flow:

1. Patient signs in and creates a profile if needed.
2. Patient opens `My Documents`.
3. Patient uploads a valid document with title and optional description.
4. System stores the file under patient-document uploads and creates a database record.
5. Patient can list, view/download, and delete their own document.

Launch checks:

- Invalid file extensions and MIME types must be rejected.
- Files over 10 MB must be rejected.
- Patient must not be able to delete or view another patient's document through id guessing.
- Offline document access is not implemented as a true encrypted vault yet.

Issue candidates:

- Verify patient document upload/delete in browser.
- Add document upload validation coverage.
- Decide whether to build encrypted offline document vault before advertising offline documents.

## Scenario 9: Patient Sends A Secure Portal Message

Persona: A patient has a non-urgent question for the hospital after creating their profile.

Business logic present:

- Patient messages are profile-scoped.
- Patient can list messages and send a subject/body.
- The message is saved to `PatientMessages`.

Expected flow:

1. Patient signs in and opens `Messages`.
2. Patient sends a non-urgent message with subject and body.
3. System saves the message against the patient's profile.
4. Patient sees the message in their message list.

Launch checks:

- Message send must require a patient profile.
- Message list must be scoped to the signed-in patient only.
- Emergency care copy should make clear this is not for urgent symptoms.

Known gap:

- The inventory shows patient send/list. A full admin reply workflow is not clearly established in the current backend flow and should not be promised until verified or built.

Issue candidates:

- Verify patient message send/list.
- Define whether staff reply workflow is required for launch.
- Add non-urgent/emergency warning copy if missing.

## Scenario 10: Patient Pays A Hospital Bill

Persona: A patient receives an invoice/reference number and wants to pay online.

Business logic present:

- Bill payment form exists at `/BillPayments`.
- Invoice/reference is normalized to uppercase.
- Invoice/reference must use letters, numbers, and hyphens.
- Duplicate invoice/reference numbers are rejected.
- Sandbox mode requires explicit acknowledgement.
- Payment provider abstraction supports mock and Paystack.
- Receipt route requires matching id and invoice/reference.

Expected flow:

1. Patient opens bill payment.
2. Patient enters invoice/reference, name, email, phone, amount, and currency.
3. System rejects invalid or duplicate invoice/reference.
4. In sandbox/mock mode, patient acknowledges test mode.
5. System records pending payment and initializes payment provider.
6. Mock mode immediately records sandbox-approved/paid payment.
7. Patient lands on receipt.
8. Admin can review bill payments.

Launch checks:

- Duplicate invoice/reference must be blocked.
- Receipt URLs must not expose a payment without matching reference.
- Provider callback/webhook must be idempotent.
- Live Paystack requires configured keys and signed webhook verification.

Issue candidates:

- Verify mock bill payment end to end.
- Verify duplicate invoice rejection.
- Verify Paystack sandbox callback and webhook once keys exist.

## Scenario 11: Donor Makes A Donation

Persona: A community member wants to donate to the hospital online.

Business logic present:

- Donation form exists at `/Donation`.
- Donation requires donor email for online processing and receipt.
- Donation reference is generated server-side.
- Currency is normalized and validated as a 3-letter code.
- Payment provider abstraction supports mock and Paystack.
- Receipt route requires matching id and payment reference.

Expected flow:

1. Donor opens donation page.
2. Donor enters name, email, amount, and currency.
3. System generates a unique donation reference.
4. System initializes provider payment.
5. Mock mode records sandbox-approved donation and sends receipt best-effort.
6. Donor lands on receipt.
7. Callback/webhook can verify and update paid status.

Launch checks:

- Server-generated reference must remain unique.
- Receipt route must require the correct reference.
- Receipt email failure must not lose the payment record.
- Live provider flow requires Paystack credentials and webhook validation.

Issue candidates:

- Verify mock donation end to end.
- Verify receipt access requires matching reference.
- Verify Paystack donation callback/webhook once keys exist.

## Scenario 12: Public Visitor Sends A Contact Request

Persona: A visitor asks a general question without creating an account.

Business logic present:

- Contact form saves `ContactSubmission`.
- Admin can list, view details, and delete submissions.
- Submission uses anti-forgery protection.

Expected flow:

1. Visitor opens contact page.
2. Visitor submits name, email, subject, and message.
3. System saves submission and redirects with success state.
4. Admin opens contact submissions queue.
5. Admin reviews details and deletes completed/invalid submissions.

Launch checks:

- Invalid contact form should not save.
- Admin-only access must protect contact submissions.
- Deleting a submission should not affect unrelated records.

Issue candidates:

- Verify public contact form to admin queue.
- Verify admin-only access for contact submissions.

## Scenario 13: Admin Maintains Doctors, Departments, And News

Persona: Hospital admin needs public content to stay accurate without developer help.

Business logic present:

- Admin doctors CRUD exists.
- Doctor slug generation is covered by tests.
- Department CRUD exists.
- CMS/news posts exist with publish state and slug routes.
- Public doctors/services/news pages read from database.

Expected flow:

1. Admin creates or edits a department.
2. Admin creates or edits a doctor and assigns department.
3. Public doctors/team/profile pages reflect the change.
4. Admin creates a news post.
5. Published posts appear publicly; unpublished posts do not.

Launch checks:

- Doctor slugs must be stable and unique.
- Unpublished posts must not show publicly.
- Public pages should tolerate missing optional images.

Issue candidates:

- Verify department CRUD.
- Verify doctor CRUD and public profile route.
- Verify post publish/unpublish behavior.

## Scenario 14: Admin Manages Users And Patient Profiles

Persona: Hospital admin needs to create staff or link patients to profiles.

Business logic present:

- Admin user management exists.
- Admin patient profile CRUD exists.
- Admin patient profile creation can assign the `Patient` role.
- Admin can attach/delete patient documents.

Expected flow:

1. Admin creates or updates a user.
2. Admin assigns appropriate role.
3. Admin creates a patient profile linked to the user.
4. Admin uploads a patient document.
5. Patient can see their own profile/documents after sign-in.

Launch checks:

- Only admins should manage users and roles.
- Patient role assignment must happen reliably.
- Patient document access must remain scoped to the linked patient.

Issue candidates:

- Verify admin user and role management.
- Verify admin patient profile linking.
- Verify admin-uploaded document appears for the correct patient.

## Scenario 15: WhatsApp Scheduling Conversation

Persona: A patient has poor web access but can message the hospital on WhatsApp.

Business logic present:

- WhatsApp webhook verify/receive routes exist.
- WhatsApp scheduling services and fallback parsing exist.
- Outbound WhatsApp templates depend on provider credentials.

Expected flow:

1. Patient sends a WhatsApp scheduling message.
2. Webhook validates provider request.
3. System parses intent using AI/fallback rules.
4. System offers available appointment slots.
5. Patient confirms a slot.
6. System reserves the slot and records the request/session.

Launch checks:

- Webhook signature/verify token must be correct.
- AI failures must fall back to local rules.
- Slot confirmation must reuse the same source-of-truth reservation logic as web booking.
- Provider credentials and templates are required before advertising this.

Issue candidates:

- Verify WhatsApp webhook challenge.
- Verify inbound scheduling fallback flow.
- Verify outbound template configuration in staging.

## Scenario 16: Push Notifications For Logged-In Patients

Persona: Patient wants browser reminders and updates after signing into the portal.

Business logic present:

- Push subscription save/delete/test routes exist.
- Push notification service uses VAPID settings.
- Push cleanup service removes stale subscriptions.

Expected flow:

1. Patient signs in.
2. Patient enables browser notifications.
3. Browser subscription is saved.
4. Hospital sends a test or appointment-related push.
5. Failed/stale subscriptions are tracked and cleaned.

Launch checks:

- VAPID keys must be configured before production push is advertised.
- Patient can delete/disable subscription.
- Push payloads must avoid sensitive medical details.

Issue candidates:

- Verify push subscription save/delete.
- Verify browser receives test push in staging.
- Review push payload content for privacy.

## Scenario 17: Offline And Low-Bandwidth Mode

Persona: Patient loses connectivity after browsing the site.

Business logic present:

- Service worker precaches public shell assets and offline pages.
- Public pages can use offline fallback.
- Private, admin, payment, upload, hub, and document routes are intentionally network-only.
- Offline appointment helper assets exist.

Expected flow:

1. Patient visits public pages while online.
2. Connectivity drops.
3. Public offline fallback loads instead of a browser error.
4. Secure/private features clearly require network.
5. No patient documents, admin pages, receipts, or private portal data are generically cached.

Launch checks:

- Offline messaging must be honest and not imply private records are cached.
- Sensitive cache exclusions must remain in place.
- If offline documents are required, build a separate encrypted opt-in vault.

Issue candidates:

- Verify service worker offline fallback in browser devtools.
- Verify private route cache exclusions.
- Decide offline document vault scope.
