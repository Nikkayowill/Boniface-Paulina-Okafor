# UI/UX Direction

## Goal

Create a premium, calm, mobile-first hospital website that feels trustworthy, easy to navigate, and appropriate for Nigerian families using the hospital online. The interface should use less copy, stronger photography, clear actions, and a maintainable design system.

## Design Principles

- Use a restrained healthcare palette: deep cyan, hospital green, white, slate, and small warm gold accents.
- Keep the hero simple: full-width image, short welcome message, one primary CTA: Contact Us.
- Mention WhatsApp lightly where relevant, especially near teleconsultation, without turning the homepage into a feature explainer.
- Prefer real hospital imagery with consistent aspect ratios and `object-fit: cover`.
- Design mobile first, then enhance desktop with wider spacing and multi-column layout.
- Avoid light text on light surfaces, decorative gradients, nested cards, and one-off hex colors in views.

## Source Of Truth

- Public Tailwind tokens: `wwwroot/css/tailwind.input.css`
- Shared public helpers: `wwwroot/css/site.css`
- Bootstrap portal/dashboard styling: `wwwroot/css/portal.css`
- Public homepage structure: `Views/Home/Index.cshtml`
- Public shell/navigation/footer: `Views/Shared/_Layout.cshtml`

## Implementation Checklist

- Keep color changes in theme tokens or CSS variables first.
- Use reusable classes for section width, section spacing, image frames, body copy, and surface cards.
- Keep homepage copy brief and action-oriented.
- Test image containers at mobile, tablet, and desktop widths.
- Verify generated Tailwind output after editing `tailwind.input.css`.
- Compile Razor after significant view changes.

## Priority Improvements

Keep this round focused on polishing the existing product instead of adding new feature areas.

1. Clarify the homepage action hierarchy.
   - Primary actions should be `Contact Us`, `Book Appointment`, and `Teleconsultation`.
   - Keep donation, news, services, and patient education visible, but lower in the page priority.

2. Make public forms feel consistent.
   - Appointment booking, teleconsultation, bill payment, and donation should share the same calm layout pattern.
   - Use consistent intro copy, form spacing, validation styling, button treatment, and confirmation tone.

3. Remove visible encoding artifacts.
   - Replace mojibake text such as broken dash or quote characters with normal ASCII punctuation or valid UTF-8 characters.
   - Prioritize patient-facing views first because small text glitches reduce trust.

4. Give the patient portal one clear next step.
   - Surface the most useful action based on patient state: complete profile, view appointment, join teleconsultation, check messages, or review documents.
   - Keep the existing dashboard counts, but make the next action easier to spot.

5. Make the admin dashboard more action-first.
   - Add or emphasize a compact work queue for pending appointments, pending teleconsultations, bill payment review, and new contact submissions.
   - Avoid heavy analytics until the core operations queue is easy to scan.

6. Replace development fallback copy on public pages.
   - Avoid public text like "details not found" or "placeholder" where a patient can see it.
   - Use helpful fallback language such as "Please contact the hospital to confirm current details."

7. Keep WhatsApp practical and light.
   - Use WhatsApp around teleconsultation updates, confirmation handoff, and contact fallback.
   - Do not turn the homepage into a WhatsApp feature explainer.

## Out Of Scope For This Pass

- Ratings and reviews.
- Advanced analytics dashboards.
- Teleconsultation recording.
- Full medical records expansion.
- Production payment gateway expansion unless required for launch.
