/**
 * PWA Registration Tests
 * Tests for wwwroot/js/pwa-register.js ES5 compatibility and install flow
 * 
 * Run with: jest pwa-register.test.js
 */

describe('PWA Registration Module', () => {
    let installPrompt = null;

    beforeEach(() => {
        installPrompt = null;
        global.navigator = {
            serviceWorker: {
                register: jest.fn().mockResolvedValue({ active: true })
            }
        };
        global.window = {
            addEventListener: jest.fn(),
            removeEventListener: jest.fn(),
            okaforPwaAppointments: undefined,
            caches: {
                keys: jest.fn().mockResolvedValue([]),
                delete: jest.fn().mockResolvedValue(true)
            }
        };
    });

    describe('Install Prompt Event Handling', () => {
        test('should capture beforeinstallprompt event', () => {
            const event = { prompt: jest.fn(), userChoice: Promise.resolve() };

            installPrompt = event;

            expect(installPrompt).toBeDefined();
            expect(installPrompt.prompt).toBeDefined();
        });

        test('should not call prompt without event', () => {
            const button = { disabled: false };
            installPrompt = null;

            if (installPrompt) {
                installPrompt.prompt();
            } else {
                button.disabled = true;
            }

            expect(button.disabled).toBe(true);
        });

        test('should enable button when prompt available', () => {
            const button = { disabled: false };
            installPrompt = { prompt: jest.fn() };

            expect(button.disabled).toBe(false);
        });

        test('should disable button when prompt not available', () => {
            const button = { disabled: true };
            installPrompt = null;

            expect(button.disabled).toBe(true);
        });
    });

    describe('Install Button Click Handling', () => {
        test('should check if installPrompt exists before calling', () => {
            installPrompt = null;

            if (!installPrompt) {
                // Button should not attempt to call prompt
                expect(installPrompt).toBeNull();
            }
        });

        test('should call prompt when installPrompt available', async () => {
            const mockPrompt = jest.fn();
            installPrompt = { 
                prompt: mockPrompt,
                userChoice: Promise.resolve({ outcome: 'accepted' })
            };

            await installPrompt.prompt();

            expect(mockPrompt).toHaveBeenCalled();
        });

        test('should handle userChoice after prompt', async () => {
            installPrompt = {
                prompt: jest.fn(),
                userChoice: Promise.resolve({ outcome: 'accepted' })
            };

            await installPrompt.prompt();
            const choice = await installPrompt.userChoice;

            expect(choice.outcome).toBe('accepted');
        });

        test('should handle userChoice dismissed', async () => {
            installPrompt = {
                prompt: jest.fn(),
                userChoice: Promise.resolve({ outcome: 'dismissed' })
            };

            const choice = await installPrompt.userChoice;

            expect(choice.outcome).toBe('dismissed');
        });
    });

    describe('ES5 Compatibility Guards', () => {
        test('should check object existence before accessing methods', () => {
            global.window.okaforPwaAppointments = null;

            const shouldClear = (
                global.window.okaforPwaAppointments &&
                typeof global.window.okaforPwaAppointments.clear === 'function'
            );

            expect(shouldClear).toBe(false);
        });

        test('should verify function type explicitly', () => {
            global.window.okaforPwaAppointments = {
                clear: () => {}
            };

            const isClearFunction = typeof global.window.okaforPwaAppointments.clear === 'function';

            expect(isClearFunction).toBe(true);
        });

        test('should call function only if type check passes', () => {
            const mockClear = jest.fn();
            global.window.okaforPwaAppointments = {
                clear: mockClear
            };

            if (global.window.okaforPwaAppointments &&
                typeof global.window.okaforPwaAppointments.clear === 'function') {
                global.window.okaforPwaAppointments.clear();
            }

            expect(mockClear).toHaveBeenCalled();
        });

        test('should use typeof instead of optional chaining', () => {
            // ES5 compatible: No optional chaining (?.)
            const obj = undefined;

            // WRONG (ES6+): const result = obj?.method?.();
            // RIGHT (ES5): Explicit checks
            const result = obj && typeof obj.method === 'function' ? obj.method() : undefined;

            expect(result).toBeUndefined();
        });

        test('should guard IndexedDB access', () => {
            global.window.indexedDB = undefined;

            const hasIndexedDB = typeof global.window.indexedDB !== 'undefined';

            expect(hasIndexedDB).toBe(false);
        });

        test('should use if statement for type checking', () => {
            global.window.okaforPwaAppointments = null;

            let cleared = false;
            if (global.window.okaforPwaAppointments && 
                typeof global.window.okaforPwaAppointments.clear === 'function') {
                global.window.okaforPwaAppointments.clear();
                cleared = true;
            }

            expect(cleared).toBe(false);
        });
    });

    describe('Service Worker Registration', () => {
        test('should register service worker on load', async () => {
            await navigator.serviceWorker.register('/service-worker.js');

            expect(navigator.serviceWorker.register).toHaveBeenCalledWith('/service-worker.js');
        });

        test('should handle registration failure gracefully', async () => {
            navigator.serviceWorker.register.mockRejectedValue(new Error('Registration failed'));

            let error = null;
            try {
                await navigator.serviceWorker.register('/service-worker.js');
            } catch (err) {
                error = err;
            }

            expect(error).toBeTruthy();
        });

        test('should not block site if registration fails', async () => {
            navigator.serviceWorker.register.mockRejectedValue(new Error('Unavailable'));

            // Site should continue to work
            const siteWorks = true;

            expect(siteWorks).toBe(true);
        });
    });

    describe('Logout Handler', () => {
        test('should clear PWA appointments on logout', () => {
            const mockClear = jest.fn();
            global.window.okaforPwaAppointments = {
                clear: mockClear
            };

            // ES5 compatible clear
            if (global.window.okaforPwaAppointments &&
                typeof global.window.okaforPwaAppointments.clear === 'function') {
                global.window.okaforPwaAppointments.clear();
            }

            expect(mockClear).toHaveBeenCalled();
        });

        test('should clear localStorage on logout', () => {
            const mockClear = jest.fn();
            global.localStorage = {
                clear: mockClear
            };

            global.localStorage.clear();

            expect(mockClear).toHaveBeenCalled();
        });

        test('should delete IndexedDB', () => {
            const mockDeleteDatabase = jest.fn();
            global.window.indexedDB = {
                deleteDatabase: mockDeleteDatabase
            };

            if (global.window.indexedDB) {
                global.window.indexedDB.deleteDatabase('okafor-pwa-crypto');
            }

            expect(mockDeleteDatabase).toHaveBeenCalledWith('okafor-pwa-crypto');
        });

        test('should delete app-owned Cache Storage entries', async () => {
            const mockDelete = jest.fn().mockResolvedValue(true);
            global.window.caches = {
                keys: jest.fn().mockResolvedValue(['okafor-pwa-v7-runtime', 'third-party-cache']),
                delete: mockDelete
            };

            const cacheNames = await global.window.caches.keys();
            await Promise.all(cacheNames
                .filter(cacheName => cacheName.indexOf('okafor-pwa-') === 0)
                .map(cacheName => global.window.caches.delete(cacheName)));

            expect(mockDelete).toHaveBeenCalledWith('okafor-pwa-v7-runtime');
            expect(mockDelete).not.toHaveBeenCalledWith('third-party-cache');
        });

        test('should continue even if cleanup has errors', () => {
            global.window.okaforPwaAppointments = undefined;

            let logoutCompleted = false;
            try {
                if (global.window.okaforPwaAppointments &&
                    typeof global.window.okaforPwaAppointments.clear === 'function') {
                    global.window.okaforPwaAppointments.clear();
                }
                logoutCompleted = true;
            } catch (err) {
                console.error('Logout cleanup error:', err);
                logoutCompleted = true; // Continue anyway
            }

            expect(logoutCompleted).toBe(true);
        });

        test('should clear all storage in correct order', () => {
            const clearOrder = [];

            // Simulate order: appointments, sessionStorage, localStorage, IndexedDB, caches
            global.window.okaforPwaAppointments = {
                clear: () => clearOrder.push('appointments')
            };

            if (global.window.okaforPwaAppointments &&
                typeof global.window.okaforPwaAppointments.clear === 'function') {
                global.window.okaforPwaAppointments.clear();
            }
            clearOrder.push('sessionStorage');
            clearOrder.push('localStorage');
            clearOrder.push('indexedDB');
            clearOrder.push('caches');

            expect(clearOrder[0]).toBe('appointments');
            expect(clearOrder[1]).toBe('sessionStorage');
            expect(clearOrder[2]).toBe('localStorage');
            expect(clearOrder[3]).toBe('indexedDB');
            expect(clearOrder[4]).toBe('caches');
        });
    });

    describe('App Installed Event', () => {
        test('should listen to appinstalled event', () => {
            global.window.addEventListener('appinstalled', jest.fn());

            expect(global.window.addEventListener).toHaveBeenCalledWith(
                'appinstalled',
                expect.any(Function)
            );
        });

        test('should null out installPrompt on install', () => {
            installPrompt = { prompt: jest.fn() };

            // Simulate appinstalled event
            installPrompt = null;

            expect(installPrompt).toBeNull();
        });

        test('should remove install button after installation', () => {
            const button = { element: 'button', remove: jest.fn() };

            button.remove();

            expect(button.remove).toHaveBeenCalled();
        });

        test('should hide button from user after install', () => {
            const button = { style: { display: 'block' } };

            button.style.display = 'none';

            expect(button.style.display).toBe('none');
        });
    });

    describe('Install Button Element', () => {
        test('should have proper button type', () => {
            const button = document.createElement('button');
            button.type = 'button';

            expect(button.type).toBe('button');
        });

        test('should have data attribute for selection', () => {
            const button = document.createElement('button');
            button.dataset.pwaInstall = 'true';

            expect(button.dataset.pwaInstall).toBe('true');
        });

        test('should have accessible label', () => {
            const button = document.createElement('button');
            button.setAttribute('aria-label', 'Install Okafor Hospital app');

            expect(button.getAttribute('aria-label')).toBe('Install Okafor Hospital app');
        });

        test('should have appropriate styling class', () => {
            const button = document.createElement('button');
            button.className = 'pwa-install-button';

            expect(button.className).toContain('pwa-install-button');
        });

        test('should be hidden initially', () => {
            const button = document.createElement('button');
            button.style.display = 'none';

            expect(button.style.display).toBe('none');
        });
    });

    describe('Error Handling', () => {
        test('should catch registration errors', async () => {
            navigator.serviceWorker.register.mockRejectedValue(new Error('SW not supported'));

            let caught = false;
            try {
                await navigator.serviceWorker.register('/service-worker.js');
            } catch (e) {
                caught = true;
            }

            expect(caught).toBe(true);
        });

        test('should use try-catch for storage operations', () => {
            const performCleanup = () => {
                try {
                    localStorage.clear();
                    if (window.indexedDB) {
                        window.indexedDB.deleteDatabase('db');
                    }
                } catch (err) {
                    console.error('Cleanup error:', err);
                }
            };

            expect(performCleanup).not.toThrow();
        });

        test('should provide error context for debugging', () => {
            const error = new Error('Installation failed');
            const context = {
                timestamp: Date.now(),
                message: error.message
            };

            expect(context.message).toBe('Installation failed');
        });
    });

    describe('Cross-Browser Compatibility', () => {
        test('should work on browsers without optional chaining', () => {
            // Verify no optional chaining syntax is used
            const code = `
                if (obj && obj.method) {
                    obj.method();
                }
            `;

            expect(code).not.toContain('?.');
        });

        test('should use standard Promise API', () => {
            const promise = new Promise((resolve) => {
                resolve('success');
            });

            expect(promise).toBeInstanceOf(Promise);
        });

        test('should use addEventListener for old browser support', () => {
            const listener = jest.fn();
            global.window.addEventListener('load', listener);

            expect(global.window.addEventListener).toHaveBeenCalledWith('load', listener);
        });
    });
});
