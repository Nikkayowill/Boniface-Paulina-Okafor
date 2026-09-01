import { useEffect, useRef, useState } from "react";

const AUTOPLAY_MS = 6500;
const SWIPE_THRESHOLD_PX = 45;

function prefersReducedMotion() {
  return (
    typeof window !== "undefined" &&
    window.matchMedia("(prefers-reduced-motion: reduce)").matches
  );
}

// Ported from the pre-React wwwroot/js/hero-carousel.js. Behaviour kept intentionally
// identical: indicators only (no prev/next/pause buttons -- see
// ResponsiveDesignTests.HeroCarousel_UsesMinimalIndicatorsWithoutArrowOrPauseControls),
// 6.5s autoplay that pauses on hover/focus/reduced-motion/hidden tab, and horizontal
// touch swipe. Neighbouring slide images are switched to eager/high-priority loading
// as they become adjacent to the current slide instead of only starting to download
// once they slide into view.
export default function Hero({ hero, careDock, urls }) {
  const slides = hero.slides;
  const slideCount = slides.length;
  const [currentIndex, setCurrentIndex] = useState(0);
  const [announce, setAnnounce] = useState("");
  const [userPaused, setUserPaused] = useState(prefersReducedMotion());
  const [interactionPaused, setInteractionPaused] = useState(false);
  const [documentHidden, setDocumentHidden] = useState(
    typeof document !== "undefined" ? document.hidden : false,
  );
  const touchStartXRef = useRef(null);

  function goToSlide(index, shouldAnnounce) {
    const next = ((index % slideCount) + slideCount) % slideCount;
    setCurrentIndex(next);
    if (shouldAnnounce) {
      const label = slides[next].caption || "Hospital photograph";
      setAnnounce(`Image ${next + 1} of ${slideCount}: ${label}`);
    }
  }

  useEffect(() => {
    if (userPaused || interactionPaused || documentHidden || slideCount < 2) {
      return undefined;
    }

    const timerId = window.setInterval(() => {
      setCurrentIndex((index) => (index + 1) % slideCount);
    }, AUTOPLAY_MS);

    return () => window.clearInterval(timerId);
  }, [userPaused, interactionPaused, documentHidden, currentIndex, slideCount]);

  useEffect(() => {
    function onVisibilityChange() {
      setDocumentHidden(document.hidden);
    }

    document.addEventListener("visibilitychange", onVisibilityChange);
    return () => document.removeEventListener("visibilitychange", onVisibilityChange);
  }, []);

  useEffect(() => {
    const media = window.matchMedia("(prefers-reduced-motion: reduce)");

    function onChange(event) {
      if (event.matches) {
        setUserPaused(true);
      }
    }

    media.addEventListener("change", onChange);
    return () => media.removeEventListener("change", onChange);
  }, []);

  function handleTouchStart(event) {
    touchStartXRef.current = event.changedTouches[0].clientX;
  }

  function handleTouchEnd(event) {
    if (touchStartXRef.current === null) {
      return;
    }

    const distance = event.changedTouches[0].clientX - touchStartXRef.current;
    touchStartXRef.current = null;

    if (Math.abs(distance) < SWIPE_THRESHOLD_PX) {
      return;
    }

    goToSlide(currentIndex + (distance < 0 ? 1 : -1), true);
  }

  function isWarm(index) {
    const distance = Math.min(
      Math.abs(index - currentIndex),
      slideCount - Math.abs(index - currentIndex),
    );
    return distance <= 1;
  }

  return (
    <section className="hospital-hero" aria-labelledby="home-hero-title">
      <div className="hospital-hero__content public-wrap">
        <div className="hospital-hero__copy">
          <p className="hospital-kicker">
            <strong>{hero.hospitalName}</strong>
          </p>
          <h1 id="home-hero-title">
            Welcome to <strong>B&amp;P Hospital</strong>
          </h1>
          <span className="hospital-hero__belief">
            Our core belief: <strong>Health is Wealth!</strong>
          </span>
          <p className="hospital-hero__lead">{hero.lead}</p>
          <div className="hospital-hero__actions" aria-label="Primary actions">
            <a href={urls.appointmentCreate} className="site-button site-button--primary">
              Request appointment
            </a>
            <a href={urls.teleconsultationCreate} className="site-button site-button--light">
              Ask about teleconsultation
            </a>
          </div>
        </div>
      </div>

      <div
        className="hospital-hero__carousel"
        role="region"
        aria-roledescription="carousel"
        aria-label="Hospital and community photographs"
        onMouseEnter={() => setInteractionPaused(true)}
        onMouseLeave={() => setInteractionPaused(false)}
        onFocus={() => setInteractionPaused(true)}
        onBlur={(event) => {
          if (!event.currentTarget.contains(event.relatedTarget)) {
            setInteractionPaused(false);
          }
        }}
      >
        <div className="hospital-hero__viewport" onTouchStart={handleTouchStart} onTouchEnd={handleTouchEnd}>
          <div
            className="hospital-hero__track"
            style={{ transform: `translateX(-${currentIndex * 100}%)` }}
          >
            {slides.map((slide, index) => {
              const isCurrent = index === currentIndex;
              const warm = isWarm(index);

              return (
                <figure
                  key={slide.image}
                  className={`hospital-hero__slide${isCurrent ? " is-active" : ""}`}
                  aria-hidden={isCurrent ? "false" : "true"}
                >
                  <picture>
                    <source media="(max-width: 719px)" srcSet={slide.mobileImage} />
                    <img
                      src={slide.image}
                      width={slide.width}
                      height={slide.height}
                      alt={slide.alt}
                      loading={isCurrent || warm ? "eager" : "lazy"}
                      fetchPriority={isCurrent ? "high" : warm ? "auto" : "low"}
                      decoding="async"
                    />
                  </picture>
                  <figcaption>{slide.caption}</figcaption>
                </figure>
              );
            })}
          </div>
        </div>

        <div className="hospital-hero__carousel-ui">
          <div className="hospital-hero__indicators" aria-label="Choose a carousel image">
            {slides.map((slide, index) => (
              <button
                key={slide.image}
                type="button"
                aria-label={`Show image ${index + 1}`}
                aria-current={index === currentIndex ? "true" : "false"}
                onClick={() => goToSlide(index, true)}
              />
            ))}
          </div>
        </div>
        <p className="sr-only" aria-live="polite">
          {announce}
        </p>
      </div>

      <div className="hospital-care-dock public-wrap" aria-label="Care shortcuts">
        <a href={urls.contact}>
          <span>Emergency line</span>
          <strong>{careDock.emergencyNumbers || "Contact hospital"}</strong>
        </a>
        <a href={urls.services}>
          <span>Services</span>
          <strong>Choose a department</strong>
        </a>
        <a href={urls.patientInfo}>
          <span>Patient information</span>
          <strong>Prevention guides</strong>
        </a>
      </div>
    </section>
  );
}
