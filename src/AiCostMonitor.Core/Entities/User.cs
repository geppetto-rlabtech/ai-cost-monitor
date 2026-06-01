namespace AiCostMonitor.Core.Entities;

public class User
{
    public Guid Id { get; set; }         // = Keycloak sub claim
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<UserProviderKey> ProviderKeys { get; set; } = [];
}