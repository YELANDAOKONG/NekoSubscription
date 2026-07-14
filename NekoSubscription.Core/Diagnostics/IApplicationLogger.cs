using System;
using System.Threading;
using System.Threading.Tasks;

namespace NekoSubscription.Core.Diagnostics;

public interface IApplicationLogger : IDisposable
{
    ApplicationLogLevel MinimumLevel { get; }

    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task SetMinimumLevelAsync(ApplicationLogLevel minimumLevel, CancellationToken cancellationToken = default);

    void Write(ApplicationLogLevel level, string message, Exception? exception = null);
}
