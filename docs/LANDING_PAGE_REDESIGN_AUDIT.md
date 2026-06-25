# Landing Page Redesign Audit

Date: 2026-06-24

## Goal

Redesign the public landing page for Boniface and Paulina Okafor Memorial Hospital. Nothing more, nothing less.

This work should stay focused on the homepage experience at `Views/Home/Index.cshtml` and its scoped stylesheet `wwwroot/css/public-site.css`. The redesign should not change backend workflows, admin screens, patient portal screens, identity pages, payment flows, appointment submission behavior, teleconsultation submission behavior, or PWA/service-worker behavior.

## Project Context For ChatGPT Browser

The project is an ASP.NET Core MVC hospital management application. It has:

- Public-facing pages for home, about, services, doctors/team, patient information, news, contact, donations, appointments, teleconsultations, and bill payments.
- Admin workflows for appointments, teleconsultations, posts, users, patients, bill payments, contact submissions, and availability.
- Patient portal workflows for dashboard, profile, documents, appointments, messages, and teleconsultations.
- PWA/offline assets and service worker tests.

Technology notes:

- Framework: ASP.NET Core MVC on .NET 10.
- Views: Razor `.cshtml`.
- Main public layout: `Views/Shared/_Layout.cshtml`.
- Landing page view: `Views/Home/Index.cshtml`.
- Landing page styles: `wwwroot/css/public-site.css`, scoped under `.public-home` and `.hospital-home`.
- Global/public nav styles: `wwwroot/css/site.css`.
- Tailwind compiled stylesheet: `wwwroot/css/tailwind.css`.
- Public homepage data model: `ViewModels/PublicWebsiteViewModels.cs`.
- Homepage controller action: `Controllers/HomeController.cs`.

The homepage currently renders successfully in `Testing` environment. A local HTTP check returned `HTTP/1.1 200 OK` for `/`.

## Current Landing Page Structure

`Views/Home/Index.cshtml` currently includes:

- Hero with full-bleed hospital image and two CTAs.
- Care shortcut dock for emergency line, services, and patient information.
- Intro statement.
- Four care-route links: appointments, teleconsultation, bills, and donations.
- Care philosophy section with image and trust bullet list.
- Featured services list from `Model.FeaturedDepartments`.
- Health education topic links.
- Four-image hospital gallery using fixed image paths.
- Latest posts/news section from `Model.LatestPosts`.
- Contact/location section with optional Google map.

`Controllers/HomeController.cs` already loads:

- `FeaturedDepartments`
- `FeaturedDoctors`
- `LatestPosts`
- `FeaturedPosts`
- `ViewBag.RandomImages`

The current homepage uses featured departments and latest posts, but it does not use featured doctors, featured posts, or randomized hospital images. Those unused data sources are good redesign material because they are already available.

## Audit Findings

The current design is functional and not broken, but it feels more like a styled content page than a polished hospital front door.

Main opportunities:

- The hero has the right idea, but the oversized hospital name and dark overlay dominate the first viewport. The page could communicate trust, location, emergency access, and appointment intent faster.
- The page repeats similar CTA language across hero, care routes, and contact sections. It needs a clearer priority ladder.
- The homepage is long and linear. It would feel more professional with stronger section contrast, tighter grouping, and fewer repetitive headings.
- Several sections use large editorial typography that can feel dramatic instead of practical for a hospital management system.
- Real hospital images exist, but the page uses fixed images and does not curate a visual story. The gallery is present, but the imagery is not used strategically to build confidence.
- Available doctors and featured posts are loaded but not used. The redesign can add human/service proof without new backend work.
- The current palette leans heavily into teal/dark teal. It is clean, but risks becoming one-note across a full page.
- Mobile styles exist, but the hero, dock, and full-width buttons need special attention so text does not crowd or feel stacked in a rushed way.
- The stylesheet is properly scoped, which is good. The redesign should preserve that boundary.

## Redesign Direction

Aim for a professional community-hospital landing page:

- Calm, trustworthy, and practical.
- Clear first-viewport paths: request appointment, contact/emergency, services, patient information.
- Use real hospital imagery with intentional cropping and alt text.
- Keep copy concise and patient-centered.
- Make the page feel like a working hospital system, not a generic charity or blog.
- Preserve backend routes and existing controller/view model data unless a small data change is clearly necessary.

Recommended section order:

1. Hero: hospital identity, location, primary CTA, secondary CTA, compact contact/emergency signal.
2. Care access strip: appointment, teleconsultation, services, patient info, bill payment/donation as secondary.
3. Services snapshot: featured departments with concise descriptions and link to services.
4. Doctors/team or care team proof: use `Model.FeaturedDoctors` if appropriate.
5. Patient guidance: prevention/education and what to bring/expect.
6. Hospital story/trust: memorial mission, community care, real imagery.
7. News/outreach: latest or featured posts.
8. Contact/location: address, email, emergency numbers, map fallback.

## Issue-Ready Cards

### Issue 1: Redesign the landing-page hero

Scope:

- Update `Views/Home/Index.cshtml`.
- Update `wwwroot/css/public-site.css`.
- Keep existing routes and CTA destinations.

Problem:

The current hero is functional but feels heavy and oversized. It needs a more professional first impression that quickly communicates the hospital name, location, care access, and next action.

Acceptance criteria:

- First viewport clearly shows the hospital identity and location.
- Primary CTA is appointment request.
- Secondary CTA is teleconsultation or contact, but not visually equal to every other action.
- Emergency/contact signal is visible without overwhelming the hero.
- Real hospital image remains visible and intentionally cropped.
- Mobile hero text and buttons do not overlap, overflow, or feel cramped.
- No changes to appointment or teleconsultation form behavior.

### Issue 2: Rework homepage information architecture

Scope:

- Update homepage section order and markup in `Views/Home/Index.cshtml`.
- Keep the redesign limited to the landing page.

Problem:

The current homepage has many good sections, but the sequence feels long and repetitive. Visitors should quickly understand where to go for care, services, education, payments, donations, and contact.

Acceptance criteria:

- Define a cleaner landing-page sequence with fewer repeated CTAs.
- Group related actions together.
- Keep appointment, teleconsultation, services, patient info, bill payment, donation, news, and contact routes available.
- Remove or rewrite duplicated copy.
- Keep semantic headings in a logical order.
- Do not remove functional routes from the site navigation or footer.

### Issue 3: Create a professional landing-page visual system

Scope:

- Update `wwwroot/css/public-site.css`.
- Keep styles scoped under `.public-home` / `.hospital-home`.

Problem:

The current visual style is clean but too dependent on dark teal, large editorial headings, and repeated card/list treatments. The page needs a more mature hospital design system.

Acceptance criteria:

- Establish a balanced palette with teal as a brand color, not the only dominant color.
- Normalize heading sizes so compact sections do not feel oversized.
- Use consistent spacing, grid behavior, button states, and section contrast.
- Keep border radius at 8px or less unless matching existing design tokens.
- Preserve visible focus states and reduced-motion handling.
- No style leakage into admin, patient portal, identity, payment, or form pages.

### Issue 4: Use existing data for trust and service proof

Scope:

- Update `Views/Home/Index.cshtml`.
- Use data already provided by `PublicHomeIndexViewModel` where possible.

Problem:

The controller loads featured doctors and featured posts, but the current homepage does not use them. The redesign can feel more credible by showing service and care-team proof without adding a new backend feature.

Acceptance criteria:

- Use `Model.FeaturedDepartments` for a concise services snapshot.
- Consider adding a care-team preview using `Model.FeaturedDoctors`.
- Consider using `Model.FeaturedPosts` or `Model.LatestPosts` for outreach/news.
- Include graceful empty states.
- Do not add new database tables or admin workflows for this redesign.

### Issue 5: Curate homepage imagery

Scope:

- Audit available files under `wwwroot/images/placeholders/Hospital`.
- Update image choices in `Views/Home/Index.cshtml`.
- Update CSS cropping rules in `wwwroot/css/public-site.css`.

Problem:

The current page uses real hospital images, but the visual story could be stronger. The images should support trust, place, care, and community instead of feeling like a generic gallery.

Acceptance criteria:

- Select images for hero, service/trust, team/community, and contact/location sections.
- Use accurate, useful alt text.
- Avoid dark, overly blurred, or uninspectable image treatment.
- Set stable dimensions/aspect ratios to prevent layout shift.
- Ensure images render well on mobile and desktop.

### Issue 6: Improve mobile landing-page polish

Scope:

- Update responsive rules in `wwwroot/css/public-site.css`.
- Review `Views/Home/Index.cshtml` only if markup changes are needed for mobile.

Problem:

The mobile page needs special attention because hospital visitors may be on phones with slow connections. The hero, CTA buttons, care dock, gallery, and contact details should be easy to scan and tap.

Acceptance criteria:

- No text overlap at common mobile widths.
- Buttons wrap cleanly and remain tappable.
- Care shortcuts are easy to scan.
- Large headings do not dominate compact sections.
- Contact details and map fallback remain readable.
- Visual rhythm remains professional without excessive vertical bloat.

### Issue 7: Accessibility and verification pass for the redesigned landing page

Scope:

- Landing page only.
- Tests/checks may touch existing smoke or responsive tests if needed.

Problem:

After redesign, the landing page should be checked for basic accessibility, route safety, and visual regressions.

Acceptance criteria:

- Homepage loads successfully in `Testing` mode.
- Heading order is logical.
- Links and buttons have visible focus states.
- Images have appropriate alt text.
- Color contrast is acceptable for text and CTAs.
- Reduced-motion behavior still exists.
- Desktop and mobile manual checks are recorded in the PR or issue.
- Existing backend workflows are not changed.

## Guardrails For The Teammate

- Do not redesign admin, patient portal, identity pages, appointment forms, teleconsultation forms, payment pages, service worker, or backend workflows.
- Do not introduce React for this task.
- Do not add new external CDN dependencies.
- Do not replace the site-wide navigation unless the landing page exposes a specific visual bug.
- Keep homepage-specific CSS in `wwwroot/css/public-site.css`.
- Preserve routes generated by ASP.NET tag helpers.
- Prefer existing data and assets before adding new content structures.

## Suggested ChatGPT Prompt

Use this prompt in browser ChatGPT if you want help creating issue tracker cards:

```text
I am working on an ASP.NET Core MVC hospital management system for Boniface and Paulina Okafor Memorial Hospital in Abia State, Nigeria. Our current task is only to redesign the public landing page, nothing more.

Relevant files:
- Views/Home/Index.cshtml
- wwwroot/css/public-site.css
- Views/Shared/_Layout.cshtml
- Controllers/HomeController.cs
- ViewModels/PublicWebsiteViewModels.cs

The homepage currently has a hero, care shortcut dock, intro, care routes, care philosophy, services, health education, image gallery, news, and contact/map section. The controller already loads FeaturedDepartments, FeaturedDoctors, LatestPosts, FeaturedPosts, and RandomImages. The current homepage uses departments and latest posts, but does not use featured doctors, featured posts, or random images.

Please create clear issue tracker cards for a professional landing-page redesign. Each issue should include title, problem, scope, acceptance criteria, files likely touched, and guardrails. Guardrails: do not change backend workflows, admin, patient portal, identity pages, payment flows, appointment/teleconsultation submission behavior, PWA/service worker, or site-wide navigation unless absolutely necessary for the landing page.
```
