namespace AiCostMonitor.Core.Entities;

public class UsageRecord
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ProviderKeyId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public long CacheReadTokens { get; set; }
    public long CacheCreationTokens { get; set; }
    public decimal CostUsd { get; set; }
    public string? RawJson { get; set; }
    public DateTimeOffset SyncedAt { get; set; } = DateTimeOffset.UtcNow;

    public User User { get; set; } = null!;
    public UserProviderKey ProviderKey { get; set; } = null!;
}