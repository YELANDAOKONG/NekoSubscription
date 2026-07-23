using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NekoSubscription.Core.Configuration;
using NekoSubscription.Core.DataManagement;
using NekoSubscription.Core.Subscriptions;
using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Core.Tests.DataManagement;

public sealed class DataManagementServiceTests : IDisposable
{
    private readonly DataManagementService _dataManagementService;
    private readonly string _temporaryDirectory;
    private readonly SubscriptionService _subscriptionService;

    public DataManagementServiceTests()
    {
        _temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            $"NekoSubscriptionDataManagementTests-{Guid.NewGuid():N}");
        var applicationDataDirectory = Path.Combine(_temporaryDirectory, "application");
        var paths = new ApplicationStoragePaths(
            _temporaryDirectory,
            applicationDataDirectory,
            Path.Combine(applicationDataDirectory, "configuration.db"),
            Path.Combine(applicationDataDirectory, "data.db"),
            Path.Combine(applicationDataDirectory, "logs"),
            Path.Combine(applicationDataDirectory, "crash-reports"));

        _dataManagementService = new DataManagementService(paths);
        _subscriptionService = new SubscriptionService(paths);
    }

    [Fact]
    public async Task PreviewSubscriptionCsvAsync_IgnoresHeaderTextAndUsesColumnOrder()
    {
        var chineseHeaderCsv = CreateCsv(
            "服务名称,会员名称,账户标识,周期费用,币种,付费周期,生效日期,失效日期,剩余时效,订阅标记,付款方式,付款账户,备注",
            "Provider,Plan,account,10.00,USD,M,2026-01-01,2026-02-01,,TRUE,DIRECT,-,Note");
        var englishHeaderCsv = CreateCsv(
            "Provider,Plan,Account,Amount,Currency,Period,Start,Next,Remaining,Active,Channel,Payment account,Notes",
            "Provider,Plan,account,10.00,USD,M,2026-01-01,2026-02-01,,TRUE,DIRECT,-,Note");

        var chinesePreview = await PreviewAsync(chineseHeaderCsv);
        var englishPreview = await PreviewAsync(englishHeaderCsv);

        Assert.True(chinesePreview.CanImport);
        Assert.True(englishPreview.CanImport);
        Assert.Equal(chinesePreview.ValidRowCount, englishPreview.ValidRowCount);
        Assert.Equal(1, chinesePreview.ValidRowCount);
    }

    [Fact]
    public async Task ImportSubscriptionCsvAsync_ImportsMappingsAndMissingMoney()
    {
        var csv = CreateCsv(
            "Header 1,Header 2,Header 3,Header 4,Header 5,Header 6,Header 7,Header 8,Header 9,Header 10,Header 11,Header 12,Header 13",
            "Provider,Plan,account,\"1,624.00\",JPY,M,7/3/2025,7/20/2026,,TRUE,GOOGLE,payer,Imported note",
            "Ente,,,,,M,,,,TRUE,DIRECT,-,");

        await using var source = CreateStream(csv);
        var result = await _dataManagementService.ImportSubscriptionCsvAsync(source);
        var subscriptions = await _subscriptionService.GetSubscriptionsAsync();

        Assert.Equal(2, result.ImportedSubscriptionCount);
        Assert.Equal(1, result.CreatedPaymentProfileCount);
        Assert.Collection(
            subscriptions.OrderBy(subscription => subscription.ProviderName),
            ente =>
            {
                Assert.Equal("Ente", ente.ProviderName);
                Assert.Equal("Ente", ente.ServiceName);
                Assert.Equal(0m, ente.BillingAmount.Amount);
                Assert.Equal("XXX", ente.BillingAmount.CurrencyCode);
            },
            provider =>
            {
                Assert.Equal("Provider", provider.ProviderName);
                Assert.Equal(1624m, provider.BillingAmount.Amount);
                Assert.Equal(new DateOnly(2026, 7, 20), provider.BillingSchedule.NextBillingOn);
                Assert.Equal(PaymentChannel.GooglePlay, provider.PaymentProfile?.Channel);
                Assert.Equal("payer", provider.PaymentProfile?.AccountIdentifier);
                Assert.Equal("Imported note", provider.Notes);
            });
    }

    [Fact]
    public async Task ImportSubscriptionCsvAsync_DoesNotWriteWhenAnyRowIsInvalid()
    {
        var csv = CreateCsv(
            "A,B,C,D,E,F,G,H,I,J,K,L,M",
            "Provider,Plan,account,10.00,USD,M,2026-01-01,2026-02-01,,TRUE,DIRECT,-,",
            "Provider,Plan,account,10.00,USD,INVALID,2026-01-01,2026-02-01,,TRUE,DIRECT,-,");

        await using var source = CreateStream(csv);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => _dataManagementService.ImportSubscriptionCsvAsync(source));
        Assert.Empty(await _subscriptionService.GetSubscriptionsAsync());
    }

    [Fact]
    public async Task ClearSubscriptionDataAsync_DeletesSubscriptionsProfilesAndTags()
    {
        var csv = CreateCsv(
            "A,B,C,D,E,F,G,H,I,J,K,L,M",
            "Provider,Plan,account,10.00,USD,M,2026-01-01,2026-02-01,,TRUE,GOOGLE,payer,");
        await using (var source = CreateStream(csv))
        {
            await _dataManagementService.ImportSubscriptionCsvAsync(source);
        }

        var tag = new Tag("Imported");
        await _subscriptionService.AddTagAsync(tag);
        var subscription = Assert.Single(await _subscriptionService.GetSubscriptionsAsync());
        Assert.True(await _subscriptionService.SetTagsAsync(subscription.Id, [tag.Id]));

        var result = await _dataManagementService.ClearSubscriptionDataAsync();

        Assert.Equal(1, result.DeletedSubscriptionCount);
        Assert.Equal(1, result.DeletedPaymentProfileCount);
        Assert.Equal(1, result.DeletedTagCount);
        Assert.Empty(await _subscriptionService.GetSubscriptionsAsync(
            new SubscriptionQuery(IncludeArchived: true, IncludeDeleted: true)));
        Assert.Empty(await _subscriptionService.GetPaymentProfilesAsync(includeArchived: true));
        Assert.Empty(await _subscriptionService.GetTagsAsync());
    }

    [Fact]
    public async Task CreateBackupAsync_WritesDatabaseSnapshotsAndManifest()
    {
        var csv = CreateCsv(
            "A,B,C,D,E,F,G,H,I,J,K,L,M",
            "Provider,Plan,account,10.00,USD,M,2026-01-01,2026-02-01,,TRUE,DIRECT,-,");
        await using (var source = CreateStream(csv))
        {
            await _dataManagementService.ImportSubscriptionCsvAsync(source);
        }

        await using var backup = new MemoryStream();
        await _dataManagementService.CreateBackupAsync(backup);
        backup.Position = 0;
        using var archive = new ZipArchive(backup, ZipArchiveMode.Read, leaveOpen: true);

        Assert.NotNull(archive.GetEntry("manifest.json"));
        Assert.True(archive.GetEntry("data.db")?.Length > 0);
        Assert.True(archive.GetEntry("configuration.db")?.Length > 0);
    }

    public void Dispose()
    {
        _subscriptionService.Dispose();
        _dataManagementService.Dispose();

        if (Directory.Exists(_temporaryDirectory))
        {
            Directory.Delete(_temporaryDirectory, recursive: true);
        }
    }

    private async Task<CsvImportPreview> PreviewAsync(string csv)
    {
        await using var source = CreateStream(csv);
        return await _dataManagementService.PreviewSubscriptionCsvAsync(source);
    }

    private static MemoryStream CreateStream(string csv) =>
        new(Encoding.UTF8.GetBytes(csv), writable: false);

    private static string CreateCsv(params string[] rows) =>
        string.Join(Environment.NewLine, rows);
}
