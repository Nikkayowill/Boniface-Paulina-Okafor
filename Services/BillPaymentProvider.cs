using Okafor_.NET.Models;

namespace Okafor_.NET.Services;

public record BillPaymentProviderResult(
    bool Success,
    string ProviderReference,
    string Channel,
    string Message,
    bool IsSandbox);

public interface IBillPaymentProvider
{
    Task<BillPaymentProviderResult> ProcessAsync(BillPayment payment, CancellationToken cancellationToken = default);
}

public sealed class MockBillPaymentProvider : IBillPaymentProvider
{
    private readonly IConfiguration _configuration;

    public MockBillPaymentProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<BillPaymentProviderResult> ProcessAsync(BillPayment payment, CancellationToken cancellationToken = default)
    {
        var prefix = _configuration["Payments:Mock:ReferencePrefix"] ?? "SANDBOX";
        var reference = $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmss}-{payment.Id:D6}";

        return Task.FromResult(new BillPaymentProviderResult(
            Success: true,
            ProviderReference: reference,
            Channel: "Sandbox",
            Message: "Sandbox payment approved. No real money was collected.",
            IsSandbox: true));
    }
}
