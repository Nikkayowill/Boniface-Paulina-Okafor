# 0006: React for the Landing Page, Without Going Headless

Date: 2026-09-01

## Status

Accepted

## Context

`a451f4f` ("Strip the homepage to a bare skeleton for a design handoff") stripped
`Views/Home/Index.cshtml` to plain unstyled markup with an empty `wwwroot/js/landing-app.js`
stub, deliberately leaving the framework choice open: "bring your own build (Vite, CRA,
esbuild, whatever) ... or use React via CDN `<script>` tags." Since then, the framework
decision has been made explicitly: the landing page will be built in React, and the design
collaborator should get a working scaffold rather than an empty stub.

## Decision

- `client/landing/` holds the React source (`main.jsx`, `App.jsx`, `components/*.jsx`),
  built by Vite into a single committed `wwwroot/js/landing.js` -- the same
  "compile source, commit output" pattern already used for Tailwind
  (`wwwroot/css/tailwind.input.css` -> `wwwroot/css/tailwind.css`). This **supersedes**
  the empty `wwwroot/js/landing-app.js` stub from `a451f4f` (now removed) and its
  "bring your own bundler" framing.
- `Views/Home/Index.cshtml` stays a real Razor view: it still computes hospital
  config, department data, feature flags, and `Url.Action` routes server-side, but
  instead of rendering markup it serializes that into one JSON payload
  (`<script type="application/json" id="landing-data">`, nonce'd like the existing
  JSON-LD block) and renders a single `<div id="landing-root">`. `main.jsx` reads
  that payload and mounts `<App>` into it.
- `Views/Shared/_Layout.cshtml` is untouched: header, top utility bar, footer, SEO
  meta tags, JSON-LD, and the WhatsApp float stay server-rendered Razor. React owns
  only what was previously `Views/Home/Index.cshtml`'s body content.
- ASP.NET Core MVC remains the only backend and the only thing deployed. React ships
  as a static asset the existing app serves same-origin, under the existing CSP
  (`script-src 'self' 'nonce-...'`) -- no new origin, no new deploy target.
- Full working-baseline content (the previous design's copy, hero carousel, and
  section structure, ported into JSX) was restored rather than left as "Add ..."
  placeholders, so the design collaborator has something real to redesign visually
  instead of an empty page. See [`docs/LANDING_PAGE_HANDOFF.md`](../LANDING_PAGE_HANDOFF.md)
  for the file-by-file contract.

## Consequences

- The design collaborator can rebuild the landing page's visual design freely inside
  `client/landing/` without touching C#, and without the app becoming two services.
- Every route, feature flag, and piece of hospital contact data the landing page
  needs must be added to the `landingData` payload in `Index.cshtml` -- React has no
  way to call back into Razor's `Url.Action`/`asp-controller` helpers, so a new
  section that links to a new controller action needs its URL added there.
- **Cost, measured on this branch:** the built bundle is ~201KB / ~62.8KB gzipped
  (`react` + `react-dom` + the app), added to the homepage only. That is larger than
  every other CSS and JS file on the site combined, and works directly against the
  mobile load-path budget from `perf/mobile-load-path` (320KB -> 39KB a page,
  2026-08-19). Anyone extending `client/landing/` should keep this in view -- e.g.
  avoid pulling in additional npm UI libraries without checking their gzip cost.
- The hero carousel's behavior (autoplay, pause on hover/focus/reduced-motion/hidden
  tab, touch swipe, indicator-only controls) was ported from the pre-`a451f4f`
  `wwwroot/js/hero-carousel.js` into `client/landing/components/Hero.jsx`; that file
  had no other caller and is now removed. Source-level tests that used to check it
  now check the JSX component instead (`tests/Okafor.NET.Tests/ResponsiveDesignTests.cs`).
- No server-side rendering: the landing page's content is empty in the initial HTML
  response until `landing.js` executes. This is an intentional tradeoff for this
  decision, not an oversight -- if it becomes a problem (SEO, no-JS users, very slow
  connections), the fix is adding SSR/hydration (e.g. via a Node-based prerender step
  at build time), not reverting to Razor markup.
