# Landing page handoff

Branch: `new-landing-page` (off `master`). The public landing page
(`Views/Home/Index.cshtml`) is built in React now -- see
[`docs/decisions/0006-react-landing-page-non-headless.md`](decisions/0006-react-landing-page-non-headless.md)
for why, and [`docs/decisions/0002-keep-aspnet-backend-separate-from-public-design-work.md`](decisions/0002-keep-aspnet-backend-separate-from-public-design-work.md)
for the original backend/design boundary this still follows. Nothing else in the
site changed -- shared header/nav/footer, every other page, and all C# backend
logic are untouched.

> Earlier revision of this doc described a bare-skeleton, framework-agnostic
> handoff (`<div id="landing-app">` + an empty `wwwroot/js/landing-app.js` stub).
> That approach is superseded now that React has been chosen explicitly -- this
> revision documents the real, working setup.

## Where your code goes

Everything you touch lives under `client/landing/`:

```
client/landing/
├── main.jsx              # reads the server data payload, mounts <App>
├── App.jsx                # top-level layout: renders each section in order
└── components/
    ├── Hero.jsx            # hero copy + carousel (autoplay, swipe, indicators)
    ├── CareRoutes.jsx       # "choose your care path" card grid
    ├── Mission.jsx          # care-philosophy section + trust points
    ├── Services.jsx         # featured departments
    ├── Overview.jsx         # "inside B&P Hospital" section
    ├── Partner.jsx           # Nigeria Family Helper Program section
    └── Contact.jsx            # visit/contact details + map
```

Redesign freely inside these files -- new components, different structure, a
different visual system entirely. The two things to preserve are the **data
contract** and the **mount point**, both below.

### Build it

```bash
npm install
npm run build:landing     # one-off build -> wwwroot/js/landing.js
npm run watch:landing     # rebuilds on every save while you work
```

`wwwroot/js/landing.js` is committed, the same way `wwwroot/css/tailwind.css`
always has been -- there's no build step in CI or at deploy time. Rebuild and
commit the output whenever `client/landing/` changes.

### The mount point

`Views/Home/Index.cshtml` renders one element:

```cshtml
<div id="landing-root" class="public-home hospital-home"></div>
```

and, at the bottom of the file:

```cshtml
@section Scripts {
    <script nonce="@(Context.Items["CspNonce"])" id="landing-data" type="application/json">@Html.Raw(landingDataJson)</script>
    <script nonce="@(Context.Items["CspNonce"])" type="module" src="~/js/landing.js" asp-append-version="true"></script>
}
```

`main.jsx` reads the JSON payload out of `#landing-data` and calls
`createRoot(document.getElementById("landing-root")).render(<App data={data} />)`.
You generally shouldn't need to touch either script tag.

### The data contract

`Index.cshtml` computes hospital config, department data, feature flags, and
`Url.Action` routes server-side and serializes them into one object
(`landingData` in the `.cshtml`, passed to `<App>` as the `data` prop). React has
no way to call back into Razor's `Url.Action`/`asp-controller` helpers -- if your
redesign needs a link to a controller action that isn't already in the payload,
add it in `Index.cshtml`'s `landingData` object, not in JSX.

Current shape (see `Index.cshtml` for the exact C# that builds it):

```jsonc
{
  "hero": { "hospitalName": "...", "lead": "...", "slides": [{ "image", "mobileImage", "width", "height", "alt", "caption" }] },
  "careDock": { "emergencyNumbers": "..." },
  "urls": { "appointmentCreate", "teleconsultationCreate", "contact", "services", "patientInfo", "gallery" },
  "careRoutes": [{ "label", "title", "body", "href", "external" }],
  "portraitImage": "...",
  "signImage": "...",
  "mission": { "trustPoints": ["..."] },
  "services": { "items": [{ "name", "excerpt" }] },
  "contact": { "hospitalName", "hospitalAddress", "emergencyNumbers", "hospitalEmail", "mapUrl" }
}
```

Server data notes:

- Featured department **descriptions** are real DB content, truncated server-side
  to 140 chars (`Excerpt()` in `Index.cshtml`) -- don't re-truncate in JS.
- `careRoutes`/`urls` already resolve to final `href`s (internal routes via
  `Url.Action`, the donate link as an external URL with `external: true`). The
  bill-payments care route is already filtered out server-side when that launch
  feature is off.
- Optional fields (`hospitalAddress`, `emergencyNumbers`, `hospitalEmail`,
  `mapUrl`) can be `null` -- the existing components render a fallback message
  when they are; keep that pattern for any new optional field you add.

## Starting point vs. a blank page

Unlike a bare skeleton, `client/landing/` currently renders the **previous
design's real copy and structure** (hero carousel, care routes, mission,
services, overview, partner, contact) ported into JSX with the same CSS classes
(`wwwroot/css/public-site.css`) the old Razor markup used. Redesign is expected
to replace this -- treat it as a working reference for content/behavior, not as
markup to preserve. The pre-React design, copy, and hero carousel implementation
are also fully available in git history: `git show 8b6fbaf:Views/Home/Index.cshtml`
(the last commit before this handoff) or `git log 8b6fbaf -- Views/Home/Index.cshtml`.

## CSP note

The layout serves a strict Content-Security-Policy with a per-request nonce
(`Context.Items["CspNonce"]`). Any `<script>` or `<style>` tag you add directly
in a `.cshtml` file needs that same `nonce="..."` attribute or the browser will
block it silently. This doesn't affect React itself (rendering DOM nodes,
event handlers, etc.) -- it only matters for tags written by hand into Razor.

## Running the site locally

`dotnet run --launch-profile demo` runs against the seeded in-memory demo
database -- no external DB setup needed to see the homepage render. See
[`README.md`](../README.md) and [`docs/LOCAL_LINUX_SETUP.md`](LOCAL_LINUX_SETUP.md) /
[`docs/LOCAL_WINDOWS_SETUP.md`](LOCAL_WINDOWS_SETUP.md) for full setup.

## Scope

Only the homepage (`Views/Home/Index.cshtml` + `client/landing/`) is React. The
shared site header/nav/footer (`Views/Shared/_Layout.cshtml`) and every other
page are unchanged Razor, so the landing page still renders inside the existing
site chrome. If the new branding needs to change the header/nav/footer too,
that's a separate, deliberate follow-up -- flag it before touching `_Layout.cshtml`
since it wraps every page on the site.
