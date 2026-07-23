namespace NekoSubscription.Core.DataManagement;

public sealed record CsvImportResult(
    int ImportedSubscriptionCount,
    int CreatedPaymentProfileCount);
