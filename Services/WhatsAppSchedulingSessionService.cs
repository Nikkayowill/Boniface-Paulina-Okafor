using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;

namespace Okafor_.NET.Services;

public sealed class WhatsAppSchedulingSessionService : IWhatsAppSchedulingSessionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WhatsAppSchedulingSessionService> _logger;

    public WhatsAppSchedulingSessionService(
        ApplicationDbContext context,
        ILogger<WhatsAppSchedulingSessionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SaveSlotOptionsAsync(
        string patientPhoneNumber,
        List<AppointmentSlotDto> slots,
        CancellationToken cancellationToken = default)
    {
        var normalizedPhone = NigerianPhoneNumber.NormalizeToE164(patientPhoneNumber);
        if (string.IsNullOrWhiteSpace(normalizedPhone) || slots.Count == 0)
            return;

        var now = DateTime.UtcNow;
        var existingSessions = await _context.WhatsAppSchedulingSessions
            .Where(session => session.PatientPhone == normalizedPhone && session.Status == "Pending")
            .ToListAsync(cancellationToken);

        foreach (var session in existingSessions)
        {
            session.Status = "Expired";
        }

        _context.WhatsAppSchedulingSessions.Add(new WhatsAppSchedulingSession
        {
            PatientPhone = normalizedPhone,
            Status = "Pending",
            SlotOptionsJson = JsonSerializer.Serialize(slots.Take(5).ToList()),
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(20)
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<WhatsAppBookingConfirmationResult> TryConfirmSelectionAsync(
        string patientPhoneNumber,
        int selectedOptionNumber,
        CancellationToken cancellationToken = default)
    {
        var normalizedPhone = NigerianPhoneNumber.NormalizeToE164(patientPhoneNumber);
        if (string.IsNullOrWhiteSpace(normalizedPhone))
            return new WhatsAppBookingConfirmationResult { Error = "Invalid phone number." };

        var now = DateTime.UtcNow;
        var session = await _context.WhatsAppSchedulingSessions
            .Where(item =>
                item.PatientPhone == normalizedPhone &&
                item.Status == "Pending" &&
                item.ExpiresAt > now)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (session is null)
            return new WhatsAppBookingConfirmationResult { Error = "No active appointment options found." };

        var slotOptions = ReadSlotOptions(session.SlotOptionsJson);
        if (selectedOptionNumber < 1 || selectedOptionNumber > slotOptions.Count)
        {
            return new WhatsAppBookingConfirmationResult
            {
                Error = "Invalid selection.",
                OptionCount = slotOptions.Count
            };
        }

        var selectedSlot = slotOptions[selectedOptionNumber - 1];

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var doctor = await _context.Doctors
                .Include(item => item.Department)
                .FirstOrDefaultAsync(item => item.Id == selectedSlot.DoctorId, cancellationToken);

            if (doctor is null)
                return new WhatsAppBookingConfirmationResult { Error = "Selected doctor was not found." };

            var existingSlot = await _context.AppointmentSlots
                .FirstOrDefaultAsync(slot =>
                    slot.DoctorId == selectedSlot.DoctorId &&
                    slot.SlotDateTime == selectedSlot.SlotDateTime,
                    cancellationToken);

            if (existingSlot?.IsBooked == true)
            {
                session.Status = "Expired";
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return new WhatsAppBookingConfirmationResult { Error = "That appointment time has just been taken." };
            }

            var appointmentRequest = new AppointmentRequest
            {
                PatientName = "WhatsApp patient",
                Email = string.Empty,
                Phone = normalizedPhone,
                DepartmentId = doctor.DepartmentId,
                DoctorId = doctor.Id,
                PreferredDate = selectedSlot.SlotDateTime.Date,
                PreferredTime = selectedSlot.SlotDateTime.ToString("h:mm tt"),
                Message = "Booked through the WhatsApp appointment assistant. Staff should call or message the patient if more details are needed.",
                Status = AppointmentStatus.Approved,
                ContactConfirmed = true,
                ContactMethod = "WhatsApp",
                ContactConfirmedAt = now,
                ApprovedAt = now,
                CreatedAt = now
            };

            _context.AppointmentRequests.Add(appointmentRequest);
            await _context.SaveChangesAsync(cancellationToken);

            if (existingSlot is null)
            {
                _context.AppointmentSlots.Add(new AppointmentSlot
                {
                    DoctorId = doctor.Id,
                    SlotDateTime = selectedSlot.SlotDateTime,
                    IsBooked = true,
                    AppointmentRequestId = appointmentRequest.Id
                });
            }
            else
            {
                existingSlot.IsBooked = true;
                existingSlot.AppointmentRequestId = appointmentRequest.Id;
            }

            session.Status = "Confirmed";
            session.SelectedOptionNumber = selectedOptionNumber;
            session.AppointmentRequestId = appointmentRequest.Id;
            session.ConfirmedAt = now;

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            selectedSlot.DoctorName = doctor.FullName;
            selectedSlot.Specialty = doctor.Specialty;
            selectedSlot.FormattedDateTime = selectedSlot.SlotDateTime.ToString("ddd, MMM d 'at' h:mm tt");

            return new WhatsAppBookingConfirmationResult
            {
                Success = true,
                AppointmentRequestId = appointmentRequest.Id,
                Slot = selectedSlot,
                OptionCount = slotOptions.Count
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "WhatsApp appointment confirmation failed for {Phone}.", normalizedPhone);
            return new WhatsAppBookingConfirmationResult { Error = "We could not reserve that appointment time." };
        }
    }

    private static List<AppointmentSlotDto> ReadSlotOptions(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<AppointmentSlotDto>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
