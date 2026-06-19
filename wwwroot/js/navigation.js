(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        initHeaderScrollState();
        initMobileMenu();
    });

    function initHeaderScrollState() {
        var header = document.querySelector("[data-site-header]");

        if (!header) {
            return;
        }

        function updateHeader() {
            header.classList.toggle("site-header--scrolled", window.scrollY > 8);
        }

        updateHeader();
        window.addEventListener("scroll", updateHeader, { passive: true });
    }

    function initMobileMenu() {
        var button = document.getElementById("mobile-menu-toggle");
        var menu = document.getElementById("site-mobile-menu");

        if (!button || !menu) {
            return;
        }

        button.addEventListener("click", function () {
            var isOpen = menu.classList.toggle("is-open");
            button.setAttribute("aria-expanded", isOpen ? "true" : "false");
        });

        menu.addEventListener("click", function (event) {
            if (event.target.closest("a")) {
                closeMenu(button, menu);
            }
        });

        document.addEventListener("keydown", function (event) {
            if (event.key === "Escape") {
                closeMenu(button, menu);
            }
        });

        window.addEventListener("resize", function () {
            if (window.innerWidth >= 1024) {
                closeMenu(button, menu);
            }
        });
    }

    function closeMenu(button, menu) {
        menu.classList.remove("is-open");
        button.setAttribute("aria-expanded", "false");
    }
})();
