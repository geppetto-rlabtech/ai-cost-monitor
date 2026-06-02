using AiCostMonitor.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiCostMonitor.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserProviderKey> ProviderKeys => Set<UserProviderKey>();
    public DbSet<UsageRecord> UsageRecords => Set<UsageRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).ValueGeneratedNever(); // Keycloak sub = our PK
            e.HasMany(u => u.ProviderKeys).WithOne(k => k.User).HasForeignKey(k => k.UserId);
        });

        modelBuilder.Entity<UserProviderKey>(e =>
        {
            e.HasKey(k => k.Id);
            e.HasIndex(k => new { k.UserId, k.Provider, k.Label });
            e.HasMany(k => k.UsageRecords).WithOne(r => r.ProviderKey).HasForeignKey(r => r.ProviderKeyId);
        });

        modelBuilder.Entity<UsageRecord>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => new { r.UserId, r.PeriodStart });
            e.HasIndex(r => new { r.Provider, r.Model });
            // Prevent duplicate records for the same key/model/period
            e.HasIndex(r => new { r.ProviderKeyId, r.Model, r.PeriodStart }).IsUnique();
            e.Property(r => r.CostUsd).HasColumnType("numeric(18, 8)");
        });
    }
}