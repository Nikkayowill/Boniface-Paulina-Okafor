(function () {
    "use strict";

    var bannerId = "okafor-offline-banner";
    var bannerText = "Operating Offline: Secure patient features are temporarily locked until your connection returns.";
    var networkFormSelector = "form.requires-network";
    var networkContainerSelector = "[data-requires-network]";
    var liveFeatureSelector = [
        "[data-live-feature]",
        networkContainerSelector,
        'a[href*="/AppointmentRequests/Create"]',
        'a[href*="/Teleconsultations/Create"]',
        'a[href*="/BillPayments"]',
        networkFormSelector,
        'form[action*="/AppointmentRequests"] button',
        'form[action*="/Teleconsultations"] button',
        'form[action*="/BillPayments"] button'
    ].join(",");
    var loginSelector = [
        "[data-offline-login-warning]",
        'a[href*="/Identity/Account/Login"]'
    ].join(",");

    document.addEventListener("DOMContentLoaded", function () {
        ensureBanner();
        syncOfflineState();

        window.addEventListener("online", syncOfflineState);
        window.addEventListener("offline", syncOfflineState);

        document.addEventListener("click", function (event) {
            var disabledTarget = event.target.closest(".is-offline-disabled, .is-offline-login-warning");
            if (!disabledTarget || navigator.onLine) {
                return;
            }

            event.preventDefault();
            event.stopPropagation();
            ensureBanner().focus({ preventScroll: false });
        }, true);
    });

    function syncOfflineState() {
        var isOffline = !navigator.onLine;
        document.documentElement.classList.toggle("is-offline", isOffline);
        syncBanner(isOffline);
        syncLiveFeatures(isOffline);
        syncNetworkForms(isOffline);
        syncNetworkContainers(isOffline);
        syncLoginLinks(isOffline);
    }

    function syncBanner(isOffline) {
        var banner = ensureBanner();
        banner.hidden = !isOffline;
    }

    function syncLiveFeatures(isOffline) {
        document.querySelectorAll(liveFeatureSelector).forEach(function (element) {
            if (element.matches(loginSelector)) {
                return;
            }

            if (!element.dataset.onlineLabel) {
                element.dataset.onlineLabel = element.textContent.trim();
            }

            element.classList.toggle("is-offline-disabled", isOffline);
            element.setAttribute("aria-disabled", isOffline ? "true" : "false");
            element.title = isOffline ? "This feature needs an internet connection." : "";

            if (element.tagName === "BUTTON" || element.tagName === "INPUT") {
                element.disabled = isOffline;
            } else if (isOffline) {
                element.setAttribute("tabindex", "-1");
            } else {
                element.removeAttribute("tabindex");
            }
        });
    }

    function syncNetworkForms(isOffline) {
        document.querySelectorAll(networkFormSelector).forEach(function (form) {
            form.classList.toggle("is-offline-disabled", isOffline);
            form.setAttribute("aria-disabled", isOffline ? "true" : "false");

            if (isOffline) {
                form.setAttribute("disabled", "disabled");
            } else {
                form.removeAttribute("disabled");
            }

            setControlsDisabled(form, isOffline);
        });
    }

    function syncNetworkContainers(isOffline) {
        document.querySelectorAll(networkContainerSelector).forEach(function (container) {
            container.classList.toggle("is-offline-disabled", isOffline);
            container.setAttribute("aria-disabled", isOffline ? "true" : "false");
            setControlsDisabled(container, isOffline);
        });
    }

    function setControlsDisabled(container, isOffline) {
        container.querySelectorAll("button,input,select,textarea").forEach(function (control) {
            if (control.type === "hidden") {
                return;
            }

            if (isOffline) {
                if (control.disabled) {
                    control.dataset.wasDisabledBeforeOffline = "true";
                } else {
                    control.disabled = true;
                }
                return;
            }

            if (control.dataset.wasDisabledBeforeOffline === "true") {
                delete control.dataset.wasDisabledBeforeOffline;
                return;
            }

            control.disabled = false;
        });
    }

    function syncLoginLinks(isOffline) {
        document.querySelectorAll(loginSelector).forEach(function (link) {
            if (!link.dataset.onlineLabel) {
                link.dataset.onlineLabel = link.textContent.trim();
            }

            link.classList.toggle("is-offline-login-warning", isOffline);
            link.setAttribute("aria-disabled", isOffline ? "true" : "false");
            link.textContent = isOffline ? "Login requires internet" : link.dataset.onlineLabel;
            link.title = isOffline ? "Sign in is unavailable while this device is offline." : "";

            if (isOffline) {
                link.setAttribute("tabindex", "-1");
            } else {
                link.removeAttribute("tabindex");
            }
        });
    }

    function ensureBanner() {
        var existing = document.getElementById(bannerId);
        if (existing) {
            return existing;
        }

        var banner = document.createElement("div");
        banner.id = bannerId;
        banner.className = "offline-status-banner";
        banner.hidden = true;
        banner.tabIndex = -1;
        banner.setAttribute("role", "status");
        banner.setAttribute("aria-live", "polite");
        banner.textContent = bannerText;
        document.body.prepend(banner);
        return banner;
    }
})();
