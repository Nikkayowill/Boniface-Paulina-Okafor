# Landing page handoff

Branch: `design/landing-page-handoff` (off `master`). This branch strips the
homepage down to a bare skeleton — no design, no copy — so a new design and
brand can be built without fighting the old one. Nothing else in the site
was touched.

## What changed

`Views/Home/Index.cshtml` — all visual markup **and** all marketing copy
removed:

- No CSS classes (the old design lived in `wwwroot/css/public-site.css` under
  `.hospital-*` / `.site-button*` — that file is untouched, just no longer
  referenced from this view).
- No hero carousel, no photos (`wwwroot/js/hero-carousel.js` and the images
  under `wwwroot/images/placeholders/Hospital/` are unused by this page now,
  but left in place — nothing else references the carousel script).
- No map embed — the map is now a plain "View map" link instead of an
  `<iframe>`.
- No headlines, taglines, or body paragraphs. Every spot that held written
  copy is now a literal "Add your headline here" / "Add a section heading" /
  "Add supporting copy here" placeholder — write real copy directly over
  these, don't design around them.

What's still there, and why it's data/behaviour rather than copy to rewrite:

- Server-rendered dynamic values: hospital name/address/email/emergency
  numbers/map URL (from config), featured department **names** (from the DB
  via `Model.FeaturedDepartments` — department descriptions were dropped,
  they were marketing text), and a bill-payments feature flag that hides
  that care route when the feature isn't launched yet.
- The working links (`asp-controller`/`asp-action` tag helpers) and their
  short button labels ("Request appointment", "View all services", "Contact
  us", etc.) to real routes: appointment requests, teleconsultations, bill
  payments, services, contact, gallery, and the external donate/partner
  links.

The **previous design and copy are fully preserved in git history** — `git
show master:Views/Home/Index.cshtml` (or `git log master --
Views/Home/Index.cshtml`) gets you the old markup, headlines, body copy, and
hero carousel if you want to reference layout, copy structure, or animation
timing.

## Where your code goes

`<div id="landing-app">…</div>` in `Views/Home/Index.cshtml` wraps the plain
content. At the bottom of that file:

```cshtml
@section Scripts {
    <script nonce="@(Context.Items["CspNonce"])" src="~/js/landing-app.js" asp-append-version="true" defer></script>
}
```

`wwwroot/js/landing-app.js` is an empty stub already wired into the page —
drop your compiled bundle's output there (or repoint the `src`) and mount
over `#landing-app`.

No React tooling has been added to this repo (no `package.json` deps beyond
Tailwind's CLI, no bundler). This is intentional — bring your own build
(Vite, CRA, esbuild, whatever) and commit the built output into
`wwwroot/js/`, or use React via CDN `<script>` tags if you'd rather skip a
build step entirely.

**CSP note:** the layout serves a strict Content-Security-Policy with a
per-request nonce (`Context.Items["CspNonce"]`). Any `<script>` or `<style>`
tag you add directly in `Index.cshtml` needs that same `nonce="..."`
attribute or the browser will block it silently. Your bundle's own runtime
behavior (React rendering DOM nodes, etc.) isn't affected — this only matters
for tags you write into the `.cshtml` by hand.

## Running the site locally

`dotnet run --launch-profile demo` runs against the seeded in-memory demo
database — no external DB setup needed to see the homepage render.

## Scope

Only the homepage (`Views/Home/Index.cshtml`) was stripped. The shared site
header/nav/footer (`Views/Shared/_Layout.cshtml`) and every other page are
unchanged, so this page still renders inside the existing site chrome. If the
new branding needs to change the header/nav/footer too, that's a separate,
deliberate follow-up — flag it before touching `_Layout.cshtml` since it
wraps every page on the site.
