using Microsoft.EntityFrameworkCore;

namespace NekoSubscription.Core.Configuration;

public sealed class ConfigurationDbContext(DbContextOptions<ConfigurationDbContext> options) : DbContext(options)
{
    public DbSet<SettingEntry> Settings => Set<SettingEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var settings = modelBuilder.Entity<SettingEntry>();
        settings.ToTable("Settings");
        settings.HasKey(setting => setting.Key);
        settings.Property(setting => setting.Key).HasMaxLength(SettingEntry.MaximumKeyLength);
        settings.Property(setting => setting.Value).IsRequired();
        settings.Property(setting => setting.UpdatedAtUtc).IsRequired();
    }
}
