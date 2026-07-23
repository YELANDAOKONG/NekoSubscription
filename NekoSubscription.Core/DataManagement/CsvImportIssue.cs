namespace NekoSubscription.Core.DataManagement;

public sealed record CsvImportIssue(
    int RowNumber,
    CsvImportIssueSeverity Severity,
    CsvImportIssueCode Code);
