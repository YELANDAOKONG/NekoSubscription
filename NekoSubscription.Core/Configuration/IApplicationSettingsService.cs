using System;
using System.Threading;
using System.Threading.Tasks;

namespace NekoSubscription.Core.Configuration;

public interface IApplicationSettingsService : IDisposable
{
    Task<ApplicationSettings> GetAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken = default);
}
