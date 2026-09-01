export default function Overview({ signImage, urls }) {
  return (
    <section className="hospital-section hospital-overview" aria-labelledby="overview-title">
      <div className="public-wrap hospital-overview__grid">
        <figure className="hospital-overview__media">
          <img
            src={signImage}
            alt="Boniface and Paulina Okafor Memorial Hospital sign at the facility entrance"
            loading="lazy"
          />
        </figure>
        <div className="hospital-overview__copy">
          <p className="hospital-kicker">Inside B&amp;P Hospital</p>
          <h2 id="overview-title">A welcoming hospital for care close to home.</h2>
          <p>
            Find the services, care teams, patient information, and practical next steps you
            need before visiting Boniface and Paulina Okafor Memorial Hospital.
          </p>
          <div className="hospital-contact__actions">
            <a href={urls.services} className="site-button site-button--primary">
              Explore hospital services
            </a>
            <a href={urls.gallery} className="site-button site-button--secondary">
              View hospital gallery
            </a>
          </div>
        </div>
      </div>
    </section>
  );
}
