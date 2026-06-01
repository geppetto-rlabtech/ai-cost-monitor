using AiCostMonitor.Core.Interfaces;
using System.Security.Claims;

namespace AiCostMonitor.Api.Endpoints;

public static class SyncEndpoints
{
    public static void MapSyncEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sync").RequireAuthorization();

        group.MapPost("/", SyncAll);
        group.MapPost("/{provider}", SyncProvider);
    }

    static async Task<IResult> SyncAll(ClaimsPrincipal user, ISyncService sync)
    {
        var userId = GetUserId(user);
        // Fire and forget — sync runs in background
        _ = Task.Run(() => sync.SyncUserAsync(userId));
        return Results.Accepted("/api/sync", new { message = "Sync started" });
    }

    static async Task<IResult> SyncProvider(string provider, ClaimsPrincipal user, ISyncService sync)
    {
        var userId = GetUserId(user);
        _ = Task.Run(() => sync.SyncUserAsync(userId, provider));
        return Results.Accepted($"/api/sync/{provider}", new { message = $"Sync started for {provider}" });
    }

    static Guid GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? user.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException();
        return Guid.Parse(sub);
    }
}