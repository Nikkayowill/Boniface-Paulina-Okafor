export default function CareRoutes({ routes }) {
  return (
    <section className="hospital-section hospital-routes" aria-labelledby="routes-title">
      <div className="public-wrap">
        <div className="hospital-section-heading">
          <p className="hospital-kicker">Care routes</p>
          <h2 id="routes-title">Choose the care path that matches your situation.</h2>
        </div>

        <div className="hospital-route-grid">
          {routes.map((route) => (
            <a
              key={route.label}
              href={route.href}
              target={route.external ? "_blank" : undefined}
              rel={route.external ? "noopener noreferrer" : undefined}
              className="hospital-route"
            >
              <span>{route.label}</span>
              <strong>{route.title}</strong>
              <em>{route.body}</em>
            </a>
          ))}
        </div>
      </div>
    </section>
  );
}
