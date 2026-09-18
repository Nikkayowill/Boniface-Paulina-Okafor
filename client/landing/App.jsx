import Hero from "./components/Hero.jsx";
import ServiceCards from "./components/ServiceCards.jsx";
import WhyDifferent from "./components/WhyDifferent.jsx";

// The public landing page, owned end-to-end by this component tree. The ASP.NET
// Core backend still renders the shell around it (Views/Shared/_Layout.cshtml --
// header, nav, footer, SEO meta, JSON-LD) and hands this tree its route URLs as
// one server-computed `data` payload (see Views/Home/Index.cshtml). This is not a
// headless split: there is no separate API-only backend and no client-side router.
// See docs/LANDING_PAGE_HANDOFF.md and
// docs/decisions/0006-react-landing-page-non-headless.md.
export default function App({ data }) {
  return (
    <>
      <Hero urls={data.urls} />
      <ServiceCards urls={data.urls} />
      <WhyDifferent />
    </>
  );
}
