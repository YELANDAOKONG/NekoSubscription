using System;

using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Entities.Tests.Subscriptions;

public sealed class PaymentProfileTests
{
    [Theory]
    [InlineData(PaymentChannel.AppleAppStore)]
    [InlineData(PaymentChannel.GooglePlay)]
    [InlineData(PaymentChannel.PayPal)]
    public void Constructor_RequiresAccountForAccountBasedChannel(PaymentChannel channel)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new PaymentProfile("Primary", channel, null, null, null));

        Assert.Equal("accountIdentifier", exception.ParamName);
    }

    [Fact]
    public void Constructor_AllowsDirectChannelWithoutAccount()
    {
        var paymentProfile = new PaymentProfile(
            "Direct payment",
            PaymentChannel.Direct,
            null,
            "Example provider",
            null);

        Assert.Null(paymentProfile.AccountIdentifier);
        Assert.Equal(PaymentChannel.Direct, paymentProfile.Channel);
    }

    [Fact]
    public void ArchiveAndRestore_PreserveProfileIdentity()
    {
        var paymentProfile = new PaymentProfile(
            "Google account",
            PaymentChannel.GooglePlay,
            "account@example.com",
            "Google",
            null);
        var profileId = paymentProfile.Id;
        var archivedAtUtc = DateTimeOffset.UtcNow;

        paymentProfile.Archive(archivedAtUtc);

        Assert.True(paymentProfile.IsArchived);
        Assert.Equal(archivedAtUtc, paymentProfile.ArchivedAtUtc);

        paymentProfile.RestoreFromArchive(archivedAtUtc.AddMinutes(1));

        Assert.False(paymentProfile.IsArchived);
        Assert.Null(paymentProfile.ArchivedAtUtc);
        Assert.Equal(profileId, paymentProfile.Id);
    }
}
