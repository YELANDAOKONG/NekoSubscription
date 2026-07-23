using System.Collections.Generic;
using System.Linq;

namespace NekoSubscription.Core.DataManagement;

public sealed record CsvImportPreview(
    int TotalRowCount,
    int ValidRowCount,
    IReadOnlyList<CsvImportIssue> Issues)
{
    public int ErrorCount => Issues.Count(issue => issue.Severity == CsvImportIssueSeverity.Error);

    public int WarningCount => Issues.Count(issue => issue.Severity == CsvImportIssueSeverity.Warning);

    public bool CanImport => ValidRowCount > 0 && ErrorCount == 0;
}
