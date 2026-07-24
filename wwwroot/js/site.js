// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Progressive enhancement for public navigation and form feedback.
(function () {
    function initActiveNav() {
        var currentPath = window.location.pathname.replace(/\/$/, '').toLowerCase() || '/';
        document.querySelectorAll('.nav-link').forEach(function (link) {
            var linkPath = new URL(link.href, window.location.origin).pathname.replace(/\/$/, '').toLowerCase() || '/';
            var isSectionPage = linkPath !== '/' && currentPath.startsWith(linkPath + '/');
            if (linkPath === currentPath || isSectionPage) {
                link.classList.add('is-active');
                link.setAttribute('aria-current', 'page');
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
        initActiveNav();
        initFormFeedback();
    });
})();
