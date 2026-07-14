using System;
using System.IO;

namespace NekoSubscription.Core.Configuration;

public sealed class ApplicationStoragePathsProvider : IApplicationStoragePathsProvider
{
    public const string DataRootEnvironmentVariableName = "NEKO_SUBSCRIPTION_DATA_ROOT";

    private const string ApplicationDirectoryName = "NekoSubscription";
    private const string ConfigurationDatabaseFileName = "configuration.db";
    private const string DataDatabaseFileName = "data.db";
    private const string LogsDirectoryName = "logs";
    private const string LatestLogFileName = "latest.log";
    private const string CrashReportsDirectoryName = "crashes";

    public ApplicationStoragePaths GetPaths()
    {
        var dataRootDirectory = GetDataRootDirectory();
        var applicationDataDirectory = Path.Combine(dataRootDirectory, ApplicationDirectoryName);
        var logsDirectory = Path.Combine(applicationDataDirectory, LogsDirectoryName);

        return new ApplicationStoragePaths(
            dataRootDirectory,
            applicationDataDirectory,
            Path.Combine(applicationDataDirectory, ConfigurationDatabaseFileName),
            Path.Combine(applicationDataDirectory, DataDatabaseFileName),
            logsDirectory,
            Path.Combine(logsDirectory, LatestLogFileName),
            Path.Combine(applicationDataDirectory, CrashReportsDirectoryName));
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
