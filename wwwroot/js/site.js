// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Progressive enhancement for public-facing motion and micro-interactions.
(function () {
    var prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    function initReveals() {
        var revealItems = document.querySelectorAll('.reveal, .scale-reveal');
        if (!revealItems.length) {
            return;
        }

        if (prefersReducedMotion || !('IntersectionObserver' in window)) {
            revealItems.forEach(function (item) { item.classList.add('is-visible'); });
            return;
        }

        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    entry.target.classList.add('is-visible');
                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.16, rootMargin: '0px 0px -8% 0px' });

        revealItems.forEach(function (item, index) {
            if (!item.style.getPropertyValue('--reveal-delay')) {
                item.style.setProperty('--reveal-delay', Math.min(index % 6, 5) * 70 + 'ms');
            }
            observer.observe(item);
        });
    }

    function initCounters() {
        var counters = document.querySelectorAll('[data-count-to]');
        if (!counters.length) {
            return;
        }

        function animateCounter(el) {
            var target = Number(el.getAttribute('data-count-to') || 0);
            var suffix = el.getAttribute('data-count-suffix') || '';
            var duration = Number(el.getAttribute('data-count-duration') || 1300);
            var startTime = null;

            function step(timestamp) {
                if (!startTime) {
                    startTime = timestamp;
                }
                var progress = Math.min((timestamp - startTime) / duration, 1);
                var eased = 1 - Math.pow(1 - progress, 3);
                var value = Math.round(target * eased);
                el.textContent = value.toLocaleString() + suffix;
                if (progress < 1) {
                    window.requestAnimationFrame(step);
                }
            }

            window.requestAnimationFrame(step);
        }

        if (prefersReducedMotion || !('IntersectionObserver' in window)) {
            counters.forEach(animateCounter);
            return;
        }

        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    animateCounter(entry.target);
                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.4 });

        counters.forEach(function (counter) { observer.observe(counter); });
    }

    function initActiveNav() {
        var currentPath = window.location.pathname.replace(/\/$/, '').toLowerCase() || '/';
        document.querySelectorAll('.nav-link').forEach(function (link) {
            var linkPath = new URL(link.href, window.location.origin).pathname.replace(/\/$/, '').toLowerCase() || '/';
            if (linkPath === currentPath) {
                link.classList.add('is-active');
            }
        });
    }

    function initFormFeedback() {
        var fields = document.querySelectorAll('input, select, textarea');
        fields.forEach(function (field) {
            function sync() {
                field.classList.toggle('field-is-filled', Boolean(field.value));
            }
            field.addEventListener('input', sync);
            field.addEventListener('change', sync);
            sync();
        });

        document.querySelectorAll('form[data-enhanced-form="true"]').forEach(function (form) {
            form.addEventListener('submit', function () {
                form.classList.add('is-submitting');
                form.querySelectorAll('button[type="submit"]').forEach(function (button) {
                    button.classList.add('is-submitting');
                    if (!button.dataset.originalText) {
                        button.dataset.originalText = button.textContent.trim();
                    }
                    button.textContent = button.getAttribute('data-loading-text') || 'Submitting...';
                });
            });
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        initReveals();
        initCounters();
        initActiveNav();
        initFormFeedback();
    });
})();
