using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using System.Globalization;
using System.Security.Claims;

namespace Okafor_.NET.Hubs;

public class BookingHub : Hub
{
    private const int MaxAdvanceBookingDays = 60;
    private readonly ApplicationDbContext _context;

    public BookingHub(ApplicationDbContext context)
    {
        _context = context;
    }

    public override async Task OnConnectedAsync()
    {
        if (Context.User?.IsInRole("Admin") == true || Context.User?.IsInRole("Staff") == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, BookingHubGroups.AdminQueue);
        }

        var email = Context.User?.FindFirstValue(ClaimTypes.Email) ?? Context.User?.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(email))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, BookingHubGroups.Patient(email));
        }

        await base.OnConnectedAsync();
    }

    public async Task SubscribeDoctorDay(int doctorId, string date)
    {
        if (doctorId <= 0 ||
            !DateTime.TryParseExact(
                date,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedDate) ||
            parsedDate.Date < DateTime.Today ||
            parsedDate.Date > DateTime.Today.AddDays(MaxAdvanceBookingDays) ||
            !await _context.Doctors.AsNoTracking().AnyAsync(doctor => doctor.Id == doctorId))
        {
            return;
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            BookingHubGroups.DoctorDay(doctorId, parsedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
    }

    public Task UnsubscribeDoctorDay(int doctorId, string date)
    {
        if (doctorId <= 0 || string.IsNullOrWhiteSpace(date))
        {
            return Task.CompletedTask;
        }

        return Groups.RemoveFromGroupAsync(Context.ConnectionId, BookingHubGroups.DoctorDay(doctorId, date));
    }
}

public static class BookingHubGroups
{
    public const string AdminQueue = "booking-admin-queue";

    public static string Patient(string email) => $"booking-patient:{email.Trim().ToLowerInvariant()}";

    public static string DoctorDay(int doctorId, string date) => $"booking-doctor:{doctorId}:{date}";
}
