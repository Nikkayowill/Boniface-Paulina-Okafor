(function () {
    "use strict";

    var dbName = "okafor-secure-offline-store";
    var dbVersion = 1;
    var recordsStore = "records";
    var sessionSeedKey = "okafor.pwaSessionSeed.v1";
    var legacyStorageKeys = [
        "okafor.offlineAppointments.v1",
        "okafor.appointmentReminders.v1",
        "okafor.offlineAppointments.v2",
        "okafor.appointmentReminders.v2"
    ];

    window.okaforEncryptedOfflineStore = {
        clearAll: clearAll,
        isAvailable: isAvailable,
        readJson: readJson,
        remove: remove,
        writeJson: writeJson
    };

    function isAvailable() {
        return Boolean(
            window.isSecureContext &&
            window.crypto &&
            window.crypto.subtle &&
            window.indexedDB &&
            window.TextEncoder &&
            window.TextDecoder &&
            window.sessionStorage
        );
    }

    async function writeJson(key, value) {
        if (!isAvailable()) {
            throw new Error("Encrypted offline storage is unavailable.");
        }

        var salt = window.crypto.getRandomValues(new Uint8Array(16));
        var iv = window.crypto.getRandomValues(new Uint8Array(12));
        var cryptoKey = await deriveKey(salt);
        var encoded = new TextEncoder().encode(JSON.stringify(value));
        var cipherBytes = await window.crypto.subtle.encrypt(
            { name: "AES-GCM", iv: iv },
            cryptoKey,
            encoded
        );

        var db = await openDb();
        await idbPut(db, {
            key: key,
            version: 1,
            algorithm: "AES-GCM",
            kdf: "PBKDF2-SHA-256",
            savedAt: new Date().toISOString(),
            salt: bytesToBase64(salt),
            iv: bytesToBase64(iv),
            cipherText: bytesToBase64(new Uint8Array(cipherBytes))
        });
        db.close();
        clearLegacyStorage();
    }

    async function readJson(key) {
        if (!isAvailable()) {
            return null;
        }

        try {
            var db = await openDb();
            var envelope = await idbGet(db, key);
            db.close();

            if (!envelope) {
                return null;
            }

            var salt = base64ToBytes(envelope.salt);
            var iv = base64ToBytes(envelope.iv);
            var cipherText = base64ToBytes(envelope.cipherText);
            var cryptoKey = await deriveKey(salt);
            var plainBytes = await window.crypto.subtle.decrypt(
                { name: "AES-GCM", iv: iv },
                cryptoKey,
                cipherText
            );

            return JSON.parse(new TextDecoder().decode(plainBytes));
        } catch {
            return null;
        }
    }

    async function remove(key) {
        if (!window.indexedDB) {
            return;
        }

        try {
            var db = await openDb();
            await idbDelete(db, key);
            db.close();
        } catch {
            // Clearing secure offline records should never block logout or page load.
        }
    }

    function clearAll() {
        clearLegacyStorage();
        clearSessionSeed();

        if (window.indexedDB) {
            window.indexedDB.deleteDatabase(dbName);
            window.indexedDB.deleteDatabase("okafor-pwa-crypto");
        }
    }

    async function deriveKey(salt) {
        var sessionIdentifier = getSessionIdentifier();
        var keyMaterial = await window.crypto.subtle.importKey(
            "raw",
            new TextEncoder().encode(sessionIdentifier),
            "PBKDF2",
            false,
            ["deriveKey"]
        );

        return window.crypto.subtle.deriveKey(
            {
                name: "PBKDF2",
                hash: "SHA-256",
                salt: salt,
                iterations: 120000
            },
            keyMaterial,
            { name: "AES-GCM", length: 256 },
            false,
            ["encrypt", "decrypt"]
        );
    }

    function getSessionIdentifier() {
        var serverSessionId = readServerSessionId();
        var browserSessionSeed = sessionStorage.getItem(sessionSeedKey);

        if (!browserSessionSeed) {
            browserSessionSeed = createSessionSeed();
            sessionStorage.setItem(sessionSeedKey, browserSessionSeed);
        }

        return [serverSessionId, browserSessionSeed].filter(Boolean).join(":");
    }

    function readServerSessionId() {
        var meta = document.querySelector('meta[name="okafor-session-id"]');
        if (meta && meta.content) {
            return meta.content;
        }

        var element = document.querySelector("[data-okafor-session-id]");
        return element ? element.getAttribute("data-okafor-session-id") || "" : "";
    }

    function createSessionSeed() {
        if (window.crypto.randomUUID) {
            return window.crypto.randomUUID();
        }

        var bytes = window.crypto.getRandomValues(new Uint8Array(16));
        return bytesToBase64(bytes);
    }

    function clearSessionSeed() {
        try {
            sessionStorage.removeItem(sessionSeedKey);
        } catch {
            // Storage may be unavailable in private or locked-down browser modes.
        }
    }

    function openDb() {
        return new Promise(function (resolve, reject) {
            var request = indexedDB.open(dbName, dbVersion);
            request.onupgradeneeded = function () {
                var db = request.result;
                if (!db.objectStoreNames.contains(recordsStore)) {
                    db.createObjectStore(recordsStore, { keyPath: "key" });
                }
            };
            request.onsuccess = function () {
                resolve(request.result);
            };
            request.onerror = function () {
                reject(request.error);
            };
        });
    }

    function idbPut(db, value) {
        return new Promise(function (resolve, reject) {
            var transaction = db.transaction(recordsStore, "readwrite");
            transaction.objectStore(recordsStore).put(value);
            transaction.oncomplete = resolve;
            transaction.onerror = function () { reject(transaction.error); };
        });
    }

    function idbGet(db, key) {
        return new Promise(function (resolve, reject) {
            var transaction = db.transaction(recordsStore, "readonly");
            var request = transaction.objectStore(recordsStore).get(key);
            request.onsuccess = function () { resolve(request.result); };
            request.onerror = function () { reject(request.error); };
        });
    }

    function idbDelete(db, key) {
        return new Promise(function (resolve, reject) {
            var transaction = db.transaction(recordsStore, "readwrite");
            transaction.objectStore(recordsStore).delete(key);
            transaction.oncomplete = resolve;
            transaction.onerror = function () { reject(transaction.error); };
        });
    }

    function bytesToBase64(bytes) {
        var binary = "";
        bytes.forEach(function (byte) {
            binary += String.fromCharCode(byte);
        });
        return btoa(binary);
    }

    function base64ToBytes(value) {
        var binary = atob(value);
        var bytes = new Uint8Array(binary.length);
        for (var index = 0; index < binary.length; index += 1) {
            bytes[index] = binary.charCodeAt(index);
        }
        return bytes;
    }

    function clearLegacyStorage() {
        try {
            legacyStorageKeys.forEach(function (key) {
                localStorage.removeItem(key);
            });
        } catch {
            // Logout and secure storage writes should continue if localStorage is blocked.
        }
    }
})();
