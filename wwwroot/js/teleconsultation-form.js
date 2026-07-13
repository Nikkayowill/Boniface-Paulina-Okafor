(function () {
    'use strict';

    function initTeleconsultationForm(form) {
        if (form.dataset.teleconsultationReady === 'true') return;
        form.dataset.teleconsultationReady = 'true';

        var departmentSelect = form.querySelector('[data-teleconsultation-department]');
        var doctorSelect = form.querySelector('[data-teleconsultation-doctor]');
        var doctorHelp = form.querySelector('[data-teleconsultation-doctor-help]');
        var submitButton = form.querySelector('[data-teleconsultation-submit]');
        var submitLabel = form.querySelector('[data-teleconsultation-submit-label]');
        var submitSpinner = form.querySelector('[data-teleconsultation-submit-spinner]');
        var doctorsData = document.getElementById('teleconsultation-doctors-data');
        var doctors = [];
        var submitting = false;

        if (doctorsData) {
            try {
                doctors = JSON.parse(doctorsData.textContent || '[]');
            } catch {
                doctors = [];
            }
        }

        function setSubmitting(value) {
            submitting = value;
            if (submitButton) submitButton.disabled = value;
            if (submitLabel) submitLabel.textContent = value ? 'Submitting Request...' : 'Submit Teleconsultation Request';
            if (submitSpinner) submitSpinner.hidden = !value;
        }

        function renderDoctors(preserveSelection) {
            if (!departmentSelect || !doctorSelect) return;

            var departmentId = departmentSelect.value;
            var selectedDoctorId = preserveSelection ? doctorSelect.value : '';
            var matchingDoctors = doctors.filter(function (doctor) {
                return departmentId && String(doctor.departmentId) === String(departmentId);
            });

            doctorSelect.innerHTML = '';
            var defaultOption = document.createElement('option');
            defaultOption.value = '';
            defaultOption.textContent = 'No preference';
            doctorSelect.appendChild(defaultOption);

            matchingDoctors.forEach(function (doctor) {
                var option = document.createElement('option');
                option.value = String(doctor.id);
                option.textContent = doctor.displayName || doctor.name || 'Doctor';
                option.selected = String(doctor.id) === String(selectedDoctorId);
                doctorSelect.appendChild(option);
            });

            doctorSelect.disabled = !departmentId || matchingDoctors.length === 0;

            if (doctorHelp) {
                if (!departmentId) {
                    doctorHelp.textContent = 'Choose a department first. You can leave the doctor as “No preference”.';
                } else if (matchingDoctors.length === 0) {
                    doctorHelp.textContent = 'No doctor is listed for this department. Staff can assign the appropriate clinician.';
                } else {
                    doctorHelp.textContent = 'Optional. Choose a doctor or leave “No preference” for staff assignment.';
                }
            }
        }

        if (departmentSelect) {
            departmentSelect.addEventListener('change', function () {
                renderDoctors(false);
            });
        }

        form.addEventListener('submit', function (event) {
            if (submitting) {
                event.preventDefault();
                return;
            }

            if (!form.checkValidity()) {
                return;
            }

            if (window.jQuery && window.jQuery.validator && !window.jQuery(form).valid()) {
                return;
            }

            setSubmitting(true);
        });

        window.addEventListener('pageshow', function () {
            setSubmitting(false);
        });

        renderDoctors(true);
    }

    function initAllTeleconsultationForms() {
        document.querySelectorAll('[data-teleconsultation-form]').forEach(initTeleconsultationForm);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initAllTeleconsultationForms);
    } else {
        initAllTeleconsultationForms();
    }
})();
