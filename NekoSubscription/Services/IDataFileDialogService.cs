using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NekoSubscription.Services;

public interface IDataFileDialogService
{
    Task<Stream?> OpenCsvFileAsync(
        string title,
        CancellationToken cancellationToken = default);

    Task<Stream?> CreateBackupFileAsync(
        string title,
        string fileTypeName,
        CancellationToken cancellationToken = default);
}
