import { useEffect, useId, useRef, useState } from "react";
import CaretRightIcon from "./CaretRightIcon.jsx";

// Booking always covers both an in-person visit and a teleconsultation, so a
// "Book" action opens this two-option chooser instead of picking one for the patient.
export default function BookingChooser({ urls, label, appearance = "button" }) {
  const [open, setOpen] = useState(false);
  const panelId = useId();
  const rootRef = useRef(null);

  useEffect(() => {
    if (!open) {
      return undefined;
    }

    function onPointerDown(event) {
      if (!rootRef.current.contains(event.target)) {
        setOpen(false);
      }
    }

    function onKeyDown(event) {
      if (event.key === "Escape") {
        setOpen(false);
      }
    }

    document.addEventListener("pointerdown", onPointerDown);
    document.addEventListener("keydown", onKeyDown);
    return () => {
      document.removeEventListener("pointerdown", onPointerDown);
      document.removeEventListener("keydown", onKeyDown);
    };
  }, [open]);

  const triggerClass = appearance === "button" ? "home-button home-button--solid" : "home-text-action";

  return (
    <div className="home-booking" ref={rootRef}>
      <button
        type="button"
        className={triggerClass}
        aria-expanded={open}
        aria-controls={panelId}
        onClick={() => setOpen((value) => !value)}
      >
        {label}
        {appearance === "button" && <CaretRightIcon />}
      </button>

      {open && (
        <div id={panelId} className="home-booking__panel" role="group" aria-label="Choose how to book">
          <a href={urls.appointmentCreate} className="home-booking__option" data-live-feature="appointment">
            <strong>Visit the hospital</strong>
            <span>Book an in-person appointment</span>
          </a>
          <a href={urls.teleconsultationCreate} className="home-booking__option" data-live-feature="teleconsultation">
            <strong>Video consultation</strong>
            <span>See a doctor from home</span>
          </a>
        </div>
      )}
    </div>
  );
}
