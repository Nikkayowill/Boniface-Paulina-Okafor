using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;

namespace Okafor_.NET.Services;

public interface IAppointmentRequestMaintenanceService
{
    Task<bool> DeleteRequestAsync(int appointmentRequestId, CancellationToken cancellationToken = default);
}

public sealed class AppointmentRequestMaintenanceService : IAppointmentRequestMaintenanceService
{
    private readonly ApplicationDbContext _context;

    public AppointmentRequestMaintenanceService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> DeleteRequestAsync(int appointmentRequestId, CancellationToken cancellationToken = default)
    {
        var request = await _context.AppointmentRequests
            .FirstOrDefaultAsync(a => a.Id == appointmentRequestId, cancellationToken);

        if (request is null)
            return false;

        var linkedPortalAppointments = await _context.PatientAppointments
            .Where(a => a.AppointmentRequestId == appointmentRequestId)
            .ToListAsync(cancellationToken);

        var linkedSlots = await _context.AppointmentSlots
            .Where(s => s.AppointmentRequestId == appointmentRequestId)
            .ToListAsync(cancellationToken);

        _context.PatientAppointments.RemoveRange(linkedPortalAppointments);

        foreach (var slot in linkedSlots)
        {
            slot.IsBooked = false;
            slot.AppointmentRequestId = null;
            slot.ReminderSent = false;
        }

        _context.AppointmentRequests.Remove(request);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
