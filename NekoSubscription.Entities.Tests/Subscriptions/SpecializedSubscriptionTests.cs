using System;

using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Entities.Tests.Subscriptions;

public sealed class SpecializedSubscriptionTests
{
    [Fact]
    public void DomainSubscription_NormalizesDomainName()
    {
        var subscription = new DomainSubscription(
            "Cloudflare",
            "Domain registration",
            null,
            "account@example.com",
            CreateMoney(),
            CreateSchedule(),
            " Example.COM ",
            new DateOnly(2025, 1, 1),
            new DateOnly(2027, 1, 1));

        Assert.Equal("example.com", subscription.DomainName);
        Assert.Equal("Cloudflare", subscription.ProviderName);
        Assert.Equal(SubscriptionCategory.Domain, subscription.Category);
    }

    [Fact]
    public void DomainSubscription_RejectsExpirationBeforeRegistration()
    {
        Assert.Throws<ArgumentException>(() => new DomainSubscription(
            "Cloudflare",
            "Domain registration",
            null,
            null,
            CreateMoney(),
            CreateSchedule(),
            "example.com",
            new DateOnly(2026, 1, 1),
            new DateOnly(2025, 1, 1)));
    }

    [Fact]
    public void CloudServiceSubscription_StoresUsageBasedEstimate()
    {
        var subscription = new CloudServiceSubscription(
            "Amazon Web Services",
            "EC2",
            null,
            "cloud@example.com",
            CreateMoney(),
            CreateSchedule(),
            CloudBillingMode.UsageBasedEstimate,
            "tenant-1",
            "project-1");

        Assert.Equal(CloudBillingMode.UsageBasedEstimate, subscription.BillingMode);
        Assert.Equal("tenant-1", subscription.TenantIdentifier);
        Assert.Equal("project-1", subscription.ProjectIdentifier);
    }

    private static Money CreateMoney() => new(10m, "USD", CurrencyKind.Iso4217);

    private static BillingSchedule CreateSchedule()
    {
        return new BillingSchedule(
            BillingCadence.Recurring,
            BillingIntervalUnit.Year,
            1,
            null,
            new DateOnly(2027, 1, 1),
            null,
            true);
    }
}
