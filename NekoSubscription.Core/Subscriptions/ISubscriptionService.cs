using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Core.Subscriptions;

public interface ISubscriptionService : IDisposable
{
    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Subscription>> GetSubscriptionsAsync(
        SubscriptionQuery? query = null,
        CancellationToken cancellationToken = default);

    Task<Subscription?> GetSubscriptionAsync(
        Guid subscriptionId,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    Task AddSubscriptionAsync(
        Subscription subscription,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateSubscriptionAsync(
        Guid subscriptionId,
        Action<Subscription, DateTimeOffset> update,
        CancellationToken cancellationToken = default);

    Task<bool> ArchiveSubscriptionAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default);

    Task<bool> RestoreSubscriptionFromArchiveAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteSubscriptionAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default);

    Task<bool> RestoreDeletedSubscriptionAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default);

    Task<bool> SetPaymentProfileAsync(
        Guid subscriptionId,
        Guid? paymentProfileId,
        CancellationToken cancellationToken = default);

    Task<bool> SetIncludedWithSubscriptionAsync(
        Guid subscriptionId,
        Guid? sourceSubscriptionId,
        CancellationToken cancellationToken = default);

    Task<bool> SetTagsAsync(
        Guid subscriptionId,
        IReadOnlyCollection<Guid> tagIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentProfile>> GetPaymentProfilesAsync(
        bool includeArchived = false,
        CancellationToken cancellationToken = default);

    Task AddPaymentProfileAsync(
        PaymentProfile paymentProfile,
        CancellationToken cancellationToken = default);

    Task<bool> UpdatePaymentProfileAsync(
        Guid paymentProfileId,
        Action<PaymentProfile, DateTimeOffset> update,
        CancellationToken cancellationToken = default);

    Task<bool> ArchivePaymentProfileAsync(
        Guid paymentProfileId,
        CancellationToken cancellationToken = default);

    Task<bool> RestorePaymentProfileFromArchiveAsync(
        Guid paymentProfileId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Tag>> GetTagsAsync(CancellationToken cancellationToken = default);

    Task AddTagAsync(Tag tag, CancellationToken cancellationToken = default);

    Task<bool> RenameTagAsync(
        Guid tagId,
        string name,
        CancellationToken cancellationToken = default);
}
