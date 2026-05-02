(function () {
    'use strict';

    window.okaforBookingRealtime = {
        connection: null,
        start: async function () {
            if (!window.signalR) {
                return null;
            }

            if (this.connection) {
                return this.connection;
            }

            this.connection = new signalR.HubConnectionBuilder()
                .withUrl('/hubs/booking')
                .withAutomaticReconnect([0, 1000, 5000, 10000, 30000])
                .build();

            try {
                await this.connection.start();
                document.dispatchEvent(new CustomEvent('bookingRealtime:connected', { detail: this.connection }));
                return this.connection;
            } catch {
                this.connection = null;
                return null;
            }
        }
    };

    document.addEventListener('DOMContentLoaded', function () {
        window.okaforBookingRealtime.start().then(function (connection) {
            if (!connection) {
                return;
            }

            connection.on('appointmentSubmitted', function (payload) {
                showRealtimeNotice('New appointment request from ' + (payload.patientName || 'a patient') + '. Refresh to review.');
                incrementBadge('[data-booking-pending-count]');
            });

            connection.on('teleconsultationSubmitted', function (payload) {
                showRealtimeNotice('New teleconsultation request from ' + (payload.patientName || 'a patient') + '. Refresh to review.');
                incrementBadge('[data-booking-pending-count]');
            });

            connection.on('bookingActioned', function () {
                showRealtimeNotice('A booking queue item was updated.');
            });

            connection.on('bookingStatusChanged', function (payload) {
                var message = payload && payload.message ? payload.message : 'Your booking status changed.';
                showRealtimeNotice(message);
                updateStatusBadge(payload);
            });
        });
    });

    function showRealtimeNotice(message) {
        var target = document.querySelector('[data-booking-realtime]');
        if (!target) {
            return;
        }

        target.classList.remove('d-none');
        target.textContent = message;
    }

    function incrementBadge(selector) {
        var badge = document.querySelector(selector);
        if (!badge) {
            return;
        }

        var value = parseInt(badge.textContent || '0', 10);
        if (!Number.isNaN(value)) {
            badge.textContent = String(value + 1);
        }
    }

    function updateStatusBadge(payload) {
        if (!payload || !payload.type || !payload.id || !payload.status) {
            return;
        }

        var badge = document.querySelector('[data-booking-status="' + payload.type + ':' + payload.id + '"]');
        if (badge) {
            badge.textContent = payload.status;
            badge.classList.remove('bg-warning', 'bg-success', 'bg-danger', 'bg-info', 'bg-secondary', 'text-dark');
            if (payload.status === 'Approved' || payload.status === 'Confirmed') {
                badge.classList.add('bg-success');
            } else if (payload.status === 'Rejected') {
                badge.classList.add('bg-danger');
            } else if (payload.status === 'Rescheduled') {
                badge.classList.add('bg-info', 'text-dark');
            } else if (payload.status === 'Completed') {
                badge.classList.add('bg-secondary');
            } else {
                badge.classList.add('bg-warning', 'text-dark');
            }
        }
    }
}());
