const IMAGES = "/images/landing";

// Copy is exactly as in the Figma design. On desktop the photo alternates sides;
// on phones every story reads text first, then photo.
const STORIES = [
  {
    image: "care-about-you",
    title: ["We Don’t Just Care for Your Health.", "We Care About You."],
    body: "A headache isn’t just a symptom to us. It’s your story. Your stress. Your life. We treat the person, not just the problem. That’s why patients call us the ‘miracle hospital’—because we listen, we see you, and we act with genuine compassion",
    alt: "A priest and hospital staff sitting with an elderly patient and her family",
  },
  {
    image: "stronger-together",
    title: ["We’re Stronger Together.", "That’s How We Started."],
    body: "This hospital wasn’t built by one person. It was born from a family’s pain and built by people who cared enough to cross continents. From Canada to Nigeria. From loss to hope. Every patient, every donor, every staff member—you’re part of our family. Your trust makes us stronger",
    alt: "Hospital staff and supporters standing together in front of the hospital",
  },
  {
    image: "life-over-budget",
    title: ["Your Life Matters More Than Our Budget."],
    body: "We don’t ask ‘Can you afford this?’ We ask ‘Do you need this?’ Subsidized care. Affordable treatment. No impossible choices between health and survival. Because at B&P, human life comes first. Always",
    alt: "Patients waiting while a nurse works at the reception desk",
  },
  {
    image: "show-up",
    title: ["We Show Up When It Matters Most."],
    body: "24/7. No waiting for morning. No unreachable doctors. No ‘come back tomorrow.’ When your child has a fever at midnight, when your mother collapses, when you need help—we’re here. Right now. Because emergencies don’t keep office hours.",
    alt: "A nurse checking a patient’s blood pressure while a colleague takes notes",
  },
  {
    image: "holistic-care",
    title: ["Medicine for Your Body, Support for Your Mind, Hope for Your Spirit."],
    body: "Founded by a priest and a clinical psychologist, we believe real healing addresses the whole you—physical, emotional, psychological, and spiritual. That’s holistic care.",
    alt: "Religious sisters, a priest and hospital staff gathered outside the hospital",
  },
];

export default function WhyDifferent() {
  return (
    <section className="home-different" aria-labelledby="home-different-title">
      <div className="bp-wrap">
        <div className="home-different__intro">
          <h2 id="home-different-title" className="home-heading">
            What Makes Us Different?
          </h2>
          <p>
            For over a decade, Boniface &amp; Paulina Okafor Memorial Hospital has been serving rural
            families with excellence and heart. Born from real loss, built on genuine care—we’re here
            for you.
          </p>
        </div>

        <div className="home-stories">
          {STORIES.map((story, index) => (
            <article
              key={story.image}
              className={`home-story${index % 2 === 1 ? " home-story--media-first" : ""}`}
            >
              <div className="home-story__text">
                <div className="home-story__copy">
                  <h3>
                    {story.title.map((line, lineIndex) => (
                      <span key={line}>
                        {lineIndex > 0 && (
                          <>
                            {" "}
                            <br className="home-story__break" />
                          </>
                        )}
                        {line}
                      </span>
                    ))}
                  </h3>
                  <p>{story.body}</p>
                </div>
              </div>
              <div className="home-story__media">
                <picture>
                  <source
                    media="(max-width: 719px)"
                    srcSet={`${IMAGES}/story-${story.image}-mobile-724.webp 724w, ${IMAGES}/story-${story.image}-mobile-1086.webp 1086w`}
                    sizes="100vw"
                  />
                  <img
                    src={`${IMAGES}/story-${story.image}-1218.webp`}
                    srcSet={`${IMAGES}/story-${story.image}-609.webp 609w, ${IMAGES}/story-${story.image}-1218.webp 1218w`}
                    sizes="(min-width: 1316px) 609px, 50vw"
                    width="609"
                    height="348"
                    alt={story.alt}
                    loading="lazy"
                    decoding="async"
                  />
                </picture>
              </div>
            </article>
          ))}
        </div>
      </div>
    </section>
  );
}
