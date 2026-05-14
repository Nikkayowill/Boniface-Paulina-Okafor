(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        var root = document.querySelector("[data-push-notifications]");
        if (!root) {
            return;
        }

        var enableButton = root.querySelector("[data-push-enable]");
        var unsubscribeButton = root.querySelector("[data-push-unsubscribe]");
        var testButton = root.querySelector("[data-push-test]");
        var status = root.querySelector("[data-push-status]");
        var publicKey = root.getAttribute("data-vapid-public-key") || "";
        var saveUrl = root.getAttribute("data-save-url") || "/PushNotifications/SaveSubscription";
        var unsubscribeUrl = root.getAttribute("data-unsubscribe-url") || "/PushNotifications/Unsubscribe";
        var testUrl = root.getAttribute("data-test-url") || "/PushNotifications/SendTestNotification";

        if (!isSupported()) {
            setStatus("This browser does not support web push notifications.", "warning");
            setDisabled(true);
            return;
        }

        if (!window.isSecureContext) {
            setStatus("Notifications require HTTPS, except on localhost during development.", "warning");
            setDisabled(true);
            return;
        }

        if (!publicKey || publicKey.indexOf("REPLACE_WITH_") === 0) {
            setStatus("VAPID public key is not configured yet.", "warning");
            setDisabled(true);
            return;
        }

        refreshSubscriptionState();
        if (enableButton) {
            enableButton.addEventListener("click", enableNotifications);
        }
        if (unsubscribeButton) {
            unsubscribeButton.addEventListener("click", unsubscribe);
        }
        if (testButton) {
            testButton.addEventListener("click", sendTestNotification);
        }

        async function enableNotifications() {
            try {
                setBusy(true);
                setStatus("Waiting for browser permission...", "info");
                var permission = await Notification.requestPermission();
                if (permission !== "granted") {
                    setStatus("Notification permission was not granted.", "warning");
                    await refreshSubscriptionState();
                    return;
                }

                var registration = await navigator.serviceWorker.ready;
                var subscription = await registration.pushManager.getSubscription();
                if (!subscription) {
                    subscription = await registration.pushManager.subscribe({
                        userVisibleOnly: true,
                        applicationServerKey: urlBase64ToUint8Array(publicKey)
                    });
                }

                var response = await postJson(saveUrl, subscription.toJSON());
                setStatus(response.message || "Notifications enabled.", response.success ? "success" : "warning");
                await refreshSubscriptionState();
            } catch (error) {
                setStatus(error.message || "Unable to enable notifications.", "danger");
            } finally {
                setBusy(false);
                await refreshSubscriptionState({ preserveStatus: true });
            }
        }

        async function unsubscribe() {
            try {
                setBusy(true);
                var registration = await navigator.serviceWorker.ready;
                var subscription = await registration.pushManager.getSubscription();
                if (!subscription) {
                    setStatus("No browser subscription found.", "secondary");
                    await refreshSubscriptionState();
                    return;
                }

                await postJson(unsubscribeUrl, { endpoint: subscription.endpoint });
                await subscription.unsubscribe();
                setStatus("Notifications disabled for this browser.", "success");
                await refreshSubscriptionState();
            } catch (error) {
                setStatus(error.message || "Unable to unsubscribe.", "danger");
            } finally {
                setBusy(false);
                await refreshSubscriptionState({ preserveStatus: true });
            }
        }

        async function sendTestNotification() {
            try {
                setBusy(true);
                var response = await postJson(testUrl, {});
                setStatus(response.message || "Test notification requested.", response.success ? "success" : "warning");
            } catch (error) {
                setStatus(error.message || "Unable to send test notification.", "danger");
            } finally {
                setBusy(false);
                await refreshSubscriptionState({ preserveStatus: true });
            }
        }

        async function refreshSubscriptionState(options) {
            var preserveStatus = options && options.preserveStatus;
            try {
                if (Notification.permission === "denied") {
                    if (!preserveStatus) {
                        setStatus("Notifications are blocked in this browser.", "warning");
                    }
                    if (enableButton) {
                        enableButton.disabled = true;
                    }
                    if (unsubscribeButton) {
                        unsubscribeButton.disabled = true;
                    }
                    if (testButton) {
                        testButton.disabled = true;
                    }
                    return;
                }

                var registration = await navigator.serviceWorker.ready;
                var subscription = await registration.pushManager.getSubscription();
                if (subscription) {
                    if (!preserveStatus) {
                        setStatus("Notifications are enabled for this browser.", "success");
                    }
                    if (enableButton) {
                        enableButton.disabled = true;
                    }
                    if (unsubscribeButton) {
                        unsubscribeButton.disabled = false;
                    }
                    if (testButton) {
                        testButton.disabled = false;
                    }
                    return;
                }

                if (!preserveStatus) {
                    setStatus("Notifications are off.", "secondary");
                }
                if (enableButton) {
                    enableButton.disabled = false;
                }
                if (unsubscribeButton) {
                    unsubscribeButton.disabled = true;
                }
                if (testButton) {
                    testButton.disabled = true;
                }
            } catch (error) {
                if (!preserveStatus) {
                    setStatus("Unable to read notification status.", "warning");
                }
            }
        }

        async function postJson(url, data) {
            var response = await fetch(url, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": getAntiForgeryToken()
                },
                body: JSON.stringify(data || {})
            });

            var json = await response.json().catch(function () {
                return { success: false, message: "Unexpected server response." };
            });

            if (!response.ok) {
                throw new Error(json.message || "Request failed.");
            }

            return json;
        }

        function getAntiForgeryToken() {
            var token = root.querySelector('input[name="__RequestVerificationToken"]');
            return token ? token.value : "";
        }

        function setStatus(message, type) {
            if (!status) {
                return;
            }

            status.className = "alert small mb-0 mt-3 alert-" + (type || "info");
            status.textContent = message;
        }

        function setDisabled(disabled) {
            [enableButton, unsubscribeButton, testButton].forEach(function (button) {
                if (button) {
                    button.disabled = disabled;
                }
            });
        }

        function setBusy(isBusy) {
            root.dataset.pushBusy = isBusy ? "true" : "false";
            setDisabled(isBusy);
        }
    });

    function isSupported() {
        return "Notification" in window &&
            "serviceWorker" in navigator &&
            "PushManager" in window;
    }

    function urlBase64ToUint8Array(base64String) {
        var padding = "=".repeat((4 - base64String.length % 4) % 4);
        var base64 = (base64String + padding).replace(/-/g, "+").replace(/_/g, "/");
        var rawData = window.atob(base64);
        var outputArray = new Uint8Array(rawData.length);

        for (var i = 0; i < rawData.length; i += 1) {
            outputArray[i] = rawData.charCodeAt(i);
        }

        return outputArray;
    }
})();
