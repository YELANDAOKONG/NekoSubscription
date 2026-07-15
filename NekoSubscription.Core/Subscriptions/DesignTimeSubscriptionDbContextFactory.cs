using System.IO;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

using NekoSubscription.Core.Configuration;

namespace NekoSubscription.Core.Subscriptions;

public sealed class DesignTimeSubscriptionDbContextFactory : IDesignTimeDbContextFactory<SubscriptionDbContext>
{
    public SubscriptionDbContext CreateDbContext(string[] args)
    {
        var pathsProvider = new ApplicationStoragePathsProvider();
        var paths = pathsProvider.GetPaths();
        Directory.CreateDirectory(paths.ApplicationDataDirectory);
        var options = SubscriptionDbContextOptions.Create(paths.DataDatabasePath);

        return new SubscriptionDbContext(options);
    }
}
