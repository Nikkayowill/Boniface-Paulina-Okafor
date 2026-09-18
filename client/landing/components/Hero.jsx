import BookingChooser from "./BookingChooser.jsx";
import CaretRightIcon from "./CaretRightIcon.jsx";

const IMAGES = "/images/landing";

export default function Hero({ urls }) {
  return (
    <>
      <section className="home-hero" aria-labelledby="home-hero-title">
        <img
          className="home-hero__palm home-hero__palm--left"
          src={`${IMAGES}/hero-palm-740.webp`}
          width="740"
          height="740"
          alt=""
        />
        <img
          className="home-hero__palm home-hero__palm--right"
          src={`${IMAGES}/hero-palm-740.webp`}
          width="740"
          height="740"
          alt=""
        />

        <div className="home-hero__content">
          <p className="home-hero__kicker">
            Boniface and Paulina Okafor <br className="home-hero__kicker-break" />
            Memorial Hospital
          </p>
          <h1 id="home-hero-title" className="home-hero__title">
            <span className="home-hero__title-thin">Health is</span> <strong>Wealth.</strong>
          </h1>
          <p className="home-hero__tagline">
            Here, <strong>Your Life</strong> Matters More Than<strong> A Budget.</strong>
          </p>
          <div className="home-hero__actions">
            <BookingChooser urls={urls} label="Book an Appointment" />
            <a href={urls.contact} className="home-button home-button--outline">
              Contact Us
              <CaretRightIcon />
            </a>
          </div>
        </div>
      </section>

      <div className="home-photo">
        <picture>
          <source
            media="(max-width: 719px)"
            srcSet={`${IMAGES}/hero-photo-mobile-804.webp 804w, ${IMAGES}/hero-photo-mobile-1206.webp 1206w`}
            sizes="100vw"
          />
          <img
            src={`${IMAGES}/hero-photo-1920.webp`}
            srcSet={`${IMAGES}/hero-photo-960.webp 960w, ${IMAGES}/hero-photo-1920.webp 1920w, ${IMAGES}/hero-photo-2750.webp 2750w`}
            sizes="100vw"
            width="1920"
            height="453"
            alt="The carved hospital sign dedicated to the memory of Boniface and Paulina Okafor"
            fetchPriority="high"
            decoding="async"
          />
        </picture>
      </div>
    </>
  );
}
