using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Okafor_.NET.Hubs;

public class BookingHub : Hub
{
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

    public Task SubscribeDoctorDay(int doctorId, string date)
    {
        if (doctorId <= 0 || string.IsNullOrWhiteSpace(date))
        {
            return Task.CompletedTask;
        }

        return Groups.AddToGroupAsync(Context.ConnectionId, BookingHubGroups.DoctorDay(doctorId, date));
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
