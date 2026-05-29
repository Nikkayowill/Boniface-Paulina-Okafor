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

    window.okaforPwaCleanup = window.okaforPwaCleanup || {};
    window.okaforPwaCleanup.clearLocalAppData = clearLocalAppData;

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

        clearLocalAppData();
    });

    document.addEventListener("DOMContentLoaded", function () {
        if (document.querySelector("[data-logout-complete]")) {
            clearLocalAppData();
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

    function clearLocalAppData() {
        try {
            if (window.okaforEncryptedOfflineStore && typeof window.okaforEncryptedOfflineStore.clearAll === "function") {
                window.okaforEncryptedOfflineStore.clearAll();
            }

            if (window.okaforPwaAppointments && typeof window.okaforPwaAppointments.clear === "function") {
                window.okaforPwaAppointments.clear();
            }

            if (window.sessionStorage) {
                window.sessionStorage.clear();
            }

            if (window.localStorage) {
                window.localStorage.clear();
            }

            if (window.indexedDB) {
                window.indexedDB.deleteDatabase("okafor-pwa-crypto");
                window.indexedDB.deleteDatabase("okafor-secure-offline-store");
            }
        } catch (err) {
            // Logout should continue even if local storage is unavailable.
        }

        return clearAppCaches();
    }

    function clearAppCaches() {
        if (!window.caches || typeof window.caches.keys !== "function") {
            return Promise.resolve();
        }

        return window.caches.keys()
            .then(function (cacheNames) {
                return Promise.all(cacheNames
                    .filter(isOkaforCache)
                    .map(function (cacheName) {
                        return window.caches.delete(cacheName);
                    }));
            })
            .catch(function () {
                // Cache cleanup is best effort and must not block logout.
            });
    }

    function isOkaforCache(cacheName) {
        return cacheName.indexOf("okafor-pwa-") === 0;
    }
})();
