# Frontend Design Model Notes

Use this as meeting context for deciding how React should enter the repo without throwing away the working ASP.NET Core backend.

## Current Navbar Ownership

- Markup: `Views/Shared/_Layout.cshtml`
- Header/menu behavior: `wwwroot/js/navigation.js`
- Header/menu visual rules: `wwwroot/css/site.css`
- Utility classes and layout primitives: `wwwroot/css/tailwind.css`
- Public homepage-specific styles: `wwwroot/css/public-site.css`

The navbar should not depend on a CDN script. Header open/close and sticky scroll behavior now run from local `site.js`. Alpine is still used by the appointment booking widget, so that should be vendored or replaced before launch if offline/low-bandwidth reliability is a priority.

## Why Styles Can Look Broken

- The public layout relies heavily on generated Tailwind utility classes from `wwwroot/css/tailwind.css`.
- If `npm run build:css` has not been run after class changes, new utility classes may not exist in the generated CSS.
- If `tailwind.css`, `site.css`, or `public-site.css` fail to load, the page can look unstyled and the mobile nav can appear open.
- CDN dependencies are fragile for Nigeria/offline-first usage: Google Fonts, Alpine CDN, and SignalR CDN should be reviewed.
- There is style overlap between Tailwind utilities, `site.css`, `public-site.css`, `portal.css`, Bootstrap-era classes, and old Identity layout CSS.

## Recommended React Approach

Use React as an enhancement layer inside the existing MVC repo first. Do not go fully headless yet.

- Keep ASP.NET Core MVC/Razor for routing, auth, forms that already work, EF Core, payments, notifications, and admin/patient workflows.
- Add React only for isolated interactive surfaces: appointment booking, patient dashboard widgets, admin scheduling/calendar, payment UI, and notification preferences.
- Mount React into Razor pages with explicit root elements, for example `<div id="appointment-booking-root"></div>`.
- Expose backend functionality through intentional JSON endpoints instead of scraping Razor HTML.
- Keep Razor layout, SEO pages, privacy pages, and content-heavy pages server-rendered.

## Design System Direction

- Choose one primary styling model for the public site. Recommended: Tailwind plus a small local token/component layer.
- Keep `site.css` for global tokens, nav, forms, PWA/WhatsApp, and shared components.
- Keep `public-site.css` only for public marketing/homepage sections.
- Keep `portal.css` scoped to authenticated patient/admin portal surfaces.
- Avoid adding more one-off CSS files unless they are route-scoped and documented.
- Replace decorative effects with purposeful UI states: loading, disabled, active, offline, validation, empty, success, failure.

## Cleanup Targets Before Handoff

- Vendor or replace remaining CDN scripts needed for core interactions.
- Add a visual regression checklist for desktop and mobile header, homepage, appointment booking, teleconsultation, donation, bill payment, and patient portal.
- Remove or quarantine old Bootstrap/Identity layout CSS that is not used by the public layout.
- Add route-level CSS ownership notes so the next developer knows which file controls which page.
- Add React only after the design contract is agreed, otherwise the repo will have two frontends fighting each other.

## Meeting Recommendation

Agree on a hybrid model:

1. MVC/Razor remains the production shell and backend workflow owner.
2. React is introduced page-by-page for complex interactive UI.
3. Public pages get a cleaned, professional design system before large rewrites.
4. Each migrated surface needs tests, API contract notes, and a rollback path.
