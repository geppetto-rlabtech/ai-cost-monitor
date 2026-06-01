namespace AiCostMonitor.Shared.Dtos;

public record SyncStatusDto(
    string Provider,
    DateTimeOffset? LastSyncedAt,
    bool IsRunning,
    string? LastError
);