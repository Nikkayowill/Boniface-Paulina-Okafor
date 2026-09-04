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

```
client/landing/
├── main.jsx              # how React loads (you probably won't touch this)
├── App.jsx               # the overall page structure
└── components/
    ├── Hero.jsx          # top section with carousel + buttons
    ├── CareRoutes.jsx    # "choose your care path" cards
    ├── Mission.jsx       # "care should start with less confusion" section
    ├── Services.jsx      # featured departments list
    ├── Overview.jsx      # "inside B&P Hospital" section
    ├── Partner.jsx       # Nigeria Family Helper Program callout
    └── Contact.jsx       # visit/contact details + map at the bottom
```

**Edit these files freely.** Change the HTML, the styling, the layout, the whole structure — redesign it however you want. The CSS still uses the existing `wwwroot/css/public-site.css`, so all those classes (`.hospital-hero`, `.site-button`, etc.) are still there if you want them, or you can add new styles.

---

## The one thing to preserve

When you add new sections or change what's there, make sure React still gets the **data it needs from the backend**. Right now that's hospital name, emergency numbers, department list, featured images, and links to real routes like "Request appointment".

If your redesign needs a link to something new (like a new page or feature), just let Nikkayo know and they'll add it to the data payload.

---

## Styling

The page uses Tailwind CSS (like the rest of the site). Add classes as needed. If you want to add new custom CSS, it goes in `wwwroot/css/public-site.css` under a new section with a comment explaining what it's for.

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

## The real copy & structure are in here now

Unlike starting from a blank page, there's already working copy and structure (hero carousel with auto-rotate and swipe, care routes grid, mission section, services list, etc.). Treat that as a starting point — something real to redesign from, not something you have to preserve. 

If you want to see how the old design looked, you can check git history:
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
