(function () {
    "use strict";

    var appointmentsKey = "okafor.offlineAppointments.v2";
    var remindersKey = "okafor.appointmentReminders.v2";
    var legacyAppointmentsKey = "okafor.offlineAppointments.v1";
    var legacyRemindersKey = "okafor.appointmentReminders.v1";
    var cryptoDbName = "okafor-pwa-crypto";
    var cryptoStoreName = "keys";
    var cryptoKeyName = "appointment-storage";
    var reminderTimers = [];

    document.addEventListener("DOMContentLoaded", function () {
        bindOfflineSave();
        bindReminderButtons();
        renderOfflineAppointments();
        scheduleStoredReminders();
    });

    window.okaforPwaAppointments = {
        clear: clearStoredAppointmentData
    };

    function canUseEncryptedStorage() {
        return window.isSecureContext &&
            window.crypto &&
            window.crypto.subtle &&
            window.indexedDB &&
            window.localStorage;
    }

    function bindOfflineSave() {
        var saveButton = document.querySelector("[data-save-offline-appointments]");
        if (!saveButton) {
            return;
        }

        saveButton.addEventListener("click", async function () {
            if (!canUseEncryptedStorage()) {
                showStatus("Offline appointment saving requires a secure browser context.", "warning");
                return;
            }

            var appointments = collectAppointments();
            if (!appointments.length) {
                showStatus("No appointments are available to save offline.", "warning");
                return;
            }

            await writeEncrypted(appointmentsKey, {
                savedAt: new Date().toISOString(),
                appointments: appointments
            });

            removeLegacyPlainStorage();
            showStatus("Encrypted offline appointment summary saved on this device.", "success");
        });
    }

    function bindReminderButtons() {
        document.querySelectorAll(".js-remind").forEach(function (button) {
            button.addEventListener("click", async function () {
                if (!canUseEncryptedStorage()) {
                    showStatus("Reminders require a secure browser context.", "warning");
                    return;
                }

                var appointment = appointmentFromElement(button);
                var when = parseDate(appointment.date);

                if (!when) {
                    showStatus("Unable to set a reminder for this appointment.", "danger");
                    return;
                }

                var reminderAt = new Date(when.getTime() - (24 * 60 * 60 * 1000));
                var now = new Date();
                var isImmediate = false;

                // If reminder time is in the past, set a minimal buffer and mark as immediate
                if (reminderAt < now) {
                    reminderAt = new Date(now.getTime() + 1000); // 1 second from now
                    isImmediate = true;
                }

                var reminder = {
                    id: appointment.id || String(when.getTime()),
                    title: "Appointment Reminder",
                    body: appointment.subject + " is coming up soon.",
                    appointmentAt: when.toISOString(),
                    reminderAt: reminderAt.toISOString()
                };

                if ("Notification" in window && Notification.permission === "default") {
                    var permissionResult = await Notification.requestPermission();
                    if (permissionResult === "denied") {
                        showStatus("Notifications denied — reminder saved but will not trigger.", "warning");
                        await upsertReminder(reminder);
                        return;
                    }
                }

                await upsertReminder(reminder);
                await scheduleStoredReminders();
                var message = isImmediate ? "Reminder will notify now." : "Encrypted reminder saved for " + reminderAt.toLocaleString() + ".";
                showStatus(message, "success");
            });
        });
    }

    function collectAppointments() {
        var map = {};

        document.querySelectorAll("[data-appointment-offline-item]").forEach(function (item) {
            var appointment = appointmentFromElement(item);
            if (!appointment.id || !appointment.date) {
                return;
            }

            map[appointment.id] = appointment;
        });

        return Object.keys(map)
            .map(function (key) { return map[key]; })
            .sort(function (a, b) { return new Date(a.date) - new Date(b.date); });
    }

    function appointmentFromElement(element) {
        var source = element.closest("[data-appointment-offline-item]") || element;
        return {
            id: readData(source, "appointmentId"),
            subject: readData(source, "subject") || "Hospital appointment",
            date: readData(source, "date"),
            department: readData(source, "department") || "General",
            doctor: readData(source, "doctor") || "Unassigned",
            status: readData(source, "status") || "Scheduled",
            type: readData(source, "type") || "Appointment"
        };
    }

    function readData(element, name) {
        return element && element.dataset ? element.dataset[name] || "" : "";
    }

    async function renderOfflineAppointments() {
        var list = document.querySelector("[data-offline-appointments-list]");
        if (!list) {
            return;
        }

        var saved = await readSavedAppointments();
        var empty = document.querySelector("[data-offline-appointments-empty]");
        list.innerHTML = "";

        if (!saved.appointments.length) {
            if (empty) {
                empty.classList.remove("d-none");
            }
            return;
        }

        if (empty) {
            empty.classList.add("d-none");
        }

        saved.appointments.forEach(function (appointment) {
            var card = document.createElement("article");
            card.className = "card shadow-sm border-0";
            card.innerHTML =
                '<div class="card-body">' +
                '<div class="d-flex justify-content-between gap-3">' +
                '<div class="min-w-0">' +
                '<h2 class="h6 mb-1 text-truncate"></h2>' +
                '<p class="small text-muted mb-0"></p>' +
                '</div>' +
                '<span class="badge bg-light text-dark border align-self-start"></span>' +
                '</div>' +
                '<dl class="row small mt-3 mb-0 g-2">' +
                '<dt class="col-5 text-muted">Department</dt><dd class="col-7 text-end mb-0" data-field="department"></dd>' +
                '<dt class="col-5 text-muted">Doctor</dt><dd class="col-7 text-end mb-0" data-field="doctor"></dd>' +
                '<dt class="col-5 text-muted">Type</dt><dd class="col-7 text-end mb-0" data-field="type"></dd>' +
                '</dl>' +
                '</div>';

            card.querySelector("h2").textContent = appointment.subject;
            card.querySelector("p").textContent = formatDate(appointment.date);
            card.querySelector(".badge").textContent = appointment.status;
            card.querySelector('[data-field="department"]').textContent = appointment.department;
            card.querySelector('[data-field="doctor"]').textContent = appointment.doctor;
            card.querySelector('[data-field="type"]').textContent = appointment.type;
            list.appendChild(card);
        });
    }

    async function readSavedAppointments() {
        var parsed = await readEncrypted(appointmentsKey);
        return {
            savedAt: parsed?.savedAt || "",
            appointments: Array.isArray(parsed?.appointments) ? parsed.appointments : []
        };
    }

    async function upsertReminder(reminder) {
        var reminders = (await readReminders()).filter(function (item) {
            return item.id !== reminder.id;
        });
        reminders.push(reminder);
        await writeEncrypted(remindersKey, reminders);
    }

    async function readReminders() {
        var parsed = await readEncrypted(remindersKey);
        return Array.isArray(parsed) ? parsed : [];
    }

    var MAX_TIMEOUT = 2147483647; // 2^31 - 1 milliseconds

    async function scheduleStoredReminders() {
        reminderTimers.forEach(clearTimeout);
        reminderTimers = [];

        var now = Date.now();
        var pending = (await readReminders()).filter(function (reminder) {
            var appointmentAt = parseDate(reminder.appointmentAt);
            return appointmentAt && appointmentAt.getTime() > now;
        });

        if (pending.length) {
            await writeEncrypted(remindersKey, pending);
        } else {
            localStorage.removeItem(remindersKey);
        }

        pending.forEach(function (reminder) {
            var reminderAt = parseDate(reminder.reminderAt);
            if (!reminderAt) {
                return;
            }

            var delay = reminderAt.getTime() - Date.now();
            if (delay <= 0) {
                notify(reminder);
                return;
            }

            // Handle setTimeout overflow by chaining timeouts for very large delays
            if (delay > MAX_TIMEOUT) {
                var timerId = setTimeout(function () {
                    // Recalculate and reschedule
                    scheduleStoredReminders();
                }, MAX_TIMEOUT);
                reminderTimers.push(timerId);
            } else {
                reminderTimers.push(setTimeout(function () {
                    notify(reminder);
                }, delay));
            }
        });
    }

    async function notify(reminder) {
        if (!("Notification" in window) || Notification.permission !== "granted") {
            return;
        }

        if ("serviceWorker" in navigator) {
            var registration = await navigator.serviceWorker.ready;
            registration.showNotification(reminder.title, {
                body: reminder.body,
                tag: "okafor-appointment-" + reminder.id,
                data: { url: "/Portal/Appointments" }
            });
            return;
        }

        new Notification(reminder.title, { body: reminder.body });
    }

    async function writeEncrypted(storageKey, value) {
        var key = await getCryptoKey();
        var iv = window.crypto.getRandomValues(new Uint8Array(12));
        var encoded = new TextEncoder().encode(JSON.stringify(value));
        var cipherBytes = await window.crypto.subtle.encrypt({ name: "AES-GCM", iv: iv }, key, encoded);

        localStorage.setItem(storageKey, JSON.stringify({
            version: 1,
            algorithm: "AES-GCM",
            iv: bytesToBase64(iv),
            cipherText: bytesToBase64(new Uint8Array(cipherBytes))
        }));
    }

    async function readEncrypted(storageKey) {
        if (!canUseEncryptedStorage()) {
            return null;
        }

        var raw = localStorage.getItem(storageKey);
        if (!raw) {
            return null;
        }

        try {
            var envelope = JSON.parse(raw);
            var key = await getCryptoKey();
            var plainBytes = await window.crypto.subtle.decrypt(
                { name: "AES-GCM", iv: base64ToBytes(envelope.iv) },
                key,
                base64ToBytes(envelope.cipherText)
            );

            return JSON.parse(new TextDecoder().decode(plainBytes));
        } catch {
            return null;
        }
    }

    async function getCryptoKey() {
        var db = await openCryptoDb();
        var existing = await idbGet(db, cryptoKeyName);
        if (existing) {
            db.close();
            return existing;
        }

        var key = await window.crypto.subtle.generateKey(
            { name: "AES-GCM", length: 256 },
            false,
            ["encrypt", "decrypt"]
        );
        await idbPut(db, key, cryptoKeyName);
        db.close();
        return key;
    }

    function openCryptoDb() {
        return new Promise(function (resolve, reject) {
            var request = indexedDB.open(cryptoDbName, 1);
            request.onupgradeneeded = function () {
                request.result.createObjectStore(cryptoStoreName);
            };
            request.onsuccess = function () {
                resolve(request.result);
            };
            request.onerror = function () {
                reject(request.error);
            };
        });
    }

    function idbGet(db, key) {
        return new Promise(function (resolve, reject) {
            var transaction = db.transaction(cryptoStoreName, "readonly");
            var request = transaction.objectStore(cryptoStoreName).get(key);
            request.onsuccess = function () { resolve(request.result); };
            request.onerror = function () { reject(request.error); };
        });
    }

    function idbPut(db, value, key) {
        return new Promise(function (resolve, reject) {
            var transaction = db.transaction(cryptoStoreName, "readwrite");
            transaction.objectStore(cryptoStoreName).put(value, key);
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

    function showStatus(message, type) {
        var status = document.querySelector("[data-appointment-pwa-status]");
        if (!status) {
            return;
        }

        status.className = "alert small mt-3 alert-" + (type || "info");
        status.textContent = message;
        status.classList.remove("d-none");
    }

    function parseDate(value) {
        if (!value) {
            return null;
        }

        var parsed = new Date(value);
        return Number.isNaN(parsed.getTime()) ? null : parsed;
    }

    function formatDate(value) {
        var date = parseDate(value);
        return date ? date.toLocaleString() : value;
    }

    function removeLegacyPlainStorage() {
        localStorage.removeItem(legacyAppointmentsKey);
        localStorage.removeItem(legacyRemindersKey);
    }

    function clearStoredAppointmentData() {
        // Clear scheduled timers first
        reminderTimers.forEach(clearTimeout);
        reminderTimers = [];
        
        // Then remove localStorage entries
        localStorage.removeItem(appointmentsKey);
        localStorage.removeItem(remindersKey);
        removeLegacyPlainStorage();
        
        // Finally, delete IndexedDB
        if (window.indexedDB) {
            indexedDB.deleteDatabase(cryptoDbName);
        }
    }
})();
