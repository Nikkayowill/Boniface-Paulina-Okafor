const VERSION = "okafor-pwa-v4";
const STATIC_CACHE = `${VERSION}-static`;
const PAGE_CACHE = `${VERSION}-pages`;

const STATIC_ASSETS = [
    "/",
    "/offline.html",
    "/offline-appointments.html",
    "/site.webmanifest",
    "/favicon.ico",
    "/lib/bootstrap/dist/css/bootstrap.min.css",
    "/css/tailwind.css",
    "/css/site.css",
    "/js/site.js",
    "/js/pwa-register.js",
    "/js/pwa-appointments.js",
    "/js/portal-security.js",
    "/js/push-notifications.js",
    "/images/icons/okafor-hospital-icon.svg"
];

const PUBLIC_PAGE_PATHS = [
    "/",
    "/Home/About",
    "/Home/Services",
    "/Home/Doctors",
    "/Home/Team",
    "/Home/PatientInformationHub",
    "/Home/News",
    "/Home/Contact",
    "/doctors"
];

const SENSITIVE_PATH_PREFIXES = [
    "/Admin",
    "/Patient",
    "/Portal",
    "/Identity",
    "/BillPayments",
    "/Donation/Receipt",
    "/Teleconsultations/Submitted",
    "/AppointmentRequests/Submitted",
    "/uploads",
    "/hubs"
];

self.addEventListener("install", (event) => {
    event.waitUntil(
        caches.open(STATIC_CACHE)
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

    if (request.method !== "GET") {
        return;
    }

    const url = new URL(request.url);

    if (url.origin !== self.location.origin || isSensitivePath(url.pathname)) {
        if (request.mode === "navigate" && url.pathname === "/Portal/Appointments") {
            event.respondWith(handleSensitiveAppointmentNavigation(request));
        }
        return;
    }

    if (request.mode === "navigate") {
        event.respondWith(handleNavigation(request, url));
        return;
    }

    event.respondWith(
        caches.match(request).then((cached) => cached || fetch(request))
    );
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
    const targetUrl = event.notification.data?.url || "/";
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
            const cache = await caches.open(PAGE_CACHE);
            await cache.put(request, response.clone());
        }

        return response;
    } catch (err) {
        console.error("Fetch failed:", err);
        const cached = await caches.match(request);
        if (cached) {
            return cached;
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

async function handleSensitiveAppointmentNavigation(request) {
    try {
        return await fetch(request);
    } catch (err) {
        console.error("Fetch failed for sensitive navigation:", err);
        const cached = await caches.match("/offline-appointments.html");
        if (cached) {
            return cached;
        }
        
        // Return fallback offline-appointments page
        const fallback = await caches.match("/offline.html");
        if (fallback) {
            return fallback;
        }
        
        // Final fallback: constructed response
        return new Response(
            "<html><body><h1>Offline</h1><p>Appointment information is not available offline. Please reconnect to access your portal.</p></body></html>",
            {
                status: 503,
                statusText: "Service Unavailable",
                headers: { "Content-Type": "text/html" }
            }
        );
    }
}

function shouldCachePage(pathname) {
    return PUBLIC_PAGE_PATHS.some((publicPath) =>
        pathname === publicPath || (publicPath !== "/" && pathname.startsWith(`${publicPath}/`)));
}

function isSensitivePath(pathname) {
    return SENSITIVE_PATH_PREFIXES.some((prefix) =>
        pathname === prefix || pathname.startsWith(`${prefix}/`));
}

function hasNoStore(response) {
    return (response.headers.get("Cache-Control") || "").toLowerCase().includes("no-store");
}
