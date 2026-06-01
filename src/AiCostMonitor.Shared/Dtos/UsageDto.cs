namespace AiCostMonitor.Shared.Dtos;

public record UsageRecordDto(
    string Provider,
    string Model,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    long InputTokens,
    long OutputTokens,
    long CacheReadTokens,
    long CacheCreationTokens,
    decimal CostUsd
);

public record UsageSummaryDto(
    string Provider,
    decimal TotalCostUsd,
    long TotalInputTokens,
    long TotalOutputTokens,
    int RecordCount
);

public record DashboardDto(
    decimal TotalCostThisMonth,
    decimal TotalCostLastMonth,
    IEnumerable<UsageSummaryDto> ByProvider,
    IEnumerable<DailySpendDto> DailyTrend,
    IEnumerable<ModelBreakdownDto> TopModels
);

public record DailySpendDto(DateOnly Date, decimal CostUsd);
public record ModelBreakdownDto(string Provider, string Model, decimal CostUsd, long TotalTokens);