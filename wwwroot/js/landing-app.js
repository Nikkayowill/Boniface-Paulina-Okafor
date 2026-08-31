// Landing page mount point.
//
// Views/Home/Index.cshtml renders plain, unstyled markup inside
// <div id="landing-app">...</div> and loads this file at the end of <body>.
//
// Drop your compiled bundle's output here (or point this <script> tag at it —
// see @section Scripts in Views/Home/Index.cshtml) and mount your React/JS
// design over #landing-app.
//
// Notes for wiring it up:
// - The layout serves a strict CSP with a per-request nonce. Any extra
//   <script>/<style> tags you add to Index.cshtml need
//   nonce="@(Context.Items["CspNonce"])" the same way this tag does, or the
//   browser will silently block them. Injecting styles/scripts from *this*
//   file at runtime (e.g. React itself) is not covered by that nonce and
//   should be fine as long as it isn't inline <script>/<style> added to the
//   DOM by document.write or innerHTML.
// - See docs/LANDING_PAGE_HANDOFF.md for the full handoff notes: what
//   copy/data is still live from the server, available routes, and where
//   the old visual design lives in git history for reference.
