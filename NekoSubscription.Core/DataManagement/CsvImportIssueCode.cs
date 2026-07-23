namespace NekoSubscription.Core.DataManagement;

public enum CsvImportIssueCode
{
    MalformedCsv,
    InvalidColumnCount,
    MissingProvider,
    InvalidAmountOrCurrency,
    InvalidBillingPeriod,
    InvalidDate,
    InvalidDateOrder,
    InvalidSubscriptionMarker,
    InvalidPaymentChannel,
    MissingPaymentAccount,
    DuplicateRow
}
