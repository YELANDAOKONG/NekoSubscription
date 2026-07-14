using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NekoSubscription.Core.Configuration;

public sealed class DesignTimeConfigurationDbContextFactory : IDesignTimeDbContextFactory<ConfigurationDbContext>
{
    public ConfigurationDbContext CreateDbContext(string[] args)
    {
        var pathsProvider = new ApplicationStoragePathsProvider();
        var paths = pathsProvider.GetPaths();
        var options = ConfigurationDbContextOptions.Create(paths.ConfigurationDatabasePath);

        return new ConfigurationDbContext(options);
    }
}
