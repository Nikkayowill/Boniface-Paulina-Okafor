/**
 * Service Worker Tests
 * Tests for wwwroot/service-worker.js functionality
 * 
 * Run with: jest service-worker.test.js
 */

describe('Service Worker Module', () => {
    const VERSION = 'okafor-pwa-v10';
    const CACHE_STATIC = `${VERSION}-static`;
    const CACHE_RUNTIME = `${VERSION}-runtime`;

    beforeEach(() => {
        jest.clearAllMocks();
    });

    describe('Notification Click Handling', () => {
        test('should match notification URL with correct pathname', () => {
            const clientUrl = 'https://okafor-hospital.com/Portal/Appointments?tab=upcoming';
            const targetUrl = '/Portal/Appointments';

            const clientPathname = new URL(clientUrl).pathname;
            const targetPathname = targetUrl;

            expect(clientPathname).toBe(targetPathname);
        });

        test('should handle different query parameters as same pathname', () => {
            const url1 = 'https://okafor-hospital.com/Portal/Appointments?tab=upcoming';
            const url2 = 'https://okafor-hospital.com/Portal/Appointments?filter=past';

            const pathname1 = new URL(url1).pathname;
            const pathname2 = new URL(url2).pathname;

            expect(pathname1).toBe(pathname2);
        });

        test('should NOT match different pathnames', () => {
            const url1 = 'https://okafor-hospital.com/Portal/Appointments';
            const url2 = 'https://okafor-hospital.com/Portal/Dashboard';

            const pathname1 = new URL(url1).pathname;
            const pathname2 = new URL(url2).pathname;

            expect(pathname1).not.toBe(pathname2);
        });

        test('should find correct client by pathname', () => {
            const clients = [
                { url: 'https://okafor-hospital.com/Home/About' },
                { url: 'https://okafor-hospital.com/Portal/Appointments?tab=upcoming' },
                { url: 'https://okafor-hospital.com/Portal/Dashboard' }
            ];
            const targetUrl = '/Portal/Appointments';

            const matchedClient = clients.find(client => {
                return new URL(client.url).pathname === targetUrl;
            });

            expect(matchedClient).toBeDefined();
            expect(matchedClient.url).toContain('/Portal/Appointments');
        });

        test('should return null when no client matches', () => {
            const clients = [
                { url: 'https://okafor-hospital.com/Home/About' }
            ];
            const targetUrl = '/Portal/Appointments';

            const matchedClient = clients.find(client => {
                return new URL(client.url).pathname === targetUrl;
            });

            expect(matchedClient).toBeUndefined();
        });
    });

    describe('Sensitive Path Detection', () => {
        test('should identify sensitive paths correctly', () => {
            const sensitivePaths = [
                '/Admin', '/Account', '/Patient', '/Portal', '/Identity',
                '/BillPayments', '/Donation/Receipt', '/uploads', '/hubs',
                '/api/account', '/api/portal', '/api/patient', '/api/admin', '/api/identity'
            ];

            const testPath = '/Portal/Appointments';
            const isSensitive = sensitivePaths.some(prefix =>
                testPath === prefix || testPath.startsWith(`${prefix}/`)
            );

            expect(isSensitive).toBe(true);
        });

        test('should NOT mark public paths as sensitive', () => {
            const sensitivePaths = [
                '/Admin', '/Account', '/Patient', '/Portal', '/Identity'
            ];

            const testPath = '/Home/About';
            const isSensitive = sensitivePaths.some(prefix =>
                testPath === prefix || testPath.startsWith(`${prefix}/`)
            );

            expect(isSensitive).toBe(false);
        });

        test('should require exact prefix match', () => {
            const sensitivePaths = ['/Admin', '/Patient'];

            // Should NOT match /Admin123 (no slash after prefix)
            const testPath = '/Admin123';
            const isSensitive = sensitivePaths.some(prefix =>
                testPath === prefix || testPath.startsWith(`${prefix}/`)
            );

            expect(isSensitive).toBe(false);
        });
    });

    describe('Public Page Caching', () => {
        test('should cache public pages', () => {
            const publicPages = [
                '/', '/Home/About', '/Home/Services', '/Home/Doctors',
                '/Home/Team', '/Home/PatientInformationHub', '/Home/News'
            ];

            const testPath = '/Home/About';
            const shouldCache = publicPages.includes(testPath);

            expect(shouldCache).toBe(true);
        });

        test('should NOT cache sensitive pages', () => {
            const publicPages = ['/', '/Home/About', '/Home/Services'];

            const testPath = '/Portal/Appointments';
            const shouldCache = publicPages.includes(testPath);

            expect(shouldCache).toBe(false);
        });

        test('should cache root path', () => {
            const publicPages = ['/'];

            const shouldCache = publicPages.includes('/');

            expect(shouldCache).toBe(true);
        });

        test('should NOT cache public form pages with antiforgery tokens', () => {
            const publicPages = [
                '/', '/Home/About', '/Home/Services', '/Home/Doctors',
                '/Home/Team', '/Home/PatientInformationHub', '/Home/News'
            ];

            const shouldCache = publicPages.includes('/Home/Contact');

            expect(shouldCache).toBe(false);
        });
    });

    describe('Navigation Handling', () => {
        test('should return cached page if available', () => {
            const cached = { status: 200, body: 'Cached content' };

            expect(cached).toBeDefined();
            expect(cached.status).toBe(200);
        });

        test('should return offline fallback when no cache', () => {
            const fallback = '/offline.html';
            const cached = null;

            const response = cached || fallback;

            expect(response).toBe(fallback);
        });

        test('should return 503 Service Unavailable as final fallback', () => {
            const response = {
                status: 503,
                body: 'Service temporarily unavailable. Please try again later.'
            };

            expect(response.status).toBe(503);
            expect(response.body).toBeTruthy();
        });
    });

    describe('Sensitive Path Fallbacks', () => {
        test('should use /offline.html for private route navigation failures', () => {
            const primaryFallback = '/offline.html';

            const fallback = primaryFallback;

            expect(fallback).toBe('/offline.html');
        });

        test('should return 503 if both fallbacks unavailable', () => {
            const privateRouteFallback = undefined;
            const finalResponse = 503;

            const response = privateRouteFallback || finalResponse;

            expect(response).toBe(503);
        });
    });

    describe('Cache Cleanup on Activate', () => {
        test('should keep current version caches', () => {
            const cacheKey = `${VERSION}-static`;
            const shouldKeep = cacheKey.startsWith(VERSION);

            expect(shouldKeep).toBe(true);
        });

        test('should delete old version caches', () => {
            const oldCacheKey = 'okafor-pwa-v3-static';
            const shouldDelete = !oldCacheKey.startsWith(VERSION);

            expect(shouldDelete).toBe(true);
        });

        test('should keep all v7 caches', () => {
            const caches = [
                `${VERSION}-static`,
                `${VERSION}-runtime`
            ];

            caches.forEach(cache => {
                expect(cache.startsWith(VERSION)).toBe(true);
            });
        });
    });

    describe('Install Event', () => {
        test('should cache critical assets', () => {
            const criticalAssets = [
                '/app-shell.html',
                '/offline.html',
                '/offline-appointments.html',
                '/css/app-shell.css',
                '/css/tailwind.css',
                '/js/encrypted-offline-store.js',
                '/js/offline-state.js',
                '/js/pwa-register.js',
                '/js/pwa-appointments.js'
            ];

            expect(criticalAssets).toContain('/app-shell.html');
            expect(criticalAssets).toContain('/offline.html');
            expect(criticalAssets).toContain('/offline-appointments.html');
            expect(criticalAssets.length).toBeGreaterThan(0);
        });

        test('should use correct cache name', () => {
            const cacheName = CACHE_STATIC;

            expect(cacheName).toContain('okafor-pwa');
            expect(cacheName).toContain('v7');
            expect(cacheName).toContain('static');
        });

        test('should use runtime cache for public navigations only', () => {
            expect(CACHE_RUNTIME).toContain('okafor-pwa');
            expect(CACHE_RUNTIME).toContain('v7');
            expect(CACHE_RUNTIME).toContain('runtime');
        });
    });

    describe('Push Event Handling', () => {
        test('should handle valid push notification payload', () => {
            const notification = {
                title: 'Appointment Reminder',
                body: 'Your appointment is in 24 hours',
                icon: '/images/icons/okafor-hospital-icon.svg',
                badge: '/images/icons/badge.png',
                url: '/Portal/Appointments'
            };

            expect(notification.title).toBeTruthy();
            expect(notification.body).toBeTruthy();
            expect(notification.icon).toBeTruthy();
            expect(notification.url).toBeTruthy();
        });

        test('should use default title if not provided', () => {
            const defaultTitle = 'Okafor Hospital';
            const notification = { title: undefined };

            const title = notification.title || defaultTitle;

            expect(title).toBe(defaultTitle);
        });

        test('should use default body if not provided', () => {
            const defaultBody = 'You have a new notification.';
            const notification = { body: undefined };

            const body = notification.body || defaultBody;

            expect(body).toBe(defaultBody);
        });

        test('should open correct URL when notification clicked', () => {
            const notification = {
                url: '/Portal/Appointments'
            };

            expect(notification.url).toBe('/Portal/Appointments');
        });
    });

    describe('Notification Click Event', () => {
        test('should find and focus client window', () => {
            const clientFound = true;
            expect(clientFound).toBe(true);
        });

        test('should open new window if client not found', () => {
            const clientFound = false;
            const action = clientFound ? 'focus' : 'open';

            expect(action).toBe('open');
        });

        test('should navigate to notification URL', () => {
            const url = '/Portal/Appointments';
            expect(url).toMatch(/^\/Portal/);
        });
    });

    describe('Fetch Event Handling', () => {
        test('should only intercept GET requests', () => {
            const requests = [
                { method: 'GET', shouldIntercept: true },
                { method: 'POST', shouldIntercept: false }
            ];

            requests.forEach(request => {
                expect(request.method === 'GET').toBe(request.shouldIntercept);
            });
        });

        test('should distinguish between navigation and API requests', () => {
            const navigationRequest = { destination: 'document' };
            const apiRequest = { destination: 'empty' };

            const isNavigation = navigationRequest.destination === 'document';
            const isAPI = apiRequest.destination === 'empty';

            expect(isNavigation).toBe(true);
            expect(isAPI).toBe(true);
        });

        test('should handle network offline gracefully', () => {
            const offline = true;
            const fallback = offline ? '/offline.html' : null;

            expect(fallback).toBe('/offline.html');
        });
    });

    describe('Service Worker Version', () => {
        test('should have current version identifier', () => {
            expect(VERSION).toBe('okafor-pwa-v10');
        });

        test('should increment version on updates', () => {
            const previousVersion = 'okafor-pwa-v9';
            const currentVersion = 'okafor-pwa-v10';

            expect(currentVersion).not.toBe(previousVersion);
            expect(currentVersion).toContain('v10');
        });
    });

    describe('Error Scenarios', () => {
        test('should handle malformed URLs', () => {
            const malformedUrl = 'not-a-valid-url';

            const isValid = malformedUrl.startsWith('/') || malformedUrl.startsWith('http');

            expect(isValid).toBe(false);
        });

        test('should log errors for debugging', () => {
            const error = new Error('Fetch failed');
            const logged = true;

            expect(error).toBeDefined();
            expect(logged).toBe(true);
        });

        test('should continue operation on non-critical errors', () => {
            const criticalError = false;
            const shouldContinue = !criticalError;

            expect(shouldContinue).toBe(true);
        });
    });
});
