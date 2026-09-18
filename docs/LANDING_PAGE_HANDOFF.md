# Landing page handoff

**Branch:** `new-landing-page`

The public landing page is built in React now. Everything you need to redesign it lives in `client/landing/`. You don't need to touch any C# backend code or worry about how the rest of the site works — just focus on making this page look and work great.

---

## Quick start

```bash
git clone <repo>
git checkout new-landing-page
npm install
npm run watch:landing
```

Then open `client/landing/` and start editing. The app rebuilds every time you save.

---

## Where to work

The page is built from the Figma file "Boniface - Paulina" (desktop frame `121:1970`, mobile frame `125:602`). Copy, spacing and colours match it exactly, so check the Figma before changing any of them.

```
client/landing/
├── main.jsx                # how React loads (you probably won't touch this)
├── App.jsx                 # the overall page structure
└── components/
    ├── Hero.jsx            # headline, buttons, palms, and the photo band
    ├── ServiceCards.jsx    # "Comprehensive Care for Your Whole Family" cards
    ├── WhyDifferent.jsx    # "What Makes Us Different?" photo stories
    ├── BookingChooser.jsx  # every "Book" action: in-person visit or video consultation
    └── CaretRightIcon.jsx  # the Figma caret used on buttons
```

The "Support a Patient" block, stats, and links at the bottom are the site footer, which is shared by every page. It lives in `Views/Shared/_Layout.cshtml`, not in React.

Photos are pre-cropped to the exact Figma framing in `wwwroot/images/landing/`, with separate `-mobile` crops for phones. Service icons are in `wwwroot/images/icons/services/`.

---

## The one thing to preserve

When you add new sections or change what's there, make sure React still gets the **data it needs from the backend**. Right now that's the four route links: appointment, teleconsultation, contact, and services.

Booking must always offer both an in-person visit and a teleconsultation. Use `BookingChooser` for any "Book" action instead of linking straight to one of them.

If your redesign needs a link to something new (like a new page or feature), just let Nikkayo know and they'll add it to the data payload.

---

## Styling

Every other public page uses the same design, not just the homepage:
- **Shell:** each page opens with the cream band (`site-page-header`), optionally followed by a full-width `page-photo`. The content sits in `page-section` / `page-section--cream` blocks on `.bp-wrap`.
- **Reused homepage pieces:** the service-card grid (`home-service-card`), the gold-divided story rows (`home-story`), and the booking chooser. On Razor pages the chooser is `Views/Shared/_BookingChooser.cshtml`, which works with no JavaScript.
- **Colours:** Tailwind's `primary-*` is the brand green and `secondary-*`/`bone-*` are the creams, so utility classes in views follow the design automatically. Corners are square site-wide.

Homepage styles are at the end of `wwwroot/css/public-site.css` (`.home-*` classes). The design tokens (`--bp-green`, `--bp-cream`, etc.) and the header/footer styles are in `wwwroot/css/site.css`. Sizes are plain pixels taken from the Figma; phones switch to the mobile frame below 720px, and the header switches to the menu button below 1024px.

---

## Building & committing

Every time you finish a chunk of work:

```bash
npm run build:landing
git add client/landing/
git commit -m "your message here"
git push origin new-landing-page
```

The bundle (`wwwroot/js/landing.js`) rebuilds automatically and needs to be committed too, same as CSS files are.

---

## Earlier versions

The page used to have a rotating photo carousel, a "care routes" grid, and mission, overview, partner and contact sections. The Figma design replaced all of them. To see how the old design looked, check git history:
```bash
git show 8b6fbaf:Views/Home/Index.cshtml
```

---

## If something breaks

- `npm run build:landing` fails? Delete `node_modules`, run `npm install`, try again.
- App won't start? Make sure you're on `new-landing-page` branch and you ran `npm install`.
- A link doesn't work? That's probably something that needs to be added to the data — ask Nikkayo.

Otherwise, you're on your own with the React — but it's just rendering HTML from the data the backend sends. Nothing fancy.

---

## CSS and JS ownership outside the landing page

The rest of the public layout (header, nav, footer) is still plain Razor/CSS, not React. If your redesign touches shared chrome, here's who owns what:

- Markup: `Views/Shared/_Layout.cshtml`
- Header/menu behavior: `wwwroot/js/navigation.js`
- Header/menu visual rules: `wwwroot/css/site.css`
- Utility classes and layout primitives: `wwwroot/css/tailwind.css` (generated — edit `wwwroot/css/tailwind.input.css` and run `npm run build:css`)
- Public homepage-specific styles: `wwwroot/css/public-site.css`
- Authenticated patient/admin portal styles: `wwwroot/css/portal.css` (don't touch for landing-page work)

The navbar doesn't depend on a CDN script. Alpine.js is used only by the Admin > Doctor Availability page (`Areas/Admin/Views/Availability/Index.cshtml`), loaded from a locally vendored copy (`wwwroot/lib/alpinejs`), not a CDN. The SignalR client is also vendored locally (`wwwroot/lib/signalr`) and referenced same-origin on the public, admin, and patient layouts. The CSP `script-src` only allows same-origin scripts plus a per-request nonce — no CDN origin is permitted.

**Why styles can look broken:**

- If `npm run build:css` hasn't been run after class changes, new Tailwind utility classes may not exist in the generated `tailwind.css` yet.
- If `tailwind.css`, `site.css`, or `public-site.css` fail to load, the page can look unstyled and the mobile nav can appear open.
- Google Fonts is still loaded from `fonts.googleapis.com`/`fonts.gstatic.com` and has not been vendored locally, so slow connections can delay font loading and shift layout.
- There is style overlap between Tailwind utilities, `site.css`, `public-site.css`, `portal.css`, and older Bootstrap/Identity layout CSS used by admin/patient screens — keep new public-site styles inside `public-site.css` rather than adding one-off files.

---

## Questions?

Ask Nikkayo. He set this up and designed the backend integration inside out.
