using AiCostMonitor.Api.Data;
using AiCostMonitor.Core.Entities;
using AiCostMonitor.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AiCostMonitor.Api.Services;

public class SyncService(
    AppDbContext db,
    IEnumerable<IProviderAdapter> adapters,
    IEncryptionService encryption,
    ILogger<SyncService> logger) : ISyncService
{
    public async Task SyncUserAsync(Guid userId, string? provider = null, CancellationToken ct = default)
    {
        var keys = await db.ProviderKeys
            .Where(k => k.UserId == userId && k.IsActive)
            .Where(k => provider == null || k.Provider == provider)
            .ToListAsync(ct);

        foreach (var key in keys)
            await SyncKeyAsync(key, ct);
    }

    public async Task SyncAllUsersAsync(CancellationToken ct = default)
    {
        var keys = await db.ProviderKeys
            .Where(k => k.IsActive)
            .ToListAsync(ct);

        foreach (var key in keys)
        {
            if (ct.IsCancellationRequested) break;
            await SyncKeyAsync(key, ct);
        }
    }

    private async Task SyncKeyAsync(UserProviderKey key, CancellationToken ct)
    {
        var adapter = adapters.FirstOrDefault(a => a.ProviderName == key.Provider);
        if (adapter is null)
        {
            logger.LogWarning("No adapter found for provider {Provider}", key.Provider);
            return;
        }

        try
        {
            // Fetch from last sync or 90 days back
            var from = key.LastSyncedAt?.AddHours(-1) ?? DateTimeOffset.UtcNow.AddDays(-90);
            var to = DateTimeOffset.UtcNow;

            logger.LogInformation("Syncing {Provider} key {KeyId} from {From} to {To}",
                key.Provider, key.Id, from, to);

            var plainKey = encryption.Decrypt(key.EncryptedApiKey);
            var records = (await adapter.FetchUsageAsync(plainKey, from, to, ct)).ToList();

            // Upsert: skip records we already have for same key/model/period
            foreach (var record in records)
            {
                record.UserId = key.UserId;
                record.ProviderKeyId = key.Id;

                var exists = await db.UsageRecords.AnyAsync(r =>
                    r.ProviderKeyId == key.Id &&
                    r.Model == record.Model &&
                    r.PeriodStart == record.PeriodStart, ct);

                if (!exists)
                    db.UsageRecords.Add(record);
                else
                {
                    // Update the existing record (cost may have changed)
                    var existing = await db.UsageRecords.FirstAsync(r =>
                        r.ProviderKeyId == key.Id &&
                        r.Model == record.Model &&
                        r.PeriodStart == record.PeriodStart, ct);

                    existing.InputTokens = record.InputTokens;
                    existing.OutputTokens = record.OutputTokens;
                    existing.CacheReadTokens = record.CacheReadTokens;
                    existing.CacheCreationTokens = record.CacheCreationTokens;
                    existing.CostUsd = record.CostUsd;
                    existing.SyncedAt = record.SyncedAt;
                }
            }

            key.LastSyncedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);

            logger.LogInformation("Synced {Count} records for key {KeyId}", records.Count, key.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to sync key {KeyId} ({Provider})", key.Id, key.Provider);
        }
    }
}