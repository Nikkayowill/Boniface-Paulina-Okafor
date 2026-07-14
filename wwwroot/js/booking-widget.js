(function () {
    'use strict';

    function byData(root, name) {
        return root.querySelector('[data-' + name + ']');
    }

    function allByData(root, name) {
        return Array.from(root.querySelectorAll('[data-' + name + ']'));
    }

    function setText(el, value) {
        if (el) el.textContent = value || '';
    }

    function initBookingWidget(root) {
        if (root.dataset.bookingWidgetReady === 'true') return;
        root.dataset.bookingWidgetReady = 'true';

        var doctors = window.__bookingDoctors || [];
        var state = {
            step: 1,
            selectedDepartmentId: '',
            selectedDoctorId: '',
            selectedDate: '',
            selectedSlot: '',
            slots: [],
            slotsRequestId: 0,
            slotsAbortController: null,
            subscribedDoctorDay: null,
            submitting: false
        };

        var departmentSelect = byData(root, 'booking-department');
        var doctorSelect = byData(root, 'booking-doctor');
        var dateInput = byData(root, 'booking-date');
        var nextButton = byData(root, 'next-step');
        var submitButton = byData(root, 'submit-booking');
        var submitLabel = byData(root, 'submit-label');
        var submitSpinner = byData(root, 'submit-spinner');
        var errorBox = byData(root, 'booking-error');
        var doctorSummary = byData(root, 'doctor-summary');
        var doctorSpecialty = byData(root, 'doctor-specialty');
        var doctorDepartment = byData(root, 'doctor-department');
        var emptyDateNote = byData(root, 'slots-empty-date');
        var loadingNote = byData(root, 'slots-loading');
        var slotsGrid = byData(root, 'slots-grid');
        var noSlotsNote = byData(root, 'slots-none');
        var formName = byData(root, 'form-name');
        var formPhone = byData(root, 'form-phone');
        var formEmail = byData(root, 'form-email');
        var formReason = byData(root, 'form-reason');
        var formConfirmed = byData(root, 'form-confirmed');

        state.selectedDepartmentId = departmentSelect ? departmentSelect.value : '';
        state.selectedDoctorId = doctorSelect ? doctorSelect.value : '';

        function selectedDoctor() {
            return doctors.find(function (doctor) {
                return String(doctor.id) === String(state.selectedDoctorId);
            }) || null;
        }

        function syncDoctorOptions(preserveSelection) {
            if (!doctorSelect) return;

            var previousDoctorId = preserveSelection
                ? doctorSelect.value || state.selectedDoctorId
                : '';
            var departmentId = departmentSelect ? departmentSelect.value : '';
            var matchingDoctors = departmentId
                ? doctors.filter(function (doctor) {
                    return String(doctor.departmentId) === String(departmentId);
                })
                : [];

            doctorSelect.replaceChildren();

            var placeholder = document.createElement('option');
            placeholder.value = '';
            placeholder.textContent = !departmentId
                ? 'Choose a department first...'
                : matchingDoctors.length > 0
                    ? 'Select a doctor...'
                    : 'No doctors currently listed';
            doctorSelect.appendChild(placeholder);

            matchingDoctors.forEach(function (doctor) {
                var option = document.createElement('option');
                option.value = String(doctor.id);
                option.textContent = doctor.displayName || doctor.fullName;
                doctorSelect.appendChild(option);
            });

            var canRestoreDoctor = matchingDoctors.some(function (doctor) {
                return String(doctor.id) === String(previousDoctorId);
            });

            if (canRestoreDoctor) {
                doctorSelect.value = String(previousDoctorId);
            }

            doctorSelect.disabled = !departmentId || matchingDoctors.length === 0;
            state.selectedDepartmentId = departmentId;
            state.selectedDoctorId = doctorSelect.value;
        }

        function showError(message) {
            if (!errorBox) return;
            errorBox.textContent = message || '';
            errorBox.hidden = !message;
            if (message) {
                errorBox.focus({ preventScroll: true });
                errorBox.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
            }
        }

        function setStep(nextStep) {
            state.step = nextStep;
            allByData(root, 'booking-step').forEach(function (section) {
                section.hidden = section.getAttribute('data-booking-step') !== String(nextStep);
            });

            for (var i = 1; i <= 3; i += 1) {
                var circle = root.querySelector('[data-step-circle="' + i + '"]');
                var label = root.querySelector('[data-step-label="' + i + '"]');
                var number = root.querySelector('[data-step-number="' + i + '"]');
                var check = root.querySelector('[data-step-check="' + i + '"]');
                var line = root.querySelector('[data-step-line="' + i + '"]');

                if (circle) {
                    circle.classList.toggle('bg-primary-800', nextStep >= i);
                    circle.classList.toggle('text-white', nextStep >= i);
                    circle.classList.toggle('border', nextStep < i);
                    circle.classList.toggle('border-slate-300', nextStep < i);
                    circle.classList.toggle('bg-white', nextStep < i);
                    circle.classList.toggle('text-slate-400', nextStep < i);

                    if (nextStep === i) {
                        circle.setAttribute('aria-current', 'step');
                    } else {
                        circle.removeAttribute('aria-current');
                    }
                }

                if (label) {
                    label.classList.toggle('text-primary-800', nextStep >= i);
                    label.classList.toggle('text-slate-400', nextStep < i);
                }

                if (number) number.hidden = nextStep > i;
                if (check) check.hidden = nextStep <= i;

                if (line) {
                    line.classList.toggle('bg-primary-800', nextStep > i);
                    line.classList.toggle('bg-slate-200', nextStep <= i);
                }
            }
        }

        function resetSlots() {
            if (state.slotsAbortController) {
                state.slotsAbortController.abort();
                state.slotsAbortController = null;
            }
            state.selectedDate = '';
            state.selectedSlot = '';
            state.slots = [];
            state.slotsRequestId += 1;
            if (dateInput) dateInput.value = '';
            if (slotsGrid) {
                slotsGrid.innerHTML = '';
                slotsGrid.hidden = true;
            }
            if (loadingNote) loadingNote.hidden = true;
            if (noSlotsNote) noSlotsNote.hidden = true;
            if (emptyDateNote) emptyDateNote.hidden = false;
        }

        function syncDoctorSummary() {
            var doctor = selectedDoctor();
            if (!doctorSummary) return;

            doctorSummary.hidden = !doctor;
            setText(doctorSpecialty, doctor ? doctor.specialty : '');
            setText(doctorDepartment, doctor ? doctor.department : '');
        }

        function syncButtons() {
            if (nextButton) nextButton.disabled = !state.selectedDepartmentId || !state.selectedDoctorId;
            if (submitButton) {
                submitButton.disabled = state.submitting ||
                    !state.selectedDoctorId ||
                    !state.selectedDate ||
                    !state.selectedSlot ||
                    !formName.value.trim() || !formName.checkValidity() ||
                    !formPhone.value.trim() || !formPhone.checkValidity() ||
                    !formEmail.value.trim() || !formEmail.checkValidity() ||
                    !formConfirmed.checked;
            }
        }

        function renderSlots() {
            if (!slotsGrid) return;

            slotsGrid.innerHTML = '';
            slotsGrid.hidden = state.slots.length === 0;
            if (noSlotsNote) noSlotsNote.hidden = !state.selectedDate || state.slots.length > 0;
            if (emptyDateNote) emptyDateNote.hidden = Boolean(state.selectedDate);

            state.slots.forEach(function (slot) {
                var button = document.createElement('button');
                button.type = 'button';
                button.className = 'ok-slot-btn bg-white text-slate-950 hover:bg-slate-50';
                button.textContent = slot;
                button.addEventListener('click', function () {
                    selectSlot(slot);
                });
                slotsGrid.appendChild(button);
            });
        }

        function highlightSelectedSlot() {
            if (!slotsGrid) return;
            Array.from(slotsGrid.querySelectorAll('button')).forEach(function (button) {
                var active = button.textContent === state.selectedSlot;
                button.classList.toggle('bg-primary-800', active);
                button.classList.toggle('text-white', active);
                button.classList.toggle('bg-white', !active);
                button.classList.toggle('text-slate-950', !active);
                button.classList.toggle('hover:bg-slate-50', !active);
            });
        }

        async function loadSlots() {
            if (!state.selectedDoctorId || !state.selectedDate) {
                resetSlots();
                return;
            }

            var requestId = ++state.slotsRequestId;
            if (state.slotsAbortController) {
                state.slotsAbortController.abort();
            }
            var abortController = new AbortController();
            state.slotsAbortController = abortController;
            var timeoutId = window.setTimeout(function () {
                abortController.abort();
            }, 10000);

            state.selectedSlot = '';
            state.slots = [];
            showError('');
            if (slotsGrid) {
                slotsGrid.innerHTML = '';
                slotsGrid.hidden = true;
            }
            if (noSlotsNote) noSlotsNote.hidden = true;
            if (emptyDateNote) emptyDateNote.hidden = true;
            if (loadingNote) loadingNote.hidden = true;
            syncButtons();

            var loadingDelayId = window.setTimeout(function () {
                if (state.slotsRequestId === requestId && loadingNote) {
                    loadingNote.hidden = false;
                }
            }, 250);

            try {
                var res = await fetch('/AppointmentRequests/GetAvailableSlots?doctorId=' + encodeURIComponent(state.selectedDoctorId) + '&date=' + encodeURIComponent(state.selectedDate), {
                    signal: abortController.signal
                });
                var data = await res.json();
                if (state.slotsRequestId !== requestId) return;

                state.slots = data.slots || [];
                if (loadingNote) loadingNote.hidden = true;
                renderSlots();
                await subscribeToSlotUpdates();
            } catch {
                if (state.slotsRequestId !== requestId) return;
                state.slots = [];
                if (loadingNote) loadingNote.hidden = true;
                showError('Could not load available times. Please try again.');
                renderSlots();
            } finally {
                window.clearTimeout(loadingDelayId);
                window.clearTimeout(timeoutId);
                if (state.slotsAbortController === abortController) {
                    state.slotsAbortController = null;
                }
                if (state.slotsRequestId === requestId && loadingNote) {
                    loadingNote.hidden = true;
                }
            }
        }

        function selectSlot(slot) {
            if (!state.selectedDoctorId || !state.selectedDate || state.slots.indexOf(slot) === -1) {
                state.selectedSlot = '';
                showError('Please choose an available time slot.');
                setStep(2);
                syncButtons();
                return;
            }

            showError('');
            state.selectedSlot = slot;
            highlightSelectedSlot();
            setStep(3);
            syncButtons();
        }

        async function subscribeToSlotUpdates() {
            var connection = await window.okaforBookingRealtime?.start?.();
            if (!connection || !state.selectedDoctorId || !state.selectedDate) return;

            var nextKey = state.selectedDoctorId + ':' + state.selectedDate;
            if (state.subscribedDoctorDay === nextKey) return;

            if (state.subscribedDoctorDay) {
                await unsubscribeFromSlotUpdates();
            }

            state.subscribedDoctorDay = nextKey;
            await connection.invoke('SubscribeDoctorDay', parseInt(state.selectedDoctorId, 10), state.selectedDate).catch(function () {});

            if (!connection._okaforSlotHandlerAttached) {
                connection.on('slotBooked', function (payload) {
                    if (!payload || String(payload.doctorId) !== String(state.selectedDoctorId) || payload.date !== state.selectedDate) return;

                    state.slots = state.slots.filter(function (slot) { return slot !== payload.slot; });
                    if (state.selectedSlot === payload.slot) {
                        state.selectedSlot = '';
                        showError(payload.message || 'That time was just booked. Please choose another available slot.');
                        setStep(2);
                    }
                    renderSlots();
                    syncButtons();
                });
                connection._okaforSlotHandlerAttached = true;
            }
        }

        async function unsubscribeFromSlotUpdates() {
            if (!state.subscribedDoctorDay) return;

            var subscribedDoctorDay = state.subscribedDoctorDay;
            state.subscribedDoctorDay = null;

            var connection = window.okaforBookingRealtime?.connection;
            if (!connection) return;

            var parts = subscribedDoctorDay.split(':');
            await connection.invoke('UnsubscribeDoctorDay', parseInt(parts[0], 10), parts[1]).catch(function () {});
        }

        function setSubmitting(value) {
            state.submitting = value;
            if (submitLabel) submitLabel.hidden = value;
            if (submitSpinner) submitSpinner.hidden = !value;
            syncButtons();
        }

        async function submitBooking() {
            showError('');
            if (!state.selectedDoctorId || !state.selectedDate || !state.selectedSlot || state.slots.indexOf(state.selectedSlot) === -1) {
                state.selectedSlot = '';
                showError('Please choose an available time slot.');
                setStep(2);
                syncButtons();
                return;
            }

            setSubmitting(true);
            try {
                var res = await fetch('/AppointmentRequests/BookSlot', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': root.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                    },
                    body: JSON.stringify({
                        doctorId: parseInt(state.selectedDoctorId, 10),
                        slotDate: state.selectedDate,
                        slotTime: state.selectedSlot,
                        patientName: formName.value.trim(),
                        patientPhone: formPhone.value.trim(),
                        patientEmail: formEmail.value.trim(),
                        reasonForVisit: formReason.value.trim()
                    })
                });
                var data = await res.json();

                if (data.success) {
                    await unsubscribeFromSlotUpdates();
                    renderSuccess(data);
                    setStep(4);
                } else {
                    showError(data.message || 'An error occurred. Please try again.');
                }
            } catch {
                showError('Connection error. Please check your internet and try again.');
            } finally {
                setSubmitting(false);
            }
        }

        function renderSuccess(data) {
            setText(byData(root, 'result-email'), formEmail.value.trim());
            setText(byData(root, 'result-reference'), data.confirmationRef);
            setText(byData(root, 'result-doctor'), data.doctorName);
            setText(byData(root, 'result-department'), data.department);
            setText(byData(root, 'result-date'), data.appointmentDate);
            setText(byData(root, 'result-time'), data.appointmentTime);

            var whatsAppLink = byData(root, 'result-whatsapp');
            if (whatsAppLink) {
                whatsAppLink.href = data.whatsAppUrl || '#';
            }
        }

        function resetWidget() {
            unsubscribeFromSlotUpdates();
            state.selectedDepartmentId = '';
            state.selectedDoctorId = '';
            state.selectedSlot = '';
            state.slots = [];
            state.slotsRequestId += 1;
            if (departmentSelect) departmentSelect.value = '';
            syncDoctorOptions(false);
            formName.value = '';
            formPhone.value = '';
            formEmail.value = '';
            formReason.value = '';
            formConfirmed.checked = false;
            resetSlots();
            syncDoctorSummary();
            showError('');
            setStep(1);
            syncButtons();
        }

        if (departmentSelect) {
            departmentSelect.addEventListener('change', function () {
                unsubscribeFromSlotUpdates();
                syncDoctorOptions(false);
                resetSlots();
                syncDoctorSummary();
                showError('');
                syncButtons();
            });
        }

        if (doctorSelect) {
            doctorSelect.addEventListener('change', function () {
                state.selectedDoctorId = doctorSelect.value;
                unsubscribeFromSlotUpdates();
                resetSlots();
                syncDoctorSummary();
                showError('');
                syncButtons();
            });
        }

        if (dateInput) {
            dateInput.addEventListener('change', function () {
                state.selectedDate = dateInput.value;
                loadSlots();
            });
        }

        if (nextButton) {
            nextButton.addEventListener('click', function () {
                if (state.selectedDoctorId) setStep(2);
            });
        }

        allByData(root, 'back-step').forEach(function (button) {
            button.addEventListener('click', function () {
                setStep(Number(button.getAttribute('data-back-step')));
            });
        });

        [formName, formPhone, formEmail, formReason, formConfirmed].forEach(function (field) {
            if (!field) return;
            field.addEventListener('input', syncButtons);
            field.addEventListener('change', syncButtons);
        });

        if (submitButton) submitButton.addEventListener('click', submitBooking);

        var resetButton = byData(root, 'reset-booking');
        if (resetButton) resetButton.addEventListener('click', resetWidget);

        syncDoctorOptions(true);
        setStep(1);
        syncDoctorSummary();
        syncButtons();
    }

    function initAllBookingWidgets() {
        document.querySelectorAll('[data-booking-widget]').forEach(initBookingWidget);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initAllBookingWidgets);
    } else {
        initAllBookingWidgets();
    }
})();
