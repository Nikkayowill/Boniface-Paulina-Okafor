import BookingChooser from "./BookingChooser.jsx";
import CaretRightIcon from "./CaretRightIcon.jsx";

const ICONS = "/images/icons/services";

// Copy, order and calls to action are exactly as in the Figma design.
const SERVICES = [
  {
    title: "24/7 Medical Consultation",
    body: "Round-the-clock care for emergencies and urgent medical needs. Our doctors are always available when you need us.",
    icon: `${ICONS}/medical-consultation.svg`,
    action: { kind: "book", label: "Book an appointment" },
  },
  {
    title: "Antenatal Care",
    body: "Comprehensive prenatal care ensuring healthy pregnancies and safe deliveries. We focus on both mother and baby’s wellbeing.",
    icon: `${ICONS}/antenatal-care.svg`,
    action: { kind: "book", label: "Book an appointment" },
  },
  {
    title: "Surgery",
    body: "Safe, professional surgical services with experienced medical team. From minor procedures to complex surgeries.",
    icon: `${ICONS}/surgery.svg`,
    action: { kind: "link", label: "Learn more", url: (urls) => `${urls.services}#surgery` },
  },
  {
    title: "Telemedicine",
    body: "Connect with specialist doctors via video consultation. Expert care, no travel required.",
    icon: `${ICONS}/telemedicine.svg`,
    action: { kind: "link", label: "Get started", url: (urls) => urls.teleconsultationCreate },
  },
  {
    title: "Eye Care",
    body: "Professional eye examinations, treatments, and referrals. Protecting your vision, locally.",
    icon: `${ICONS}/eye-care.svg`,
    action: { kind: "book", label: "Book an appointment" },
  },
  {
    title: "Primary Care",
    body: "General health services for the whole family. Preventive care, treatment, and health education.",
    icon: `${ICONS}/primary-care.png`,
    action: { kind: "book", label: "Get started" },
  },
];

export default function ServiceCards({ urls }) {
  return (
    <section className="home-services" aria-labelledby="home-services-title">
      <div className="home-services__layout bp-wrap">
        <h2 id="home-services-title" className="home-heading">
          Comprehensive Care for Your Whole Family.
        </h2>

        <a href={urls.services} className="home-services__all">
          View all services
          <CaretRightIcon />
        </a>

        <ul className="home-services__grid" role="list">
          {SERVICES.map((service) => (
            <li key={service.title} className="home-service-card">
              <img className="home-service-card__icon" src={service.icon} width="48" height="48" alt="" />
              <div className="home-service-card__text">
                <h3>{service.title}</h3>
                <p>{service.body}</p>
              </div>
              <div className="home-service-card__action">
                {service.action.kind === "book" ? (
                  <BookingChooser urls={urls} label={service.action.label} appearance="text" />
                ) : (
                  <a href={service.action.url(urls)} className="home-text-action">
                    {service.action.label}
                  </a>
                )}
              </div>
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}
