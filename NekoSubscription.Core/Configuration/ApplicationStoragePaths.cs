namespace NekoSubscription.Core.Configuration;

public sealed record ApplicationStoragePaths(
    string DataRootDirectory,
    string ApplicationDataDirectory,
    string ConfigurationDatabasePath,
    string DataDatabasePath);
