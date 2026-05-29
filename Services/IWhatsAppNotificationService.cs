using Okafor_.NET.Models;

namespace Okafor_.NET.Services;

public interface IWhatsAppNotificationService
{
    Task<bool> SendTextMessageAsync(string recipientPhone, string message, CancellationToken cancellationToken = default);

    Task<bool> SendTeleconsultationReceivedAsync(TeleconsultationRequest request, CancellationToken cancellationToken = default);

    Task<bool> SendTeleconsultationStatusAsync(TeleconsultationRequest request, CancellationToken cancellationToken = default);
}
