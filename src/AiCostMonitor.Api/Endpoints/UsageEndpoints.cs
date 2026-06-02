using AiCostMonitor.Api.Data;
using AiCostMonitor.Api.Extensions;
using AiCostMonitor.Shared.Dtos;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AiCostMonitor.Api.Endpoints;

public static class UsageEndpoints
{
    public static void MapUsageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/usage").RequireAuthorization();

        group.MapGet("/", GetUsage);
        group.MapGet("/by-model", GetByModel);
        group.MapGet("/summary", GetSummary);
    }

    static async Task<IResult> GetUsage(
        ClaimsPrincipal user, AppDbContext db,
        DateTimeOffset? from, DateTimeOffset? to, string? provider)
    {
        var userId = user.GetUserId();
        var fromDate = from ?? DateTimeOffset.UtcNow.AddDays(-30);
        var toDate = to ?? DateTimeOffset.UtcNow;

        var query = db.UsageRecords
            .Where(r => r.UserId == userId
                     && r.PeriodStart >= fromDate
                     && r.PeriodEnd <= toDate);

        if (provider is not null)
            query = query.Where(r => r.Provider == provider);

        var records = await query
            .OrderByDescending(r => r.PeriodStart)
            .Select(r => new UsageRecordDto(
                r.Provider, r.Model,
                r.PeriodStart, r.PeriodEnd,
                r.InputTokens, r.OutputTokens,
                r.CacheReadTokens, r.CacheCreationTokens,
                r.CostUsd))
            .ToListAsync();

        return Results.Ok(records);
    }

    static async Task<IResult> GetByModel(
        ClaimsPrincipal user, AppDbContext db,
        DateTimeOffset? from, DateTimeOffset? to)
    {
        var userId = user.GetUserId();
        var fromDate = from ?? DateTimeOffset.UtcNow.AddDays(-30);
        var toDate = to ?? DateTimeOffset.UtcNow;

        var breakdown = await db.UsageRecords
            .Where(r => r.UserId == userId
                     && r.PeriodStart >= fromDate
                     && r.PeriodEnd <= toDate)
            .GroupBy(r => new { r.Provider, r.Model })
            .Select(g => new ModelBreakdownDto(
                g.Key.Provider,
                g.Key.Model,
                g.Sum(r => r.CostUsd),
                g.Sum(r => r.InputTokens + r.OutputTokens)))
            .OrderByDescending(x => x.CostUsd)
            .ToListAsync();

        return Results.Ok(breakdown);
    }

    static async Task<IResult> GetSummary(
        ClaimsPrincipal user, AppDbContext db)
    {
        var userId = user.GetUserId();
        var now = DateTimeOffset.UtcNow;
        var thisMonthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var summary = await db.UsageRecords
            .Where(r => r.UserId == userId && r.PeriodStart >= thisMonthStart)
            .GroupBy(r => r.Provider)
            .Select(g => new UsageSummaryDto(
                g.Key,
                g.Sum(r => r.CostUsd),
                g.Sum(r => r.InputTokens),
                g.Sum(r => r.OutputTokens),
                g.Count()))
            .OrderByDescending(s => s.TotalCostUsd)
            .ToListAsync();

        return Results.Ok(summary);
    }
}
