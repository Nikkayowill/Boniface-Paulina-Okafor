(function () {
    "use strict";

    function initializeCarousels() {
        document.querySelectorAll("[data-hero-carousel]").forEach(initializeCarousel);
    }

    function initializeCarousel(carousel) {
        if (carousel.dataset.carouselReady === "true") {
            return;
        }

        var track = carousel.querySelector("[data-carousel-track]");
        var viewport = carousel.querySelector("[data-carousel-viewport]");
        var slides = Array.from(carousel.querySelectorAll("[data-carousel-slide]"));
        var indicators = Array.from(carousel.querySelectorAll("[data-carousel-go]"));
        var previousButton = carousel.querySelector("[data-carousel-prev]");
        var nextButton = carousel.querySelector("[data-carousel-next]");
        var toggleButton = carousel.querySelector("[data-carousel-toggle]");
        var status = carousel.querySelector("[data-carousel-status]");

        if (!track || !viewport || slides.length < 2) {
            return;
        }

        carousel.dataset.carouselReady = "true";

        var reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
        var currentIndex = 0;
        var timerId = null;
        var userPaused = reducedMotion.matches;
        var interactionPaused = false;
        var touchStartX = null;

        // Slides past the first are lazy, so without this each one only begins
        // downloading as it slides in and the transition lands on the empty navy
        // panel. Warming the neighbours means the image is already decoded by the
        // time it is on screen, in either direction.
        function warmSlide(index) {
            var slide = slides[(index + slides.length) % slides.length];
            var image = slide && slide.querySelector("img");

            if (image && image.loading === "lazy") {
                image.loading = "eager";
                if (image.fetchPriority === "low") {
                    image.fetchPriority = "auto";
                }
            }
        }

        function updateCarousel(announceChange) {
            track.style.transform = "translateX(-" + currentIndex * 100 + "%)";

            warmSlide(currentIndex + 1);
            warmSlide(currentIndex - 1);

            slides.forEach(function (slide, index) {
                var isCurrent = index === currentIndex;
                slide.classList.toggle("is-active", isCurrent);
                slide.setAttribute("aria-hidden", isCurrent ? "false" : "true");
            });

            indicators.forEach(function (indicator, index) {
                indicator.setAttribute("aria-current", index === currentIndex ? "true" : "false");
            });

            if (announceChange && status) {
                var label = slides[currentIndex].dataset.carouselLabel || "Hospital photograph";
                status.textContent = "Image " + (currentIndex + 1) + " of " + slides.length + ": " + label;
            }
        }

        function updateToggleButton() {
            if (!toggleButton) {
                return;
            }

            toggleButton.textContent = userPaused ? "Play" : "Pause";
            toggleButton.setAttribute(
                "aria-label",
                userPaused ? "Play image carousel" : "Pause image carousel"
            );
        }

        function stopTimer() {
            if (timerId !== null) {
                window.clearInterval(timerId);
                timerId = null;
            }
        }

        function restartTimer() {
            stopTimer();

            if (userPaused || interactionPaused || document.hidden) {
                return;
            }

            timerId = window.setInterval(function () {
                goToSlide(currentIndex + 1, false);
            }, 6500);
        }

        function goToSlide(index, announceChange) {
            currentIndex = (index + slides.length) % slides.length;
            updateCarousel(announceChange);
            restartTimer();
        }

        if (previousButton) {
            previousButton.addEventListener("click", function () {
                goToSlide(currentIndex - 1, true);
            });
        }

        if (nextButton) {
            nextButton.addEventListener("click", function () {
                goToSlide(currentIndex + 1, true);
            });
        }

        if (toggleButton) {
            toggleButton.addEventListener("click", function () {
                userPaused = !userPaused;
                updateToggleButton();
                restartTimer();
            });
        }

        indicators.forEach(function (indicator) {
            indicator.addEventListener("click", function () {
                goToSlide(Number(indicator.dataset.carouselGo), true);
            });
        });

        carousel.addEventListener("mouseenter", function () {
            interactionPaused = true;
            stopTimer();
        });

        carousel.addEventListener("mouseleave", function () {
            interactionPaused = false;
            restartTimer();
        });

        carousel.addEventListener("focusin", function () {
            interactionPaused = true;
            stopTimer();
        });

        carousel.addEventListener("focusout", function (event) {
            if (!carousel.contains(event.relatedTarget)) {
                interactionPaused = false;
                restartTimer();
            }
        });

        viewport.addEventListener("touchstart", function (event) {
            touchStartX = event.changedTouches[0].clientX;
        }, { passive: true });

        viewport.addEventListener("touchend", function (event) {
            if (touchStartX === null) {
                return;
            }

            var distance = event.changedTouches[0].clientX - touchStartX;
            touchStartX = null;

            if (Math.abs(distance) < 45) {
                return;
            }

            goToSlide(currentIndex + (distance < 0 ? 1 : -1), true);
        }, { passive: true });

        document.addEventListener("visibilitychange", restartTimer);

        reducedMotion.addEventListener("change", function (event) {
            if (event.matches) {
                userPaused = true;
                updateToggleButton();
            }

            restartTimer();
        });

        updateCarousel(false);
        updateToggleButton();
        restartTimer();
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initializeCarousels);
    } else {
        initializeCarousels();
    }
})();
