import Hero from "./components/Hero.jsx";
import CareRoutes from "./components/CareRoutes.jsx";
import Mission from "./components/Mission.jsx";
import Services from "./components/Services.jsx";
import Overview from "./components/Overview.jsx";
import Partner from "./components/Partner.jsx";
import Contact from "./components/Contact.jsx";

// The public landing page, owned end-to-end by this component tree. The ASP.NET
// Core backend still renders the shell around it (Views/Shared/_Layout.cshtml --
// header, nav, footer, SEO meta, JSON-LD) and hands this tree its content and
// route URLs as one server-computed `data` payload (see Views/Home/Index.cshtml).
// This is not a headless split: there is no separate API-only backend and no
// client-side router. See docs/LANDING_PAGE_HANDOFF.md and
// docs/decisions/0006-react-landing-page-non-headless.md.
export default function App({ data }) {
  return (
    <>
      <Hero hero={data.hero} careDock={data.careDock} urls={data.urls} />
      <CareRoutes routes={data.careRoutes} />
      <Mission portraitImage={data.portraitImage} mission={data.mission} />
      <Services services={data.services} urls={data.urls} />
      <Overview signImage={data.signImage} urls={data.urls} />
      <Partner />
      <Contact contact={data.contact} urls={data.urls} />
    </>
  );
}
