# CSS to Tailwind Migration Prompt for Copilot

You are helping refactor a healthcare application (Boniface and Paulina Okafor Memorial Hospital) from mixed CSS/Bootstrap styling to 90% Tailwind CSS with clean, modern UI components.

## Project Context
- **Framework**: ASP.NET Core 10.0 with Razor Views
- **Current Stack**: Tailwind CSS v4 (compiled from `wwwroot/css/tailwind.input.css`), Alpine.js, custom site.css, Bootstrap in Admin/Patient portal areas
- **Target**: Migrate site.css patterns to Tailwind utilities; modernize UI components
- **Font Family**: Source Serif 4 (headings), Inter (body)
- **Color Palette**: Bone/warm earth tones (#f4efe5, #c0e3d6, #1b3d2d, #1c1c1c)

## Priority Client Views (High Impact)
1. **Home/Index.cshtml** - Landing page, hero section
2. **Home/Doctors.cshtml** - Doctor profiles and listings
3. **Home/Services.cshtml** - Department/service cards
4. **Home/News.cshtml & NewsDetail.cshtml** - Content pages
5. **Areas/Patient/Pages/** - Patient portal views
6. **Views/Shared/_Layout.cshtml** - Navigation, footer (global styling)
7. **Views/AppointmentRequests/** - Booking UI
8. **Views/Departments/** - Department listings
9. **Shared Components** - Alerts, modals, cards, badges

## Current Issues to Address
- site.css contains public interaction styles plus some legacy Bootstrap-era utility overrides
- Inconsistent spacing and sizing across views
- Multiple CSS files are still loaded across layouts (`tailwind.css`, `site.css`, and Bootstrap/portal CSS in Admin/Patient areas), causing duplication
- Manual hover/transition effects that Tailwind can handle
- Non-semantic class names like `.min-touch-target`, `.card`, `.btn-sm`

## Migration Rules
1. **Replace Bootstrap classes** with Tailwind equivalents:
   - `.card` -> `rounded-lg border border-slate-200 shadow-md hover:shadow-lg hover:border-sky-200 transition-all`
   - `.btn` -> `inline-flex items-center justify-center px-4 py-2 rounded-md font-medium transition-colors`
   - `.alert` -> `rounded-lg border px-4 py-3 text-sm`
   - `.table-responsive` -> Use Tailwind's table utilities with overflow-x

2. **Use Tailwind for styling** instead of custom CSS:
   - Spacing: `p-4`, `m-6`, `gap-4` instead of custom padding/margins
   - Colors: Tailwind's palette with custom theme variables in `wwwroot/css/tailwind.input.css`
   - Shadows: `shadow-sm`, `shadow-md`, `shadow-lg` instead of custom box-shadow
   - Transitions: `transition-all`, `hover:`, `focus:` instead of custom @keyframes (unless complex)

3. **Keep Custom CSS for**:
   - Global scroll behavior, font smoothing
   - Responsive typography scales (if needed)
   - Complex animations only (fade, slide, bounce)
   - Brand-specific design system tokens

4. **Component Naming**: Move from classes to semantic HTML + Tailwind:
   - Avoid `<div class="card">`; prefer `<article class="rounded-lg border border-slate-200 shadow-md p-6">`
   - Avoid `<button class="btn btn-primary">`; prefer `<button class="px-4 py-2 bg-teal-700 hover:bg-teal-800 text-white rounded-md font-medium transition-colors">`

## Focus Areas for UI Components

### Buttons
- Primary: `px-4 py-2 bg-teal-700 hover:bg-teal-800 text-white rounded-md font-medium transition-colors disabled:opacity-50`
- Secondary: `px-4 py-2 border border-teal-200 text-teal-700 hover:bg-teal-50 rounded-md font-medium transition-colors`
- Small: `px-3 py-1.5 text-sm`
- Loading state: `disabled opacity-75 cursor-not-allowed`

### Cards
- Medical/content cards: `rounded-lg border border-slate-200 bg-white shadow-sm hover:shadow-md p-6 transition-shadow`
- Profile cards: Add `overflow-hidden` for image containers
- Appointment cards: Include `border-l-4 border-teal-500` for visual hierarchy

### Navigation
- Navbar: `sticky top-0 bg-white border-b shadow-sm z-50`
- Breadcrumbs: `flex gap-2 text-sm` with separators
- Tabs: `flex gap-1 border-b` with `border-b-2 border-teal-600` for active

### Forms
- Inputs: `w-full px-3 py-2 border border-slate-300 rounded-md focus:ring-2 focus:ring-teal-500 focus:border-transparent`
- Labels: `block text-sm font-medium text-gray-700 mb-1`
- Validation: `text-red-600 text-sm mt-1` for errors
- Checkboxes/Radio: Use native styling + custom accent colors

### Alerts & Status
- Success: `rounded-lg border border-green-200 bg-green-50 text-green-800 p-4`
- Error: `rounded-lg border border-red-200 bg-red-50 text-red-800 p-4`
- Info: `rounded-lg border border-blue-200 bg-blue-50 text-blue-800 p-4`
- Add icons: `flex items-center gap-3` with `feather-icon` or similar

### Spacing & Layout
- Container: `mx-auto px-4 sm:px-6 lg:px-8 max-w-7xl`
- Section spacing: `py-12 sm:py-16 lg:py-20`
- Grid layouts: `grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6`
- Flexbox: Default to `flex flex-col sm:flex-row gap-4`

## Implementation Strategy

### Phase 1: Global & Layout (Week 1)
- Update `_Layout.cshtml` navbar/footer with Tailwind
- Create reusable component partials for buttons, alerts, cards
- Establish color/spacing scale in `wwwroot/css/tailwind.input.css`
- Remove unused Bootstrap CSS imports from public layouts only; keep Bootstrap in Admin/Patient until those areas are migrated

### Phase 2: Key Client Views (Weeks 2-3)
1. Home/Index.cshtml - Hero, stats, featured doctors
2. Home/Doctors.cshtml - Doctor cards grid
3. Home/Services.cshtml - Service/department cards
4. Home/News.cshtml - News grid with Tailwind
5. Shared navigation components

### Phase 3: Forms & Interactions (Week 4)
- AppointmentRequests views
- Patient portal forms
- Apply Alpine.js + Tailwind for modals/interactions

### Phase 4: Admin & Remaining Views
- Admin dashboard tables with Tailwind
- Remaining CRUD forms
- Data tables with responsive overflow

### Phase 5: Cleanup & Optimization
- Remove site.css entirely (or keep only essential global styles)
- Audit for unused Tailwind classes
- Add dark mode support (optional)
- Verify accessibility (focus states, contrast, ARIA)

## Code Examples to Replace

### Current site.css Pattern
```css
.card {
  border-radius: 0.5rem;
  border-color: #e2e8f0;
  box-shadow: 0 10px 28px rgba(15, 23, 42, 0.06);
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}
.card:hover {
  border-color: #d9f1f7;
  box-shadow: 0 18px 42px rgba(15, 23, 42, 0.09);
  transform: translateY(-3px);
}
```

### Tailwind Replacement
```html
<div class="rounded-lg border border-slate-200 bg-white shadow-md hover:shadow-lg hover:border-sky-100 hover:-translate-y-1 transition-all duration-200 p-6">
  <!-- content -->
</div>
```

### Form Validation
```html
<!-- Input -->
@{
    var fieldErrors = ViewData.ModelState[field]?.Errors.Any() == true;
    var inputClasses = $"w-full px-3 py-2 border border-slate-300 rounded-md focus:ring-2 focus:ring-teal-500 focus:border-transparent disabled:bg-slate-100 disabled:cursor-not-allowed{(fieldErrors ? " border-red-500" : "")}";
}
<input type="email" class="@inputClasses" />

<!-- Error Message -->
@if (ViewData.ModelState[field]?.Errors.Any() == true) {
  <span class="text-red-600 text-sm mt-1">@ViewData.ModelState[field].Errors.First().ErrorMessage</span>
}
```

## Tailwind Theme Additions (`wwwroot/css/tailwind.input.css`)

```css
@theme {
  --color-teal-50: #f0fdf4;
  --color-teal-700: #1b3d2d;
  --color-teal-800: #0f2419;
  --color-bone-100: #f4efe5;
  --color-bone-200: #e2ddd3;

  --font-serif: "Source Serif 4", Georgia, serif;
  --font-sans: Inter, system-ui, sans-serif;
}
```

## Repo-Specific Guardrails

- Public site: prioritize Tailwind and keep `site.css` for only global behavior, motion, and a small number of shared public helpers.
- Admin/Patient portals: Bootstrap can remain during migration; move Bootstrap compatibility rules into a portal-specific stylesheet instead of keeping them in the public site layer.
- Do not assume `tailwind.config.js` exists. This project currently uses Tailwind v4's CSS-first theme configuration.

## Acceptance Criteria
- [ ] `site.css` reduced by 70%+ (or eliminated)
- [ ] All primary client views use 90%+ Tailwind classes
- [ ] No visual regressions (compare screenshots pre/post)
- [ ] Consistent spacing and sizing across all views
- [ ] Responsive design maintained (mobile, tablet, desktop)
- [ ] Focus states and accessibility preserved
- [ ] Button, card, form, and navigation components standardized
- [ ] Performance: no increase in CSS file size

## Testing Checklist
- [ ] Home page desktop & mobile
- [ ] Doctors listing & profile view
- [ ] Appointment booking flow
- [ ] Patient portal views
- [ ] Admin dashboard
- [ ] All form validations display correctly
- [ ] Hover/focus states visible and accessible
- [ ] No layout shifts during transitions
- [ ] Print styles work correctly (if needed)

---

**When working on a view**, ask Copilot:
> "Refactor [ViewName].cshtml to use 90% Tailwind CSS. Replace custom classes with Tailwind utilities, ensure responsive design (mobile-first), maintain the current visual hierarchy, and follow the color palette (teal-700, bone-100, slate-200). Show before/after if making large changes."
