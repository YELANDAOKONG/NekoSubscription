using System;
using System.IO;

namespace NekoSubscription.Core.Configuration;

public sealed class ApplicationStoragePathsProvider : IApplicationStoragePathsProvider
{
    public const string DataRootEnvironmentVariableName = "NEKO_SUBSCRIPTION_DATA_ROOT";

    private const string ApplicationDirectoryName = "NekoSubscription";
    private const string ConfigurationDatabaseFileName = "configuration.db";
    private const string DataDatabaseFileName = "data.db";

    public ApplicationStoragePaths GetPaths()
    {
        var dataRootDirectory = GetDataRootDirectory();
        var applicationDataDirectory = Path.Combine(dataRootDirectory, ApplicationDirectoryName);

        return new ApplicationStoragePaths(
            dataRootDirectory,
            applicationDataDirectory,
            Path.Combine(applicationDataDirectory, ConfigurationDatabaseFileName),
            Path.Combine(applicationDataDirectory, DataDatabaseFileName));
    }

    private static string GetDataRootDirectory()
    {
        var redirectedDataRoot = Environment.GetEnvironmentVariable(DataRootEnvironmentVariableName);

        if (!string.IsNullOrWhiteSpace(redirectedDataRoot))
        {
            return Path.GetFullPath(redirectedDataRoot);
        }

        var systemDataRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        if (string.IsNullOrWhiteSpace(systemDataRoot))
        {
            throw new InvalidOperationException("The system local application data directory is unavailable.");
        }

        return Path.GetFullPath(systemDataRoot);
    }
}
