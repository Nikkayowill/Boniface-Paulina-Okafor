using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;

namespace Okafor_.NET.Services;

public interface IAvailabilityService
{
    Task<List<DateTime>> GetAvailableSlotsAsync(int doctorId, DateTime date);
    Task<(bool Success, string? Error)> ReserveSlotAsync(int doctorId, DateTime slotDateTime, int appointmentRequestId);
}

public class AvailabilityService : IAvailabilityService
{
    private readonly ApplicationDbContext _context;

    public AvailabilityService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<DateTime>> GetAvailableSlotsAsync(int doctorId, DateTime date)
    {
        var availability = await _context.DoctorAvailabilities
            .AsNoTracking()
            .FirstOrDefaultAsync(a =>
                a.DoctorId == doctorId &&
                a.DayOfWeek == date.DayOfWeek &&
                a.IsActive);

        if (availability is null)
            return [];

        // Generate all possible slots for the day
        var allSlots = new List<DateTime>();
        var current = date.Date.Add(availability.StartTime);
        var end = date.Date.Add(availability.EndTime);

        while (current.Add(TimeSpan.FromMinutes(availability.SlotDurationMinutes)) <= end)
        {
            allSlots.Add(current);
            current = current.AddMinutes(availability.SlotDurationMinutes);
        }

        if (allSlots.Count == 0)
            return [];

        // Load already-booked slots for this doctor on this date
        var bookedSlots = await _context.AppointmentSlots
            .AsNoTracking()
            .Where(s =>
                s.DoctorId == doctorId &&
                s.SlotDateTime.Date == date.Date &&
                s.IsBooked)
            .Select(s => s.SlotDateTime)
            .ToListAsync();

        // Filter out past slots (for today) and booked slots
        var now = DateTime.Now;
        return allSlots
            .Where(s => s > now && !bookedSlots.Contains(s))
            .ToList();
    }

    public async Task<(bool Success, string? Error)> ReserveSlotAsync(
        int doctorId, DateTime slotDateTime, int appointmentRequestId)
    {
        // Check if slot already booked (race-condition safe: use a transaction)
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var existing = await _context.AppointmentSlots
                .FirstOrDefaultAsync(s =>
                    s.DoctorId == doctorId &&
                    s.SlotDateTime == slotDateTime);

            if (existing?.IsBooked == true)
                return (false, "This slot has just been taken. Please choose another time.");

            if (existing is not null)
            {
                existing.IsBooked = true;
                existing.AppointmentRequestId = appointmentRequestId;
            }
            else
            {
                var slot = new AppointmentSlot
                {
                    DoctorId = doctorId,
                    SlotDateTime = slotDateTime,
                    IsBooked = true,
                    AppointmentRequestId = appointmentRequestId
                };

                _context.AppointmentSlots.Add(slot);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, ex.Message);
        }
    }
}
