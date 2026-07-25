using System;
using System.IO;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using NekoSubscription.Core.Configuration.CompiledModels;

namespace NekoSubscription.Core.Configuration;

public static class ConfigurationDbContextOptions
{
    public static DbContextOptions<ConfigurationDbContext> Create(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.GetFullPath(databasePath),
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        return new DbContextOptionsBuilder<ConfigurationDbContext>()
            .UseSqlite(connectionString)
            .UseModel(ConfigurationDbContextModel.Instance)
            .Options;
    }
}
