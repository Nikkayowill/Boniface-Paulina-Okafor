(function () {
    "use strict";

    var cleanupTasks = [];

    if ("serviceWorker" in navigator && typeof navigator.serviceWorker.getRegistrations === "function") {
        cleanupTasks.push(
            navigator.serviceWorker.getRegistrations().then(function (registrations) {
                return Promise.all(registrations.map(function (registration) {
                    return registration.unregister();
                }));
            })
        );
    }

    if (window.caches && typeof window.caches.keys === "function") {
        cleanupTasks.push(
            window.caches.keys().then(function (cacheNames) {
                return Promise.all(cacheNames
                    .filter(function (cacheName) {
                        return cacheName.indexOf("okafor-pwa-") === 0;
                    })
                    .map(function (cacheName) {
                        return window.caches.delete(cacheName);
                    }));
            })
        );
    }

    if (!cleanupTasks.length) {
        return;
    }

    Promise.all(cleanupTasks).then(function (results) {
        var changed = results.some(function (group) {
            return group.some(function (result) { return result === true; });
        });
        var reloadKey = "okafor-pwa-development-reset";

        if (changed && window.sessionStorage && !window.sessionStorage.getItem(reloadKey)) {
            window.sessionStorage.setItem(reloadKey, "1");
            window.location.reload();
        }
    }).catch(function () {
        // Development cleanup is best effort and must not block the page.
    });
})();
