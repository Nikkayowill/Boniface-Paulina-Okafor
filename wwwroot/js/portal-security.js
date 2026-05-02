(function () {
    "use strict";

    var timeoutMinutes = 15;
    var warningMinutes = 2;
    var timeoutMs = timeoutMinutes * 60 * 1000;
    var warningMs = warningMinutes * 60 * 1000;
    var timeoutTimer;
    var warningTimer;
    var modal;

    document.addEventListener("DOMContentLoaded", function () {
        var logoutForm = document.querySelector("[data-portal-logout-form]");
        if (!logoutForm) {
            return;
        }

        var modalElement = document.getElementById("portalTimeoutModal");
        if (modalElement && window.bootstrap) {
            modal = new bootstrap.Modal(modalElement, {
                backdrop: "static",
                keyboard: false
            });
        }

        bindActivityTracking(logoutForm);
        bindStaySignedIn();
        resetTimers(logoutForm);
    });

    function bindActivityTracking(logoutForm) {
        ["click", "keydown", "mousemove", "scroll", "touchstart"].forEach(function (eventName) {
            window.addEventListener(eventName, function () {
                resetTimers(logoutForm);
            }, { passive: true });
        });

        document.addEventListener("visibilitychange", function () {
            if (!document.hidden) {
                resetTimers(logoutForm);
            }
        });
    }

    function bindStaySignedIn() {
        var button = document.querySelector("[data-portal-stay-signed-in]");
        if (!button) {
            return;
        }

        button.addEventListener("click", function () {
            modal?.hide();
            var logoutForm = document.querySelector("[data-portal-logout-form]");
            if (logoutForm) {
                resetTimers(logoutForm);
            }
        });
    }

    function resetTimers(logoutForm) {
        clearTimeout(warningTimer);
        clearTimeout(timeoutTimer);

        warningTimer = setTimeout(function () {
            modal?.show();
        }, Math.max(timeoutMs - warningMs, 0));

        timeoutTimer = setTimeout(function () {
            clearPwaData();
            logoutForm.submit();
        }, timeoutMs);
    }

    function clearPwaData() {
        try {
            window.okaforPwaAppointments?.clear?.();
            localStorage.removeItem("okafor.offlineAppointments.v1");
            localStorage.removeItem("okafor.appointmentReminders.v1");
            localStorage.removeItem("okafor.offlineAppointments.v2");
            localStorage.removeItem("okafor.appointmentReminders.v2");
            indexedDB?.deleteDatabase?.("okafor-pwa-crypto");
        } catch {
            // Auto-logout should continue even when browser storage is unavailable.
        }
    }
})();
