using System.Security.Claims;

namespace AiCostMonitor.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? user.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException("User identifier claim not found.");
        return Guid.TryParse(sub, out var id) ? id
            : throw new UnauthorizedAccessException("Invalid user identifier format.");
    }
}
