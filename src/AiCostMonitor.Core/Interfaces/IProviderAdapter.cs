using AiCostMonitor.Core.Entities;

namespace AiCostMonitor.Core.Interfaces;

public interface IProviderAdapter
{
    string ProviderName { get; }
    Task<bool> TestKeyAsync(string apiKey, CancellationToken ct = default);
    Task<IEnumerable<UsageRecord>> FetchUsageAsync(
        string apiKey,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default);
}