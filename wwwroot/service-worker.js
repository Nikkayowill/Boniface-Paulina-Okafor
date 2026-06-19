const VERSION = "okafor-pwa-v8";
const STATIC_CACHE_NAME = `${VERSION}-static`;
const RUNTIME_CACHE_NAME = `${VERSION}-runtime`;
const APP_SHELL_URL = "/app-shell.html";

// Keep this whitelist limited to public shell assets. Regenerate candidates with
// scripts/update-pwa-cache-manifest.ps1, then review before adding them here.
const STATIC_ASSETS = [
    APP_SHELL_URL,
    "/offline.html",
    "/offline-appointments.html",
    "/site.webmanifest",
    "/favicon.ico",
    "/lib/bootstrap/dist/css/bootstrap.min.css",
    "/lib/bootstrap/dist/js/bootstrap.bundle.min.js",
    "/css/app-shell.css",
    "/css/tailwind.css",
    "/css/site.css",
    "/css/public-site.css",
    "/js/navigation.js",
    "/js/site.js",
    "/js/offline-state.js",
    "/js/encrypted-offline-store.js",
    "/js/pwa-register.js",
    "/js/pwa-appointments.js",
    "/js/portal-security.js",
    "/js/push-notifications.js",
    "/images/icons/okafor-hospital-icon.svg",
    "/images/icons/okafor-navbar-logo.svg",
    "/images/icons/okafor-primary-logo.svg",
    "/images/icons/icon-192.png",
    "/images/icons/icon-512.png",
    "/images/icons/maskable-icon-512.png",
    "/images/icons/apple-touch-icon.png",
    "/images/icons/shortcut-emergency-192.png",
    "/images/icons/shortcut-hours-192.png"
];

const PUBLIC_ROUTES = [
    "/",
    "/Home/About",
    "/Home/Services",
    "/Home/Doctors",
    "/Home/Team",
    "/Home/PatientInformationHub",
    "/Home/News",
    "/doctors"
];

const PRIVATE_ROUTE_PREFIXES = [
    "/Admin",
    "/Account",
    "/Patient",
    "/Portal",
    "/Identity",
    "/BillPayments",
    "/Donation/Receipt",
    "/Teleconsultations/Submitted",
    "/AppointmentRequests/Submitted",
    "/uploads",
    "/hubs",
    "/api/account",
    "/api/portal",
    "/api/patient",
    "/api/admin",
    "/api/identity",
    "/api/billing",
    "/api/billpayments",
    "/api/documents",
    "/api/messages"
];

self.addEventListener("install", (event) => {
    event.waitUntil(
        caches.open(STATIC_CACHE_NAME)
            .then((cache) => cache.addAll(STATIC_ASSETS))
            .then(() => self.skipWaiting())
    );
});

self.addEventListener("activate", (event) => {
    event.waitUntil(
        caches.keys()
            .then((keys) => Promise.all(keys
                .filter((key) => !key.startsWith(VERSION))
                .map((key) => caches.delete(key))))
            .then(() => self.clients.claim())
    );
});

self.addEventListener("fetch", (event) => {
    const request = event.request;

    // Never intercept form submissions or API writes. ASP.NET antiforgery
    // tokens must go straight to the network and must never enter Cache Storage.
    if (request.method !== "GET") {
        return;
    }

    const url = new URL(request.url);

    if (url.origin !== self.location.origin) {
        return;
    }

    if (isPrivateRoute(url.pathname)) {
        event.respondWith(handleNetworkOnly(request, url));
        return;
    }

    if (request.mode === "navigate") {
        event.respondWith(handleNavigation(request, url));
        return;
    }

    if (shouldCacheStaticAsset(url.pathname)) {
        event.respondWith(cacheFirstStaticAsset(request));
    }
});

self.addEventListener("push", (event) => {
    const defaults = {
        title: "Okafor Hospital",
        body: "You have a new notification.",
        icon: "/images/icons/okafor-hospital-icon.svg",
        badge: "/images/icons/okafor-hospital-icon.svg",
        url: "/"
    };

    let payload = {};
    if (event.data) {
        try {
            payload = event.data.json();
        } catch {
            payload = { body: event.data.text() };
        }
    }

    const title = payload.title || defaults.title;
    const options = {
        body: payload.body || defaults.body,
        icon: payload.icon || defaults.icon,
        badge: payload.badge || defaults.badge,
        data: {
            url: payload.url || defaults.url
        }
    };

    event.waitUntil(self.registration.showNotification(title, options));
});

self.addEventListener("notificationclick", (event) => {
    event.notification.close();
    const targetUrl = event.notification.data && event.notification.data.url
        ? event.notification.data.url
        : "/";
    event.waitUntil(
        self.clients.matchAll({ type: "window", includeUncontrolled: true }).then((clients) => {
            try {
                // Parse target URL to get pathname for accurate matching
                const targetUrlObj = new URL(targetUrl, self.location.origin);
                const targetPathname = targetUrlObj.pathname;
                
                // Find client with matching pathname
                const existing = clients.find((client) => {
                    try {
                        const clientUrl = new URL(client.url, self.location.origin);
                        return clientUrl.pathname === targetPathname;
                    } catch (err) {
                        return false;
                    }
                });
                
                if (existing) {
                    return existing.focus();
                }
            } catch (err) {
                console.error("Error matching client URL:", err);
            }
            
            return self.clients.openWindow(targetUrl);
        })
    );
});

async function handleNavigation(request, url) {
    try {
        const response = await fetch(request);

        if (response.ok && shouldCachePage(url.pathname) && !hasNoStore(response)) {
            const cache = await caches.open(RUNTIME_CACHE_NAME);
            await cache.put(request, response.clone());
        }

        return response;
    } catch (err) {
        console.error("Fetch failed:", err);
        const cached = await caches.match(request);
        if (cached) {
            return cached;
        }

        if (shouldCachePage(url.pathname)) {
            const appShell = await caches.match(APP_SHELL_URL);
            if (appShell) {
                return appShell;
            }
        }
        
        // Try offline.html fallback
        const offlineFallback = await caches.match("/offline.html");
        if (offlineFallback) {
            return offlineFallback;
        }
        
        // Return a constructed offline response as last resort
        return new Response(
            "<html><body><h1>Offline</h1><p>This page is not available offline.</p></body></html>",
            {
                status: 503,
                statusText: "Service Unavailable",
                headers: { "Content-Type": "text/html" }
            }
        );
    }
}

async function handleNetworkOnly(request, url) {
    try {
        return await networkOnlyFetch(request);
    } catch (err) {
        if (request.destination === "document" || request.mode === "navigate") {
            const offlineFallback = await caches.match("/offline.html");
            if (offlineFallback) {
                return offlineFallback;
            }
        }

        const isApi = url.pathname.toLowerCase().startsWith("/api/");
        return new Response(
            isApi
                ? JSON.stringify({ error: "Network connection required for secure medical data." })
                : "<html><body><h1>Connection required</h1><p>This secure hospital page is not available offline.</p></body></html>",
            {
                status: 503,
                statusText: "Service Unavailable",
                headers: {
                    "Cache-Control": "no-store",
                    "Content-Type": isApi ? "application/json" : "text/html"
                }
            }
        );
    }
}

async function cacheFirstStaticAsset(request) {
    const cached = await caches.match(request, { ignoreSearch: true });
    return cached || fetch(request);
}

function networkOnlyFetch(request) {
    const noStoreRequest = new Request(request, { cache: "no-store" });
    return fetch(noStoreRequest);
}

function shouldCachePage(pathname) {
    return PUBLIC_ROUTES.some((publicPath) =>
        pathname === publicPath || (publicPath !== "/" && pathname.startsWith(`${publicPath}/`)));
}

function shouldCacheStaticAsset(pathname) {
    const normalizedPath = pathname.toLowerCase();
    return STATIC_ASSETS.some((asset) => asset.toLowerCase() === normalizedPath);
}

function isPrivateRoute(pathname) {
    const normalizedPath = pathname.toLowerCase();
    return PRIVATE_ROUTE_PREFIXES.some((prefix) => {
        const normalizedPrefix = prefix.toLowerCase();
        return normalizedPath === normalizedPrefix || normalizedPath.startsWith(`${normalizedPrefix}/`);
    });
}

function hasNoStore(response) {
    return (response.headers.get("Cache-Control") || "").toLowerCase().includes("no-store");
}
