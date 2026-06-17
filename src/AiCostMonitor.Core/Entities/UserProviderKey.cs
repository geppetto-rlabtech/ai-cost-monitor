namespace AiCostMonitor.Core.Entities;

public class UserProviderKey
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string EncryptedApiKey { get; set; } = string.Empty;
    public string KeySuffix { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastSyncedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<UsageRecord> UsageRecords { get; set; } = [];
}