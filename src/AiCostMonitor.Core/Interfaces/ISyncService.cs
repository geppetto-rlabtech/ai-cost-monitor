namespace AiCostMonitor.Core.Interfaces;

public interface ISyncService
{
    Task SyncUserAsync(Guid userId, string? provider = null, CancellationToken ct = default);
    Task SyncAllUsersAsync(CancellationToken ct = default);
}