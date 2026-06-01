using AiCostMonitor.Api.Data;
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
        var userId = GetUserId(user);
        var now = DateTimeOffset.UtcNow;
        var thisMonthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var lastMonthStart = thisMonthStart.AddMonths(-1);

        var thisMonthRecords = await db.UsageRecords
            .Where(r => r.UserId == userId && r.PeriodStart >= thisMonthStart)
            .ToListAsync();

        var lastMonthRecords = await db.UsageRecords
            .Where(r => r.UserId == userId && r.PeriodStart >= lastMonthStart && r.PeriodStart < thisMonthStart)
            .ToListAsync();

        var byProvider = thisMonthRecords
            .GroupBy(r => r.Provider)
            .Select(g => new UsageSummaryDto(
                g.Key,
                g.Sum(r => r.CostUsd),
                g.Sum(r => r.InputTokens),
                g.Sum(r => r.OutputTokens),
                g.Count()))
            .OrderByDescending(s => s.TotalCostUsd)
            .ToList();

        // Daily trend: last 30 days
        var thirtyDaysAgo = now.AddDays(-30);
        var dailyTrend = await db.UsageRecords
            .Where(r => r.UserId == userId && r.PeriodStart >= thirtyDaysAgo)
            .GroupBy(r => DateOnly.FromDateTime(r.PeriodStart.DateTime))
            .Select(g => new DailySpendDto(g.Key, g.Sum(r => r.CostUsd)))
            .OrderBy(d => d.Date)
            .ToListAsync();

        var topModels = thisMonthRecords
            .GroupBy(r => new { r.Provider, r.Model })
            .Select(g => new ModelBreakdownDto(
                g.Key.Provider, g.Key.Model,
                g.Sum(r => r.CostUsd),
                g.Sum(r => r.InputTokens + r.OutputTokens)))
            .OrderByDescending(m => m.CostUsd)
            .Take(5)
            .ToList();

        return Results.Ok(new DashboardDto(
            thisMonthRecords.Sum(r => r.CostUsd),
            lastMonthRecords.Sum(r => r.CostUsd),
            byProvider,
            dailyTrend,
            topModels));
    }

    static Guid GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? user.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException();
        return Guid.Parse(sub);
    }
}