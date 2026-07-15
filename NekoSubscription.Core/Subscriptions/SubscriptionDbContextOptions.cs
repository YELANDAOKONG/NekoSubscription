using System;
using System.IO;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace NekoSubscription.Core.Subscriptions;

public static class SubscriptionDbContextOptions
{
    public static DbContextOptions<SubscriptionDbContext> Create(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.GetFullPath(databasePath),
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        return new DbContextOptionsBuilder<SubscriptionDbContext>()
            .UseSqlite(connectionString)
            .Options;
    }
}
