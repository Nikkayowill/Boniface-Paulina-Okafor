export default function Partner() {
  return (
    <section className="hospital-partner" aria-labelledby="partner-title">
      <div className="public-wrap hospital-partner__grid">
        <div className="hospital-partner__copy">
          <p className="hospital-kicker">Hospital support partner</p>
          <h2 id="partner-title">Care strengthened by the Nigeria Family Helper Program.</h2>
          <p>
            The program helps advance accessible, subsidized healthcare and supports the
            hospital&apos;s work with mothers, infants, seniors, and families in vulnerable
            circumstances.
          </p>
        </div>
        <a
          href="https://www.nigeriafamilyhelperprogram.org/boniface-and-paulina-okafor-hospital"
          target="_blank"
          rel="noopener noreferrer"
          className="site-button site-button--secondary"
        >
          Visit partner website
          <span aria-hidden="true">↗</span>
        </a>
      </div>
    </section>
  );
}
