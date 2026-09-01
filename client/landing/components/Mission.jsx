export default function Mission({ portraitImage, mission }) {
  return (
    <section className="hospital-section hospital-section--deep" aria-labelledby="mission-title">
      <div className="hospital-mission">
        <figure className="hospital-mission__media">
          <img
            src={portraitImage}
            alt="Patients from different generations seated together at the hospital"
            loading="lazy"
          />
        </figure>
        <div className="hospital-mission__copy">
          <p className="hospital-kicker">Care philosophy</p>
          <h2 id="mission-title">Care should start with less confusion.</h2>
          <p>
            Families come with symptoms, bills, directions, follow-up questions, and worries
            about what happens next. The hospital website keeps those needs visible so patients
            can act with more confidence.
          </p>
          <ul className="hospital-proof-list">
            {mission.trustPoints.map((point) => (
              <li key={point}>{point}</li>
            ))}
          </ul>
        </div>
      </div>
    </section>
  );
}
