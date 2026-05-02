(function () {
    "use strict";

    if ("serviceWorker" in navigator) {
        window.addEventListener("load", function () {
            navigator.serviceWorker.register("/service-worker.js").catch(function () {
                // PWA support is progressive; failed registration should not block the site.
            });
        });
    }

    var installPrompt;

    window.addEventListener("beforeinstallprompt", function (event) {
        event.preventDefault();
        installPrompt = event;
        showInstallButton();
    });

    window.addEventListener("appinstalled", function () {
        installPrompt = null;
        var button = document.querySelector("[data-pwa-install]");
        if (button) {
            button.remove();
        }
    });

    document.addEventListener("submit", function (event) {
        var form = event.target;
        if (!form || !form.action || form.action.indexOf("/Account/Logout") === -1) {
            return;
        }

        try {
            // Clear PWA appointment data using explicit guards for ES5 compatibility
            if (window.okaforPwaAppointments && typeof window.okaforPwaAppointments.clear === 'function') {
                window.okaforPwaAppointments.clear();
            }
            
            localStorage.removeItem("okafor.offlineAppointments.v1");
            localStorage.removeItem("okafor.appointmentReminders.v1");
            localStorage.removeItem("okafor.offlineAppointments.v2");
            localStorage.removeItem("okafor.appointmentReminders.v2");
            
            if (window.indexedDB) {
                window.indexedDB.deleteDatabase("okafor-pwa-crypto");
            }
        } catch (err) {
            // Logout should continue even if local storage is unavailable.
        }
    });

    function showInstallButton() {
        if (!installPrompt || document.querySelector("[data-pwa-install]")) {
            return;
        }

        var button = document.createElement("button");
        button.type = "button";
        button.dataset.pwaInstall = "true";
        button.textContent = "Install app";
        button.setAttribute("aria-label", "Install Okafor Hospital app");
        button.className = "pwa-install-button";
        button.addEventListener("click", function () {
            if (!installPrompt) {
                // Install prompt has been nulled, re-enable button
                button.disabled = false;
                return;
            }
            
            button.disabled = true;
            installPrompt.prompt();
            installPrompt.userChoice.finally(function () {
                installPrompt = null;
                button.remove();
            });
        });

        document.body.appendChild(button);
    }
})();
