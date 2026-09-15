# QA Checklist

Manual browser checks that cannot be safely automated. Run these after any change
to the PWA install flow, service worker, or offline appointment behavior.

## Manual Checklist: PWA Install Prompt

Requires a deployed/staging URL over HTTPS (install prompts do not fire on `http://` origins except `localhost`). Run this after every change to `wwwroot/js/pwa-register.js`, `wwwroot/service-worker.js`, or `wwwroot/site.webmanifest`.

**Desktop Chrome/Edge (fires `beforeinstallprompt` — most reliable target):**
1. Open the site in a fresh profile (or clear site data first) so the install prompt hasn't already been dismissed this session.
2. Confirm a `[data-pwa-install]` button appears near the footer within a few seconds of page load.
3. Click it once: the browser's native install dialog should appear immediately (button click must call `installPrompt.prompt()` synchronously — if the dialog doesn't appear, check the console for a "user gesture" rejection).
4. Accept the install. Confirm: the app opens in its own window, the `[data-pwa-install]` button is removed from the original page (via the `appinstalled` listener), and the OS shows an installed app icon.
5. Reload the original tab. Confirm the install button does not reappear (browser suppresses `beforeinstallprompt` once installed).
6. Uninstall the PWA and repeat once, this time dismissing the native dialog instead of accepting: confirm the button becomes clickable again (re-enabled, not stuck disabled) — this covers the `installPrompt` nulled/re-enable guard in `pwa-register.js`.

**Android Chrome:** same flow as desktop; additionally confirm the install button does not visually collide with the floating WhatsApp button at 360px/390px/430px widths.

**iOS Safari (no `beforeinstallprompt` support — this is expected, not a bug):** confirm the `[data-pwa-install]` button correctly never appears (Safari never fires the event, so `showInstallButton()` never runs). Manually verify "Add to Home Screen" from the Safari share sheet still installs a working icon/splash screen using `site.webmanifest`.

**Failed registration path:** with dev tools open, block `/service-worker.js` (Network tab → block request URL) and reload. Confirm the page still loads normally (registration failure is caught and swallowed, per `pwa-register.js`'s `.catch(function () {})` — verified structurally by `PWARegistrationTests.ServiceWorkerRegistration_IsDeferredToLoad_AndFailureIsHandled`, but the resulting page behavior still needs a human check).

## Manual Checklist: Offline Appointment Sync

1. With a normal network connection, sign in as a patient with at least one upcoming appointment and open `/Portal/Appointments` so the current appointment list gets cached by `pwa-appointments.js`.
2. Enable airplane mode / DevTools "Offline" throttling, then open `/offline-appointments.html` directly.
3. Confirm the previously-viewed appointment summary renders from the local encrypted store, not a blank/error state.
4. With no appointments ever viewed (fresh browser profile, still offline), open `/offline-appointments.html` and confirm the empty state renders: the `aria-live="polite"` / `role="status"` banner from `wwwroot/offline-appointments.html` (`data-offline-appointments-empty`), not a broken page.
5. Restore network connectivity and confirm a normal `/Portal/Appointments` load still works and reflects live server data (i.e., the offline cache is a fallback, not a stale source of truth once the network returns).
6. Confirm private/authenticated routes (`/Portal/*`, `/Admin/*`) never get served from the service worker's cache while offline with no prior visit — they should show the "Connection required" fallback from `handleNetworkOnly()` in `service-worker.js`, not a cached page or stale patient data.
