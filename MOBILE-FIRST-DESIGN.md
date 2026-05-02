# Mobile-First Design Guide for Copilot

**THIS IS YOUR PRIMARY DESIGN CONSTRAINT.** All new UI development and refactoring must start mobile-first.

## Core Principle
**Mobile First = Build for small screens first, then enhance for larger screens.**

Not: Desktop → shrink for mobile  
**YES**: Mobile → expand for desktop ✅

---

## Why Mobile-First for Okafor Hospital

1. **Accessibility** - Many Nigerians access via mobile 🇳🇬
2. **Patient Experience** - Clinic visitors use phones to book appointments
3. **Performance** - Mobile constrains bloat; forces efficiency
4. **Conversion** - 60%+ of healthcare searches happen on mobile
5. **Legal** - Hospital site should be accessible to all devices

---

## Mobile Viewport Requirements

```html
<!-- MUST BE in _Layout.cshtml <head> -->
<meta name="viewport" content="width=device-width, initial-scale=1.0, viewport-fit=cover" />
```

**Device Targets:**
- **320px** - Old phones (legacy)
- **375px** - iPhone SE, Galaxy A
- **428px** - iPhone 12/13 Pro Max
- **768px** - Tablets (iPad mini)
- **1024px** - iPad/small laptops
- **1280px+** - Desktop/large screens

---

## Tailwind Mobile-First Breakpoints

Tailwind is **mobile-first by default**. This is the correct pattern:

```html
<!-- ❌ WRONG - Desktop first -->
<div class="grid grid-cols-3 sm:grid-cols-1">
  Desktop 3 columns, mobile 1 column = backwards!
</div>

<!-- ✅ CORRECT - Mobile first -->
<div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4">
  Mobile: 1 column
  Tablet (640px): 2 columns
  Medium (768px): 3 columns
  Desktop (1024px): 4 columns
</div>
```

### Breakpoint Reference
| Prefix | Min Width | Device |
|--------|-----------|--------|
| (none) | 0px       | Mobile |
| sm     | 640px     | Tablet |
| md     | 768px     | Large Tablet |
| lg     | 1024px    | Laptop |
| xl     | 1280px    | Desktop |
| 2xl    | 1536px    | Large Desktop |

---

## Responsive Type Scale

Never use fixed sizes. Use responsive typography:

```html
<!-- ✅ GOOD -->
<h1 class="text-2xl sm:text-3xl md:text-4xl lg:text-5xl">Heading</h1>
<!-- Mobile: 24px → Tablet: 30px → Desktop: 48px → Large: 64px -->

<p class="text-sm sm:text-base md:text-lg">Body text</p>
<!-- Mobile: 14px → Tablet: 16px → Desktop: 18px -->

<!-- ❌ BAD -->
<h1 style="font-size: 48px">Fixed size, breaks on mobile</h1>
```

**Recommended Type Scale:**
- **Hero title**: `text-3xl sm:text-4xl md:text-5xl lg:text-6xl`
- **Section heading**: `text-2xl sm:text-3xl md:text-4xl`
- **Card heading**: `text-lg sm:text-xl md:text-2xl`
- **Body text**: `text-sm sm:text-base md:text-lg` (14px mobile → 16px tablet → 18px desktop)
- **Small text**: `text-xs sm:text-sm` (labels, timestamps)

---

## Responsive Spacing & Layout

```html
<!-- ✅ CORRECT - Responsive padding -->
<div class="px-4 sm:px-6 md:px-8 lg:px-12 py-6 sm:py-10 md:py-16">
  Mobile: 16px padding
  Tablet: 24px padding
  Desktop: 32px padding
</div>

<!-- ✅ CORRECT - Container with responsive width -->
<div class="mx-auto max-w-site px-4 sm:px-6 md:px-8">
  <!-- Auto margins, fluid on mobile, fixed max-width on desktop -->
</div>

<!-- ✅ CORRECT - Responsive grid -->
<div class="grid grid-cols-1 gap-4 sm:grid-cols-2 sm:gap-6 md:grid-cols-3 md:gap-8 lg:grid-cols-4">
  Mobile: 1 col, 16px gap
  Tablet: 2 cols, 24px gap
  Desktop: 3-4 cols, 32px gap
</div>

<!-- ✅ CORRECT - Responsive flex direction -->
<div class="flex flex-col gap-4 sm:gap-6 md:flex-row md:gap-8 lg:gap-12">
  Mobile: column stack
  Desktop: row (horizontal)
</div>
```

---

## Navigation: Mobile-Critical Component

**Hero Pattern: Hidden Sidebar on Mobile**
```html
<!-- Sidebar hidden on mobile, visible on lg -->
<aside class="fixed inset-y-0 left-0 w-64 bg-white shadow-lg 
             hidden lg:block lg:static lg:shadow-none">
  <!-- Navigation menu -->
</aside>

<!-- Mobile: hamburger menu button -->
<button class="lg:hidden" id="nav-toggle">
  <svg><!-- hamburger icon --></svg>
</button>

<!-- Mobile overlay backdrop -->
<div id="backdrop" class="fixed inset-0 bg-black/50 
                          hidden [.nav-open_&]:block lg:hidden"></div>
```

**Button Sizes - Touch Targets**
- Minimum 44×44px on mobile (accessibility standard)
- Minimum 48×48px for primary actions

```html
<!-- Mobile-optimized buttons -->
<button class="w-full py-3 px-4 sm:w-auto sm:px-6">
  Full-width on mobile, auto on larger screens
</button>

<!-- Small button -->
<button class="px-3 py-2 text-sm">Secondary action</button>
```

---

## Forms: Mobile-First Input Design

```html
<!-- ✅ MOBILE-FIRST FORM -->
<div class="space-y-4">
  <label class="block text-sm font-medium text-gray-700 mb-1">Email</label>
  
  <!-- Full-width on mobile, flexible on desktop -->
  <input type="email" 
         class="w-full px-3 py-2.5 sm:py-2 border border-gray-300 rounded-md 
                text-base sm:text-sm
                focus:ring-2 focus:ring-teal-500 focus:border-transparent
                disabled:bg-gray-100" />
         <!-- text-base (16px) on mobile prevents iOS auto-zoom -->
  
  <!-- Validation message -->
  <span class="text-red-600 text-xs mt-1 block">Error message</span>
</div>

<!-- Checkbox/Radio - larger touch targets on mobile -->
<label class="flex items-center gap-3 py-3 px-2">
  <input type="checkbox" class="w-5 h-5" />
  <span class="text-sm">Option label</span>
</label>

<!-- Select - native on mobile, custom on desktop if needed -->
<select class="w-full px-3 py-2 border rounded-md text-base sm:text-sm">
  <option>-- Select --</option>
</select>
```

---

## Cards & Content Blocks: Stack on Mobile

```html
<!-- ✅ RESPONSIVE CARD GRID -->
<div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
  <!-- Each card full-width on mobile, 2-up on tablet, 3-up on desktop -->
  <article class="border rounded-lg p-4 sm:p-6 bg-white shadow-sm hover:shadow-md transition-shadow">
    <h3 class="text-lg sm:text-xl font-semibold mb-2 sm:mb-3">Title</h3>
    <p class="text-sm sm:text-base text-gray-600">Content here</p>
  </article>
</div>

<!-- ✅ HERO WITH SIDE CONTENT -->
<div class="flex flex-col gap-6 md:gap-8 lg:flex-row">
  <!-- Left: full-width on mobile, ~50% on desktop -->
  <div class="flex-1 flex flex-col justify-center">
    <h1 class="text-3xl sm:text-4xl md:text-5xl">Big heading</h1>
  </div>
  
  <!-- Right: full-width on mobile, ~50% on desktop, hidden on sm -->
  <div class="flex-1 hidden sm:block">
    <img src="..." alt="" class="w-full h-auto" />
  </div>
</div>
```

---

## Images: Mobile-Optimized

```html
<!-- ✅ RESPONSIVE IMAGES -->
<img src="image-mobile.jpg" 
     srcset="
       image-mobile.jpg 375w,
       image-tablet.jpg 768w,
       image-desktop.jpg 1024w,
       image-large.jpg 1280w"
     sizes="(max-width: 640px) 100vw, 
            (max-width: 1024px) 90vw, 
            1200px"
     alt="Description"
     class="w-full h-auto" />

<!-- OR use picture element for art direction -->
<picture>
  <source srcset="image-mobile.jpg" media="(max-width: 640px)" />
  <source srcset="image-tablet.jpg" media="(max-width: 1024px)" />
  <img src="image-desktop.jpg" alt="Description" class="w-full h-auto" />
</picture>

<!-- Aspect ratio containers for images -->
<div class="aspect-video overflow-hidden rounded-lg">
  <img src="..." alt="" class="w-full h-full object-cover" />
</div>

<div class="aspect-[4/3] overflow-hidden">
  <img src="..." alt="" class="w-full h-full object-cover" />
</div>
```

---

## Tables: Mobile-Friendly Alternatives

**Tables are problematic on mobile.** Use responsive alternatives:

```html
<!-- ❌ AVOID: Regular table on mobile (horizontal scroll mess) -->

<!-- ✅ GOOD: Stack on mobile, table on desktop -->
<div class="overflow-x-auto">
  <table class="w-full text-sm">
    <!-- Shown as-is on desktop, stacked on mobile with custom CSS -->
  </table>
</div>

<!-- ✅ BETTER: Responsive card view on mobile -->
<div class="space-y-4 md:table w-full">
  @foreach (var row in data) {
    <div class="block md:table-row border-b md:border-b mb-4 md:mb-0">
      <div class="block md:table-cell py-2 before:content-['Label'] before:font-bold md:before:content-none">
        @row.Value
      </div>
    </div>
  }
</div>

<!-- OR use data attributes -->
<div class="space-y-3 md:table w-full">
  <div class="md:table-row border-b pb-3 mb-3 md:pb-0 md:mb-0">
    <div data-label="Name" class="md:table-cell py-2">John Doe</div>
    <div data-label="Date" class="md:table-cell py-2">May 2, 2026</div>
  </div>
</div>

<style>
  @media (max-width: 768px) {
    [data-label]::before {
      content: attr(data-label);
      font-weight: 600;
      display: inline-block;
      width: 100px;
    }
  }
</style>
```

---

## Visibility: Show/Hide by Device

```html
<!-- Hide on mobile, show on tablet+ -->
<div class="hidden sm:block">
  Visible only on tablets and up
</div>

<!-- Show on mobile, hide on tablet+ -->
<div class="sm:hidden">
  Mobile-only content
</div>

<!-- Hide on mobile, show on desktop -->
<div class="hidden lg:block">
  Desktop feature
</div>

<!-- Different content per device -->
<picture>
  <source media="(max-width: 640px)" srcset="mobile-nav.jpg" />
  <img src="desktop-nav.jpg" alt="Navigation" />
</picture>
```

---

## Performance: Mobile Network Matters

Since users access via mobile networks (often 3G/4G in Nigeria):

```html
<!-- ✅ Lazy load images -->
<img src="..." loading="lazy" />

<!-- ✅ Responsive image sizes (smaller on mobile) -->
<img src="image-small.jpg" 
     srcset="image-small.jpg 375w, image-large.jpg 1200w"
     sizes="(max-width: 640px) 375px, 1200px" />

<!-- ✅ Async JavaScript (non-critical) -->
<script src="analytics.js" async defer></script>

<!-- ✅ Use WebP with fallback -->
<picture>
  <source srcset="image.webp" type="image/webp" />
  <img src="image.jpg" alt="" />
</picture>
```

---

## Orientation: Portrait-Focused Design

Most mobile users hold phones in **portrait** mode:
- Width: 320-430px
- Height: 600-900px
- Limited horizontal space
- Vertical scrolling expected

```html
<!-- ✅ Portrait-first -->
<div class="w-full flex flex-col">
  <!-- Stack vertically by default -->
</div>

<!-- Landscape should be graceful fallback, not primary -->
@media (orientation: landscape) {
  /* Adjust if needed, but portrait design should work */
}
```

---

## Testing Checklist (Mobile-First)

Before marking any view complete, test on:

- [ ] **Mobile (375px)** - iPhone SE / Galaxy A
  - [ ] All text readable without horizontal scroll
  - [ ] Buttons/inputs are 44px+ tall
  - [ ] Images load and scale correctly
  - [ ] Navigation is accessible
  - [ ] Forms work without keyboard-jumping

- [ ] **Tablet (768px)** - iPad
  - [ ] 2-column layouts work
  - [ ] Images scale appropriately
  - [ ] Spacing looks balanced

- [ ] **Desktop (1280px)**
  - [ ] Full grid/multi-column layouts render
  - [ ] Max-width container appears centered
  - [ ] No horizontal scroll

- [ ] **Network:**
  - [ ] Works on 3G (DevTools throttle to "Fast 3G")
  - [ ] Images lazy-load
  - [ ] Navigation is responsive

- [ ] **Accessibility:**
  - [ ] Touch targets are 44×44px minimum
  - [ ] Text contrast passes WCAG AA
  - [ ] Can zoom to 200% without breaking layout

---

## .NET Packages for Mobile Support

### 1. **Device Detection** (Optional - not recommended, use responsive CSS instead)
```bash
dotnet add package Mobile.Detection
```
**Note:** Client-side responsive design is preferred over server-side detection.

### 2. **Progressive Web App (PWA) Support**
```bash
dotnet add package WebEssentials.AspNetCore.ServiceWorker
```
Enables offline support, install-to-home screen, push notifications.

**Usage:**
```html
<!-- In _Layout.cshtml -->
<link rel="manifest" href="/site.webmanifest" />
<script>
  if ('serviceWorker' in navigator) {
    navigator.serviceWorker.register('/service-worker.js');
  }
</script>
```

### 3. **Responsive Image Handling**
```bash
dotnet add package SixLabors.ImageSharp  # Image resizing on server
dotnet add package ImageSharp.Web        # For on-the-fly optimization
```

### 4. **Mobile-Specific Headers**
Already built into ASP.NET Core:
```csharp
// In Program.cs
app.Use(async (context, next) => {
    context.Response.Headers.Add("X-UA-Compatible", "IE=edge");
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    await next();
});
```

---

## Current Tech Stack (Already Mobile-Friendly)

✅ **Tailwind CSS 4.0** - Mobile-first utility classes  
✅ **Alpine.js 3.x** - Lightweight interactivity (good for mobile)  
✅ **ASP.NET Core 10** - Fast, server-side rendering  
✅ **No heavy JS frameworks** - Better mobile performance  

**Recommendation:** Stick with current stack. Add:
- [ ] Consider WebEssentials for PWA support
- [ ] Implement image optimization middleware
- [ ] Add service worker for offline support

---

## Checklist for Every View Refactor

When working on a view, ask Copilot:

> "Refactor [ViewName].cshtml with **mobile-first** approach:
> - Start with mobile layout (1 column, full-width)
> - Add `sm:`, `md:`, `lg:` breakpoints progressively
> - Ensure 44px touch targets
> - Use responsive text sizes (`text-sm sm:text-base md:text-lg`)
> - Test all breakpoints: 375px → 768px → 1024px → 1280px
> - No horizontal scroll on mobile
> - Images lazy-load and scale responsively"

---

## Quick Reference: Mobile-First Pattern

```html
<!-- TEMPLATE: Use this for every layout -->

<!-- 1. MOBILE BASE (no prefix) -->
<div class="px-4 py-6 text-base">
  
  <!-- 2. TABLET+ (sm:) -->
  <div class="sm:px-6 sm:text-base">
    
    <!-- 3. LARGE TABLET+ (md:) -->
    <div class="md:px-8 md:text-lg md:flex gap-8">
      
      <!-- 4. DESKTOP+ (lg:) -->
      <div class="lg:px-12 lg:py-12">
        Content
      </div>
    </div>
  </div>
</div>
```

---

## Summary

**Mobile-first = Progressive Enhancement**
1. ✅ Build core experience for 320px screens
2. ✅ Add responsive breakpoints (`sm:`, `md:`, `lg:`)
3. ✅ Enhance with desktop features
4. ✅ Test on real devices (not just DevTools)
5. ✅ Performance first — lean CSS, lazy images, fast navigation

**Never:**
- ❌ Build desktop-first then try to shrink
- ❌ Use fixed widths/heights
- ❌ Hide content only on mobile (show sensible alternatives)
- ❌ Forget the viewport meta tag
- ❌ Ignore touch targets (44px minimum)
