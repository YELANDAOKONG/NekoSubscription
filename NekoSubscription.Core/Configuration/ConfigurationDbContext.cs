using Microsoft.EntityFrameworkCore;

namespace NekoSubscription.Core.Configuration;

public sealed class ConfigurationDbContext(DbContextOptions<ConfigurationDbContext> options) : DbContext(options)
{
    public DbSet<ApplicationSettings> Settings => Set<ApplicationSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var settings = modelBuilder.Entity<ApplicationSettings>();
        settings.ToTable("ApplicationSettings");
        settings.HasKey(setting => setting.Id);
        settings.Property(setting => setting.Id).ValueGeneratedNever();
        settings.Property(setting => setting.Theme).IsRequired();
        settings.Property(setting => setting.CultureName)
            .HasMaxLength(ApplicationSettings.MaximumCultureNameLength);
        settings.Property(setting => setting.MinimumLogLevel).IsRequired();
        settings.Property(setting => setting.UpdatedAtUtc).IsRequired();
    }
}
