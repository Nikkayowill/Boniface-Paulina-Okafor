(function () {
    "use strict";

    var timeoutMinutes = 5;
    var warningMinutes = 1;
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
        ["click", "keydown", "mousemove", "pointermove", "touchstart"].forEach(function (eventName) {
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
            logoutAsync(logoutForm);
        }, timeoutMs);
    }

    async function logoutAsync(logoutForm) {
        clearPwaData();

        try {
            var response = await fetch(logoutForm.action, {
                method: logoutForm.method || "POST",
                body: new FormData(logoutForm),
                credentials: "same-origin",
                cache: "no-store",
                headers: {
                    "X-Requested-With": "XMLHttpRequest"
                }
            });

            if (response.ok || response.redirected) {
                window.location.assign(response.url || "/");
                return;
            }
        } catch {
            // Fall back to the normal form post below.
        }

        logoutForm.submit();
    }

    function clearPwaData() {
        try {
            window.okaforEncryptedOfflineStore?.clearAll?.();
            window.okaforPwaAppointments?.clear?.();
            localStorage.removeItem("okafor.offlineAppointments.v1");
            localStorage.removeItem("okafor.appointmentReminders.v1");
            localStorage.removeItem("okafor.offlineAppointments.v2");
            localStorage.removeItem("okafor.appointmentReminders.v2");
            sessionStorage.clear();
            indexedDB?.deleteDatabase?.("okafor-pwa-crypto");
            indexedDB?.deleteDatabase?.("okafor-secure-offline-store");
        } catch {
            // Auto-logout should continue even when browser storage is unavailable.
        }
    }
})();
