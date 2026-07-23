using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NekoSubscription.Core.DataManagement;

public interface IDataManagementService : IDisposable
{
    Task CreateBackupAsync(
        Stream destination,
        CancellationToken cancellationToken = default);

    Task<CsvImportPreview> PreviewSubscriptionCsvAsync(
        Stream source,
        CancellationToken cancellationToken = default);

    Task<CsvImportResult> ImportSubscriptionCsvAsync(
        Stream source,
        CancellationToken cancellationToken = default);

    Task<DataClearResult> ClearSubscriptionDataAsync(
        CancellationToken cancellationToken = default);
}
