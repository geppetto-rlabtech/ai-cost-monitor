using AiCostMonitor.Core.Interfaces;

namespace AiCostMonitor.Api.Services;

public class SyncBackgroundService(
    IServiceProvider services,
    IConfiguration config,
    ILogger<SyncBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var intervalHours = config.GetValue("Sync:IntervalHours", 6);
        logger.LogInformation("Background sync started. Interval: {Hours}h", intervalHours);

        // Wait a bit before first run to let the app start up
        await Task.Delay(TimeSpan.FromSeconds(30), ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                var sync = scope.ServiceProvider.GetRequiredService<ISyncService>();
                await sync.SyncAllUsersAsync(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during background sync");
            }

            await Task.Delay(TimeSpan.FromHours(intervalHours), ct);
        }
    }
}