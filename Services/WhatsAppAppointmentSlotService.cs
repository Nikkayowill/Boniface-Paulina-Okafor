using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;

namespace Okafor_.NET.Services;

public sealed class WhatsAppAppointmentSlotService : IWhatsAppAppointmentSlotService
{
    private readonly ApplicationDbContext _context;

    public WhatsAppAppointmentSlotService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<AppointmentSlotDto>> FindAvailableSlotsAsync(
        string specialty,
        DateTime preferredDate,
        string timeWindow,
        CancellationToken cancellationToken = default)
    {
        var (startTime, endTime) = GetTimeWindow(timeWindow);
        var dayStart = preferredDate.Date.Add(startTime);
        var dayEnd = preferredDate.Date.Add(endTime);
        var specialtyTerm = NormalizeSpecialty(specialty);

        var exactMatches = await QueryOpenSlotsAsync(specialtyTerm, dayStart, dayEnd, 5, cancellationToken);
        if (exactMatches.Count > 0)
            return exactMatches;

        var fallbackStart = DateTime.Now > dayStart ? DateTime.Now : dayStart;
        var fallbackEnd = fallbackStart.AddHours(48);

        var storedFallbacks = await QueryOpenSlotsAsync(specialtyTerm, fallbackStart, fallbackEnd, 3, cancellationToken);
        if (storedFallbacks.Count > 0)
            return storedFallbacks;

        return await GenerateFallbackSlotsFromAvailabilityAsync(specialtyTerm, fallbackStart, fallbackEnd, cancellationToken);
    }

    private async Task<List<AppointmentSlotDto>> QueryOpenSlotsAsync(
        string specialtyTerm,
        DateTime rangeStart,
        DateTime rangeEnd,
        int take,
        CancellationToken cancellationToken)
    {
        return await _context.AppointmentSlots
            .AsNoTracking()
            .Include(slot => slot.Doctor)
            .ThenInclude(doctor => doctor.Department)
            .Where(slot =>
                !slot.IsBooked &&
                slot.SlotDateTime >= rangeStart &&
                slot.SlotDateTime < rangeEnd &&
                slot.Doctor != null &&
                (slot.Doctor.Specialty.ToLower().Contains(specialtyTerm) ||
                    (slot.Doctor.Department != null && slot.Doctor.Department.Name.ToLower().Contains(specialtyTerm))))
            .OrderBy(slot => slot.SlotDateTime)
            .Take(take)
            .Select(slot => new AppointmentSlotDto
            {
                SlotId = slot.Id,
                DoctorId = slot.DoctorId,
                DoctorName = slot.Doctor.FullName,
                Specialty = slot.Doctor.Specialty,
                SlotDateTime = slot.SlotDateTime,
                FormattedDateTime = slot.SlotDateTime.ToString("ddd, MMM d 'at' h:mm tt")
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<List<AppointmentSlotDto>> GenerateFallbackSlotsFromAvailabilityAsync(
        string specialtyTerm,
        DateTime rangeStart,
        DateTime rangeEnd,
        CancellationToken cancellationToken)
    {
        var doctors = await _context.Doctors
            .AsNoTracking()
            .Include(doctor => doctor.Department)
            .Where(doctor =>
                doctor.Specialty.ToLower().Contains(specialtyTerm) ||
                (doctor.Department != null && doctor.Department.Name.ToLower().Contains(specialtyTerm)))
            .ToListAsync(cancellationToken);

        if (doctors.Count == 0)
            return [];

        var doctorIds = doctors.Select(doctor => doctor.Id).ToList();
        var availabilities = await _context.DoctorAvailabilities
            .AsNoTracking()
            .Where(availability => doctorIds.Contains(availability.DoctorId) && availability.IsActive)
            .ToListAsync(cancellationToken);

        var bookedSlots = await _context.AppointmentSlots
            .AsNoTracking()
            .Where(slot =>
                doctorIds.Contains(slot.DoctorId) &&
                slot.IsBooked &&
                slot.SlotDateTime >= rangeStart &&
                slot.SlotDateTime < rangeEnd)
            .Select(slot => new { slot.DoctorId, slot.SlotDateTime })
            .ToListAsync(cancellationToken);

        var bookedLookup = bookedSlots
            .Select(slot => $"{slot.DoctorId}:{slot.SlotDateTime:O}")
            .ToHashSet(StringComparer.Ordinal);

        var results = new List<AppointmentSlotDto>();
        foreach (var doctor in doctors)
        {
            var doctorAvailability = availabilities.Where(availability => availability.DoctorId == doctor.Id);
            foreach (var availability in doctorAvailability)
            {
                var currentDate = rangeStart.Date;
                while (currentDate < rangeEnd.Date.AddDays(1))
                {
                    if (currentDate.DayOfWeek != availability.DayOfWeek)
                    {
                        currentDate = currentDate.AddDays(1);
                        continue;
                    }

                    var current = currentDate.Add(availability.StartTime);
                    var end = currentDate.Add(availability.EndTime);
                    while (current.AddMinutes(availability.SlotDurationMinutes) <= end)
                    {
                        var key = $"{doctor.Id}:{current:O}";
                        if (current >= rangeStart && current < rangeEnd && !bookedLookup.Contains(key))
                        {
                            results.Add(new AppointmentSlotDto
                            {
                                SlotId = 0,
                                DoctorId = doctor.Id,
                                DoctorName = doctor.FullName,
                                Specialty = doctor.Specialty,
                                SlotDateTime = current,
                                FormattedDateTime = current.ToString("ddd, MMM d 'at' h:mm tt")
                            });
                        }

                        current = current.AddMinutes(availability.SlotDurationMinutes);
                    }

                    currentDate = currentDate.AddDays(1);
                }
            }
        }

        return results
            .OrderBy(slot => slot.SlotDateTime)
            .Take(3)
            .ToList();
    }

    private static (TimeSpan Start, TimeSpan End) GetTimeWindow(string timeWindow)
    {
        return timeWindow.Trim().ToLowerInvariant() switch
        {
            "morning" => (TimeSpan.FromHours(8), TimeSpan.FromHours(12)),
            "afternoon" => (TimeSpan.FromHours(12), TimeSpan.FromHours(17)),
            "evening" => (TimeSpan.FromHours(17), TimeSpan.FromHours(20)),
            _ => (TimeSpan.FromHours(8), TimeSpan.FromHours(18))
        };
    }

    private static string NormalizeSpecialty(string specialty)
    {
        var normalized = specialty.Trim().ToLowerInvariant();
        return normalized switch
        {
            "general" => "general",
            "maternity" => "maternity",
            "pediatrics" or "paediatrics" => "pediatric",
            "diagnostics" or "laboratory" => "diagnostic",
            "surgery" => "surgical",
            "teleconsultation" => "general",
            _ => normalized
        };
    }
}
