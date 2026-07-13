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
                .withUrl('/hubs/bookings')
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
                adjustPendingCount('appointment', 1);
            });

            connection.on('teleconsultationSubmitted', function (payload) {
                showRealtimeNotice('New teleconsultation request from ' + (payload.patientName || 'a patient') + '. Refresh to review.');
                adjustPendingCount('teleconsultation', 1);
            });

            connection.on('bookingActioned', function (payload) {
                showRealtimeNotice('A booking queue item was updated.');
                updateStatusBadge(payload);
            });

            connection.on('bookingRemoved', function (payload) {
                showRealtimeNotice('A pending booking was cancelled.');
                removeBookingRows(payload);
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

    function adjustPendingCount(type, delta) {
        if (!type || !delta) {
            return;
        }

        var badge = document.querySelector('[data-booking-pending-count="' + type + '"]');
        if (!badge) {
            return;
        }

        var value = parseInt(badge.textContent || '0', 10);
        if (!Number.isNaN(value)) {
            badge.textContent = String(Math.max(0, value + delta));
        }
    }

    function updateStatusBadge(payload) {
        if (!payload || !payload.type || !payload.id || !payload.status) {
            return;
        }

        var badges = document.querySelectorAll('[data-booking-status="' + payload.type + ':' + payload.id + '"]');
        var previousStatus = badges.length > 0 ? (badges[0].textContent || '').trim() : '';

        badges.forEach(function (badge) {
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
        });

        if (badges.length > 0 && previousStatus !== payload.status) {
            if (previousStatus === 'Pending' && payload.status !== 'Pending') {
                adjustPendingCount(payload.type, -1);
            } else if (previousStatus !== 'Pending' && payload.status === 'Pending') {
                adjustPendingCount(payload.type, 1);
            }
        }
    }

    function removeBookingRows(payload) {
        if (!payload || !payload.type || !payload.id) {
            return;
        }

        var rows = document.querySelectorAll('[data-booking-row="' + payload.type + ':' + payload.id + '"]');
        var removedPending = false;

        rows.forEach(function (row) {
            var statusBadge = row.querySelector('[data-booking-status="' + payload.type + ':' + payload.id + '"]');
            var status = statusBadge ? (statusBadge.textContent || '').trim() : payload.status;

            if (status === 'Pending') {
                removedPending = true;
            }

            row.remove();
        });

        if (removedPending) {
            adjustPendingCount(payload.type, -1);
        }
    }
}());
