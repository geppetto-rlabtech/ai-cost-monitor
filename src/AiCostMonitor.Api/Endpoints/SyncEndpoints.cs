using AiCostMonitor.Api.Extensions;
using AiCostMonitor.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
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

    static IResult SyncAll(ClaimsPrincipal user, IServiceProvider services)
    {
        var userId = user.GetUserId();
        // Fire and forget — sync runs in background with a fresh scope
        _ = Task.Run(async () =>
        {
            using var scope = services.CreateScope();
            var sync = scope.ServiceProvider.GetRequiredService<ISyncService>();
            await sync.SyncUserAsync(userId);
        });
        return Results.Accepted("/api/sync", new { message = "Sync started" });
    }

    static IResult SyncProvider(string provider, ClaimsPrincipal user, IServiceProvider services)
    {
        var userId = user.GetUserId();
        _ = Task.Run(async () =>
        {
            using var scope = services.CreateScope();
            var sync = scope.ServiceProvider.GetRequiredService<ISyncService>();
            await sync.SyncUserAsync(userId, provider);
        });
        return Results.Accepted($"/api/sync/{provider}", new { message = $"Sync started for {provider}" });
    }
}
