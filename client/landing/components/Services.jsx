export default function Services({ services, urls }) {
  const items = services.items;

  return (
    <section className="hospital-section hospital-services" aria-labelledby="services-title">
      <div className="public-wrap hospital-services__layout">
        <div className="hospital-services__header">
          <p className="hospital-kicker">Clinical services</p>
          <h2 id="services-title">Find the department that fits your health concern.</h2>
          <p>Start with featured services, then view the full services page for more detail.</p>
          <a href={urls.services} className="site-button site-button--secondary">
            View all services
          </a>
        </div>

        <div className="hospital-service-list" aria-label="Featured departments">
          {items.length > 0 ? (
            items.map((service) => (
              <article key={service.name} className="hospital-service-row">
                <span>{service.name}</span>
                <p>{service.excerpt}</p>
              </article>
            ))
          ) : (
            <article className="hospital-service-row">
              <span>Care guidance available</span>
              <p>Please contact the hospital team for current department and service details.</p>
            </article>
          )}
        </div>
      </div>
    </section>
  );
}
