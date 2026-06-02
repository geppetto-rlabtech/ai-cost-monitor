using AiCostMonitor.Api.Data;
using AiCostMonitor.Api.Extensions;
using AiCostMonitor.Shared.Dtos;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AiCostMonitor.Api.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/dashboard", GetDashboard).RequireAuthorization();
    }

    static async Task<IResult> GetDashboard(ClaimsPrincipal user, AppDbContext db)
    {
        var userId = user.GetUserId();
        var now = DateTimeOffset.UtcNow;
        var thisMonthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var lastMonthStart = thisMonthStart.AddMonths(-1);

        // Aggregate by provider in the DB (this month)
        var byProvider = await db.UsageRecords
            .Where(r => r.UserId == userId && r.PeriodStart >= thisMonthStart)
            .GroupBy(r => r.Provider)
            .Select(g => new UsageSummaryDto(
                g.Key,
                g.Sum(r => r.CostUsd),
                g.Sum(r => r.InputTokens),
                g.Sum(r => r.OutputTokens),
                g.Count()))
            .ToListAsync();
         byProvider = byProvider.OrderByDescending(s => s.TotalCostUsd).ToList();

        // Totals for this month and last month
        var thisMonthTotal = await db.UsageRecords
            .Where(r => r.UserId == userId && r.PeriodStart >= thisMonthStart)
            .SumAsync(r => r.CostUsd);

        var lastMonthTotal = await db.UsageRecords
            .Where(r => r.UserId == userId && r.PeriodStart >= lastMonthStart && r.PeriodStart < thisMonthStart)
            .SumAsync(r => r.CostUsd);

        // Daily trend: last 30 days
        var thirtyDaysAgo = now.AddDays(-30);
        var dailyTrend = await db.UsageRecords
            .Where(r => r.UserId == userId && r.PeriodStart >= thirtyDaysAgo)
            .GroupBy(r => DateOnly.FromDateTime(r.PeriodStart.DateTime))
            .Select(g => new DailySpendDto(g.Key, g.Sum(r => r.CostUsd)))
            .OrderBy(d => d.Date)
            .ToListAsync();

        // Top 5 models this month — GroupBy composite key (translated to SQL)
        var topModels = await db.UsageRecords
            .Where(r => r.UserId == userId && r.PeriodStart >= thisMonthStart)
            .GroupBy(r => new { r.Provider, r.Model })
            .Select(g => new ModelBreakdownDto(
                g.Key.Provider, g.Key.Model,
                g.Sum(r => r.CostUsd),
                 g.Sum(r => r.InputTokens) + g.Sum(r => r.OutputTokens)))
            .ToListAsync();
         topModels = topModels.OrderByDescending(m => m.CostUsd).Take(5).ToList();

        return Results.Ok(new DashboardDto(
            thisMonthTotal,
            lastMonthTotal,
            byProvider,
            dailyTrend,
            topModels));
    }
}
