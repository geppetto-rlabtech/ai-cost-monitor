using AiCostMonitor.Api.Data;
using AiCostMonitor.Core.Entities;
using AiCostMonitor.Core.Interfaces;
using AiCostMonitor.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AiCostMonitor.Api.Endpoints;

public static class ProviderKeyEndpoints
{
    public static void MapProviderKeyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/providers").RequireAuthorization();

        group.MapGet("/", GetKeys);
        group.MapPost("/{provider}/keys", AddKey);
        group.MapDelete("/{provider}/keys/{id:guid}", DeleteKey);
        group.MapPost("/{provider}/keys/{id:guid}/test", TestKey);
    }

    static async Task<IResult> GetKeys(ClaimsPrincipal user, AppDbContext db)
    {
        var userId = GetUserId(user);
        var keys = await db.ProviderKeys
            .Where(k => k.UserId == userId)
            .OrderBy(k => k.Provider).ThenBy(k => k.Label)
            .ToListAsync();

        var keyDtos = keys.Select(k => new ProviderKeyDto(
                k.Id, k.Provider, k.Label,
                "****" + k.EncryptedApiKey[^4..],
                k.IsActive, k.CreatedAt, k.LastSyncedAt))
            .ToList();
        return Results.Ok(keyDtos);
    }

    static async Task<IResult> AddKey(
        string provider,
        [FromBody] AddProviderKeyRequest req,
        ClaimsPrincipal user,
        AppDbContext db,
        IEncryptionService encryption,
        IEnumerable<IProviderAdapter> adapters)
    {
        var userId = GetUserId(user);

        // Ensure user exists
        if (!await db.Users.AnyAsync(u => u.Id == userId))
        {
            db.Users.Add(new User
            {
                Id = userId,
                Email = user.FindFirstValue(ClaimTypes.Email) ?? "",
                DisplayName = user.FindFirstValue("name") ?? ""
            });
        }

        var encryptedKey = encryption.Encrypt(req.ApiKey);

        var key = new UserProviderKey
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = provider.ToLowerInvariant(),
            EncryptedApiKey = encryptedKey,
            Label = req.Label
        };

        db.ProviderKeys.Add(key);
        await db.SaveChangesAsync();

        return Results.Created($"/api/providers/{provider}/keys/{key.Id}",
            new ProviderKeyDto(key.Id, key.Provider, key.Label,
                "****" + encryptedKey[^4..], key.IsActive, key.CreatedAt, null));
    }

    static async Task<IResult> DeleteKey(
        string provider, Guid id,
        ClaimsPrincipal user, AppDbContext db)
    {
        var userId = GetUserId(user);
        var key = await db.ProviderKeys
            .FirstOrDefaultAsync(k => k.Id == id && k.UserId == userId);

        if (key is null) return Results.NotFound();

        db.ProviderKeys.Remove(key);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    static async Task<IResult> TestKey(
        string provider, Guid id,
        ClaimsPrincipal user, AppDbContext db,
        IEncryptionService encryption,
        IEnumerable<IProviderAdapter> adapters)
    {
        var userId = GetUserId(user);
        var key = await db.ProviderKeys
            .FirstOrDefaultAsync(k => k.Id == id && k.UserId == userId);

        if (key is null) return Results.NotFound();

        var adapter = adapters.FirstOrDefault(a => a.ProviderName == provider);
        if (adapter is null) return Results.BadRequest(new { error = "Unsupported provider" });

        var plainKey = encryption.Decrypt(key.EncryptedApiKey);
        var ok = await adapter.TestKeyAsync(plainKey);
        return Results.Ok(new { success = ok });
    }

    static Guid GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? user.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException();
        return Guid.Parse(sub);
    }
}