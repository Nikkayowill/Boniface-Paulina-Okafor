export default function Contact({ contact, urls }) {
  return (
    <section className="hospital-section hospital-contact" aria-labelledby="contact-title">
      <div className="public-wrap hospital-contact__grid">
        <div className="hospital-contact__copy">
          <p className="hospital-kicker">Visit or contact us</p>
          <h2 id="contact-title">Speak with the hospital before you travel.</h2>
          <p>Confirm clinic availability, directions, and current visiting details with the team.</p>
          <div className="hospital-contact__actions">
            <a href={urls.appointmentCreate} className="site-button site-button--primary">
              Request appointment
            </a>
            <a href={urls.contact} className="site-button site-button--secondary">
              Contact us
            </a>
          </div>
        </div>

        <div className="hospital-contact__details">
          <dl>
            <div>
              <dt>Hospital</dt>
              <dd>{contact.hospitalName}</dd>
            </div>
            <div>
              <dt>Location</dt>
              <dd>{contact.hospitalAddress || "Please contact the hospital for current location details."}</dd>
            </div>
            <div>
              <dt>Emergency</dt>
              <dd>{contact.emergencyNumbers || "Please use the contact page to reach the hospital team."}</dd>
            </div>
            <div>
              <dt>Email</dt>
              <dd>
                {contact.hospitalEmail ? (
                  <a href={`mailto:${contact.hospitalEmail}`}>{contact.hospitalEmail}</a>
                ) : (
                  <span>Please use the contact form for email enquiries.</span>
                )}
              </dd>
            </div>
          </dl>
        </div>

        <div className="hospital-map">
          {contact.mapUrl ? (
            <iframe
              title="Map to Boniface and Paulina Okafor Memorial Hospital"
              src={contact.mapUrl}
              loading="lazy"
              referrerPolicy="no-referrer-when-downgrade"
            />
          ) : (
            <span>Directions available from the hospital team.</span>
          )}
        </div>
      </div>
    </section>
  );
}
