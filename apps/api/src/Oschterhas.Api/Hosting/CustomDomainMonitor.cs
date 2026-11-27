using Oschterhas.Core.Domains;

namespace Oschterhas.Api.Hosting;

/// <summary>
/// Re-checks custom domains in the background: pending ones every 10 minutes, verified ones daily.
/// The rules live in <see cref="ICustomDomainService.CheckDueDomainsAsync"/>; this only runs the loop.
/// </summary>
internal sealed class CustomDomainMonitor(
    IServiceScopeFactory scopes,
    TimeProvider timeProvider,
    ILogger<CustomDomainMonitor> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, timeProvider);
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<ICustomDomainService>();
                var checkedCount = await service.CheckDueDomainsAsync(stoppingToken);
                if (checkedCount > 0)
                {
                    logger.LogInformation("Custom domains checked: {Count}", checkedCount);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Keep the monitor alive; the next round tries again.
                logger.LogWarning(ex, "Custom domain check failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
