function bookingWidget(departments) {
    return {
        step: 1,
        departments: departments,
        doctors: [],
        doctorsLoading: false,
        selectedDeptId: '',
        selectedDoctorId: '',
        selectedDate: '',
        slots: [],
        slotsLoading: false,
        selectedSlot: null,
        form: { name: '', phone: '', email: '', reason: '' },
        confirmed: false,
        submitting: false,
        result: null,
        error: null,
        subscribedDoctorDay: null,

        async loadDoctors() {
            if (!this.selectedDeptId) { this.doctors = []; return; }
            this.doctorsLoading = true;
            this.selectedDoctorId = '';
            this.selectedDate = '';
            this.slots = [];
            this.selectedSlot = null;
            try {
                const res = await fetch(`/Doctors/GetByDepartment?deptId=${this.selectedDeptId}`);
                this.doctors = await res.json();
            } catch { this.doctors = []; }
            finally { this.doctorsLoading = false; }
        },

        async loadSlots() {
            if (!this.selectedDate || !this.selectedDoctorId) return;
            this.slotsLoading = true;
            this.selectedSlot = null;
            this.slots = [];
            try {
                const res = await fetch(`/AppointmentRequests/GetAvailableSlots?doctorId=${this.selectedDoctorId}&date=${this.selectedDate}`);
                const data = await res.json();
                this.slots = data.slots || [];
                await this.subscribeToSlotUpdates();
            } catch { this.slots = []; }
            finally { this.slotsLoading = false; }
        },

        async subscribeToSlotUpdates() {
            const connection = await window.okaforBookingRealtime?.start?.();
            if (!connection || !this.selectedDoctorId || !this.selectedDate) return;

            const nextKey = `${this.selectedDoctorId}:${this.selectedDate}`;
            if (this.subscribedDoctorDay === nextKey) return;

            if (this.subscribedDoctorDay) {
                const parts = this.subscribedDoctorDay.split(':');
                await connection.invoke('UnsubscribeDoctorDay', parseInt(parts[0]), parts[1]).catch(() => {});
            }

            this.subscribedDoctorDay = nextKey;
            await connection.invoke('SubscribeDoctorDay', parseInt(this.selectedDoctorId), this.selectedDate).catch(() => {});

            if (!connection._okaforSlotHandlerAttached) {
                connection.on('slotBooked', (payload) => {
                    if (!payload || String(payload.doctorId) !== String(this.selectedDoctorId) || payload.date !== this.selectedDate) return;
                    this.slots = this.slots.filter(slot => slot !== payload.slot);
                    if (this.selectedSlot === payload.slot) {
                        this.selectedSlot = null;
                        this.error = payload.message || 'That time was just booked. Please choose another available slot.';
                    }
                });
                connection._okaforSlotHandlerAttached = true;
            }
        },

        async submitBooking() {
            this.error = null;
            this.submitting = true;
            try {
                const res = await fetch('/AppointmentRequests/BookSlot', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                    },
                    body: JSON.stringify({
                        doctorId: parseInt(this.selectedDoctorId),
                        slotDate: this.selectedDate,
                        slotTime: this.selectedSlot,
                        patientName: this.form.name,
                        patientPhone: this.form.phone,
                        patientEmail: this.form.email,
                        reasonForVisit: this.form.reason
                    })
                });
                const data = await res.json();
                if (data.success) {
                    this.result = data;
                    this.step = 4;
                } else {
                    this.error = data.message || 'An error occurred. Please try again.';
                }
            } catch {
                this.error = 'Connection error. Please check your internet and try again.';
            } finally {
                this.submitting = false;
            }
        },

        resetWidget() {
            this.step = 1;
            this.selectedDeptId = '';
            this.selectedDoctorId = '';
            this.doctors = [];
            this.selectedDate = '';
            this.slots = [];
            this.selectedSlot = null;
            this.form = { name: '', phone: '', email: '', reason: '' };
            this.confirmed = false;
            this.result = null;
            this.error = null;
        }
    };
}

// expose globally in case templates expect it
window.bookingWidget = bookingWidget;
