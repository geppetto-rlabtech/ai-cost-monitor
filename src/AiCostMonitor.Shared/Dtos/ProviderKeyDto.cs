namespace AiCostMonitor.Shared.Dtos;

public record ProviderKeyDto(
    Guid Id,
    string Provider,
    string Label,
    string MaskedKey,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSyncedAt
);

public record AddProviderKeyRequest(
    string Provider,
    string ApiKey,
    string Label
);