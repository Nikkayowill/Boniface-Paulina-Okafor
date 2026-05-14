using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;

namespace Okafor_.NET.Services;

public sealed class PushSubscriptionCleanupService : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(24);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PushSubscriptionCleanupService> _logger;

    public PushSubscriptionCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<PushSubscriptionCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await CleanupAsync(stoppingToken);
                await Task.Delay(CleanupInterval, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var cutoff = DateTime.UtcNow.AddDays(-30);

            var staleSubscriptions = await context.PushSubscriptions
                .Where(s => s.FailureCount >= 5 && s.LastFailureAt != null && s.LastFailureAt < cutoff)
                .ToListAsync(cancellationToken);

            if (staleSubscriptions.Count == 0)
            {
                return;
            }

            context.PushSubscriptions.RemoveRange(staleSubscriptions);
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Removed {Count} stale push subscriptions.", staleSubscriptions.Count);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clean stale push subscriptions.");
        }
    }
}
