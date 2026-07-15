using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using NekoSubscription.Core.Configuration;
using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Core.Subscriptions;

public sealed class SubscriptionService : ISubscriptionService
{
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private readonly DbContextOptions<SubscriptionDbContext> _options;
    private readonly ApplicationStoragePaths _paths;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private bool _isDisposed;
    private bool _isInitialized;

    public SubscriptionService(IApplicationStoragePathsProvider pathsProvider)
    {
        ArgumentNullException.ThrowIfNull(pathsProvider);

        _paths = pathsProvider.GetPaths();
        _options = SubscriptionDbContextOptions.Create(_paths.DataDatabasePath);
    }

    public SubscriptionService(ApplicationStoragePaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        _paths = paths;
        _options = SubscriptionDbContextOptions.Create(_paths.DataDatabasePath);
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        EnsureInitializedAsync(cancellationToken);

    public async Task<IReadOnlyList<Subscription>> GetSubscriptionsAsync(
        SubscriptionQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        var normalizedQuery = ValidateAndNormalizeQuery(query ?? new SubscriptionQuery());
        await using var context = CreateContext();
        IQueryable<Subscription> subscriptions = context.Subscriptions;

        if (normalizedQuery.IncludeDeleted)
        {
            subscriptions = subscriptions.IgnoreQueryFilters();
        }

        if (!normalizedQuery.IncludeArchived)
        {
            subscriptions = subscriptions.Where(subscription => !subscription.IsArchived);
        }

        if (normalizedQuery.Category is { } category)
        {
            subscriptions = subscriptions.Where(subscription => subscription.Category == category);
        }

        if (normalizedQuery.ConfirmationStatus is { } confirmationStatus)
        {
            subscriptions = subscriptions.Where(
                subscription => subscription.ConfirmationStatus == confirmationStatus);
        }

        if (normalizedQuery.LifecycleStatus is { } lifecycleStatus)
        {
            subscriptions = subscriptions.Where(
                subscription => subscription.LifecycleStatus == lifecycleStatus);
        }

        if (normalizedQuery.CurrencyCode is { } currencyCode)
        {
            subscriptions = subscriptions.Where(
                subscription => subscription.BillingAmount.CurrencyCode == currencyCode);
        }

        if (normalizedQuery.AccountName is { } accountName)
        {
            subscriptions = subscriptions.Where(subscription => subscription.AccountName == accountName);
        }

        subscriptions = IncludeAggregate(subscriptions)
            .OrderBy(subscription => subscription.ProviderName)
            .ThenBy(subscription => subscription.ServiceName)
            .ThenBy(subscription => subscription.AccountName)
            .ThenBy(subscription => subscription.Id);

        var results = await subscriptions
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        await LoadCustomFieldsAsync(context, results, cancellationToken).ConfigureAwait(false);

        return results;
    }

    public async Task<Subscription?> GetSubscriptionAsync(
        Guid subscriptionId,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(subscriptionId, nameof(subscriptionId));
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await using var context = CreateContext();
        var subscription = await FindSubscriptionAsync(
                context,
                subscriptionId,
                includeDeleted,
                cancellationToken)
            .ConfigureAwait(false);

        if (subscription is not null)
        {
            await LoadCustomFieldsAsync(context, [subscription], cancellationToken).ConfigureAwait(false);
        }

        return subscription;
    }

    public async Task AddSubscriptionAsync(
        Subscription subscription,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        await ExecuteWriteAsync(
                async (context, operationCancellationToken) =>
                {
                    await ResolveExistingReferencesAsync(
                            context,
                            subscription,
                            operationCancellationToken)
                        .ConfigureAwait(false);
                    context.Subscriptions.Add(subscription);
                    await context.SaveChangesAsync(operationCancellationToken).ConfigureAwait(false);
                    return true;
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<bool> UpdateSubscriptionAsync(
        Guid subscriptionId,
        Action<Subscription, DateTimeOffset> update,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(subscriptionId, nameof(subscriptionId));
        ArgumentNullException.ThrowIfNull(update);

        return ExecuteSubscriptionMutationAsync(
            subscriptionId,
            (subscription, changedAtUtc) => update(subscription, changedAtUtc),
            cancellationToken);
    }

    public Task<bool> ArchiveSubscriptionAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(subscriptionId, nameof(subscriptionId));
        return ExecuteSubscriptionMutationAsync(
            subscriptionId,
            (subscription, changedAtUtc) => subscription.Archive(changedAtUtc),
            cancellationToken);
    }

    public Task<bool> RestoreSubscriptionFromArchiveAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(subscriptionId, nameof(subscriptionId));
        return ExecuteSubscriptionMutationAsync(
            subscriptionId,
            (subscription, changedAtUtc) => subscription.RestoreFromArchive(changedAtUtc),
            cancellationToken);
    }

    public Task<bool> SoftDeleteSubscriptionAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(subscriptionId, nameof(subscriptionId));
        return ExecuteSubscriptionMutationAsync(
            subscriptionId,
            (subscription, changedAtUtc) => subscription.SoftDelete(changedAtUtc),
            cancellationToken);
    }

    public Task<bool> RestoreDeletedSubscriptionAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(subscriptionId, nameof(subscriptionId));
        return ExecuteSubscriptionMutationAsync(
            subscriptionId,
            (subscription, changedAtUtc) => subscription.RestoreDeleted(changedAtUtc),
            cancellationToken);
    }

    public async Task<bool> SetPaymentProfileAsync(
        Guid subscriptionId,
        Guid? paymentProfileId,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(subscriptionId, nameof(subscriptionId));
        ValidateOptionalIdentifier(paymentProfileId, nameof(paymentProfileId));

        return await ExecuteWriteAsync(
                async (context, operationCancellationToken) =>
                {
                    var subscription = await FindSubscriptionAsync(
                            context,
                            subscriptionId,
                            true,
                            operationCancellationToken)
                        .ConfigureAwait(false);
                    if (subscription is null)
                    {
                        return false;
                    }

                    PaymentProfile? paymentProfile = null;
                    if (paymentProfileId is { } selectedPaymentProfileId)
                    {
                        paymentProfile = await context.PaymentProfiles
                            .SingleOrDefaultAsync(
                                candidate => candidate.Id == selectedPaymentProfileId,
                                operationCancellationToken)
                            .ConfigureAwait(false);
                        if (paymentProfile is null)
                        {
                            return false;
                        }
                    }

                    subscription.SetPaymentProfile(paymentProfile, DateTimeOffset.UtcNow);
                    await context.SaveChangesAsync(operationCancellationToken).ConfigureAwait(false);
                    return true;
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> SetIncludedWithSubscriptionAsync(
        Guid subscriptionId,
        Guid? sourceSubscriptionId,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(subscriptionId, nameof(subscriptionId));
        ValidateOptionalIdentifier(sourceSubscriptionId, nameof(sourceSubscriptionId));

        return await ExecuteWriteAsync(
                async (context, operationCancellationToken) =>
                {
                    var subscription = await FindSubscriptionAsync(
                            context,
                            subscriptionId,
                            true,
                            operationCancellationToken)
                        .ConfigureAwait(false);
                    if (subscription is null)
                    {
                        return false;
                    }

                    Subscription? sourceSubscription = null;
                    if (sourceSubscriptionId is { } selectedSourceId)
                    {
                        await EnsureBundleRelationshipHasNoCycleAsync(
                                context,
                                subscriptionId,
                                selectedSourceId,
                                operationCancellationToken)
                            .ConfigureAwait(false);
                        sourceSubscription = await FindSubscriptionAsync(
                                context,
                                selectedSourceId,
                                true,
                                operationCancellationToken)
                            .ConfigureAwait(false);
                        if (sourceSubscription is null)
                        {
                            return false;
                        }
                    }

                    subscription.SetIncludedWithSubscription(sourceSubscription, DateTimeOffset.UtcNow);
                    await context.SaveChangesAsync(operationCancellationToken).ConfigureAwait(false);
                    return true;
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> SetTagsAsync(
        Guid subscriptionId,
        IReadOnlyCollection<Guid> tagIds,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(subscriptionId, nameof(subscriptionId));
        ArgumentNullException.ThrowIfNull(tagIds);

        if (tagIds.Any(tagId => tagId == Guid.Empty))
        {
            throw new ArgumentException("Tag identifiers cannot be empty.", nameof(tagIds));
        }

        var distinctTagIds = tagIds.Distinct().ToArray();
        return await ExecuteWriteAsync(
                async (context, operationCancellationToken) =>
                {
                    var subscription = await FindSubscriptionAsync(
                            context,
                            subscriptionId,
                            true,
                            operationCancellationToken)
                        .ConfigureAwait(false);
                    if (subscription is null)
                    {
                        return false;
                    }

                    var tags = await context.Tags
                        .Where(tag => distinctTagIds.Contains(tag.Id))
                        .OrderBy(tag => tag.Name)
                        .ToListAsync(operationCancellationToken)
                        .ConfigureAwait(false);
                    if (tags.Count != distinctTagIds.Length)
                    {
                        return false;
                    }

                    subscription.Tags.Clear();
                    foreach (var tag in tags)
                    {
                        subscription.Tags.Add(tag);
                    }

                    await context.SaveChangesAsync(operationCancellationToken).ConfigureAwait(false);
                    return true;
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<PaymentProfile>> GetPaymentProfilesAsync(
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await using var context = CreateContext();
        var paymentProfiles = context.PaymentProfiles.AsNoTracking();
        if (!includeArchived)
        {
            paymentProfiles = paymentProfiles.Where(paymentProfile => !paymentProfile.IsArchived);
        }

        return await paymentProfiles
            .OrderBy(paymentProfile => paymentProfile.DisplayName)
            .ThenBy(paymentProfile => paymentProfile.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddPaymentProfileAsync(
        PaymentProfile paymentProfile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paymentProfile);
        await ExecuteWriteAsync(
                async (context, operationCancellationToken) =>
                {
                    context.PaymentProfiles.Add(paymentProfile);
                    await context.SaveChangesAsync(operationCancellationToken).ConfigureAwait(false);
                    return true;
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> UpdatePaymentProfileAsync(
        Guid paymentProfileId,
        Action<PaymentProfile, DateTimeOffset> update,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(paymentProfileId, nameof(paymentProfileId));
        ArgumentNullException.ThrowIfNull(update);

        return await ExecuteWriteAsync(
                async (context, operationCancellationToken) =>
                {
                    var paymentProfile = await context.PaymentProfiles
                        .SingleOrDefaultAsync(
                            candidate => candidate.Id == paymentProfileId,
                            operationCancellationToken)
                        .ConfigureAwait(false);
                    if (paymentProfile is null)
                    {
                        return false;
                    }

                    update(paymentProfile, DateTimeOffset.UtcNow);
                    await context.SaveChangesAsync(operationCancellationToken).ConfigureAwait(false);
                    return true;
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<bool> ArchivePaymentProfileAsync(
        Guid paymentProfileId,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(paymentProfileId, nameof(paymentProfileId));
        return UpdatePaymentProfileAsync(
            paymentProfileId,
            (paymentProfile, changedAtUtc) => paymentProfile.Archive(changedAtUtc),
            cancellationToken);
    }

    public Task<bool> RestorePaymentProfileFromArchiveAsync(
        Guid paymentProfileId,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(paymentProfileId, nameof(paymentProfileId));
        return UpdatePaymentProfileAsync(
            paymentProfileId,
            (paymentProfile, changedAtUtc) => paymentProfile.RestoreFromArchive(changedAtUtc),
            cancellationToken);
    }

    public async Task<IReadOnlyList<Tag>> GetTagsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await using var context = CreateContext();
        return await context.Tags
            .AsNoTracking()
            .OrderBy(tag => tag.Name)
            .ThenBy(tag => tag.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddTagAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tag);
        await ExecuteWriteAsync(
                async (context, operationCancellationToken) =>
                {
                    context.Tags.Add(tag);
                    await context.SaveChangesAsync(operationCancellationToken).ConfigureAwait(false);
                    return true;
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> RenameTagAsync(
        Guid tagId,
        string name,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(tagId, nameof(tagId));

        return await ExecuteWriteAsync(
                async (context, operationCancellationToken) =>
                {
                    var tag = await context.Tags
                        .SingleOrDefaultAsync(candidate => candidate.Id == tagId, operationCancellationToken)
                        .ConfigureAwait(false);
                    if (tag is null)
                    {
                        return false;
                    }

                    tag.Rename(name);
                    await context.SaveChangesAsync(operationCancellationToken).ConfigureAwait(false);
                    return true;
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _initializationLock.Dispose();
        _writeLock.Dispose();
        _isDisposed = true;
    }

    private SubscriptionDbContext CreateContext() => new(_options);

    private async Task<bool> ExecuteSubscriptionMutationAsync(
        Guid subscriptionId,
        Action<Subscription, DateTimeOffset> update,
        CancellationToken cancellationToken)
    {
        return await ExecuteWriteAsync(
                async (context, operationCancellationToken) =>
                {
                    var subscription = await FindSubscriptionAsync(
                            context,
                            subscriptionId,
                            true,
                            operationCancellationToken)
                        .ConfigureAwait(false);
                    if (subscription is null)
                    {
                        return false;
                    }

                    await LoadCustomFieldsAsync(
                            context,
                            [subscription],
                            operationCancellationToken)
                        .ConfigureAwait(false);
                    update(subscription, DateTimeOffset.UtcNow);
                    await context.SaveChangesAsync(operationCancellationToken).ConfigureAwait(false);
                    return true;
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<T> ExecuteWriteAsync<T>(
        Func<SubscriptionDbContext, CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            await using var context = CreateContext();
            return await operation(context, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_isInitialized)
        {
            return;
        }

        await _initializationLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            if (_isInitialized)
            {
                return;
            }

            Directory.CreateDirectory(_paths.ApplicationDataDirectory);

            await using var context = CreateContext();
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            _isInitialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private static IQueryable<Subscription> IncludeAggregate(IQueryable<Subscription> subscriptions)
    {
        return subscriptions
            .Include(subscription => subscription.PaymentProfile)
            .Include(subscription => subscription.IncludedWithSubscription)
            .Include(subscription => subscription.Tags)
            .AsSplitQuery();
    }

    private static async Task<Subscription?> FindSubscriptionAsync(
        SubscriptionDbContext context,
        Guid subscriptionId,
        bool includeDeleted,
        CancellationToken cancellationToken)
    {
        IQueryable<Subscription> subscriptions = context.Subscriptions;
        if (includeDeleted)
        {
            subscriptions = subscriptions.IgnoreQueryFilters();
        }

        return await IncludeAggregate(subscriptions)
            .SingleOrDefaultAsync(
                subscription => subscription.Id == subscriptionId,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task LoadCustomFieldsAsync(
        SubscriptionDbContext context,
        IReadOnlyCollection<Subscription> subscriptions,
        CancellationToken cancellationToken)
    {
        var customSubscriptionIds = subscriptions
            .OfType<CustomSubscription>()
            .Select(subscription => subscription.Id)
            .ToArray();
        if (customSubscriptionIds.Length == 0)
        {
            return;
        }

        await context.CustomFields
            .Where(field => customSubscriptionIds.Contains(field.CustomSubscriptionId))
            .OrderBy(field => field.SortOrder)
            .ThenBy(field => field.Id)
            .LoadAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task ResolveExistingReferencesAsync(
        SubscriptionDbContext context,
        Subscription subscription,
        CancellationToken cancellationToken)
    {
        var unchangedAtUtc = subscription.UpdatedAtUtc;

        if (subscription.PaymentProfile is { } paymentProfile)
        {
            var persistedPaymentProfile = await context.PaymentProfiles
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == paymentProfile.Id,
                    cancellationToken)
                .ConfigureAwait(false);
            if (persistedPaymentProfile is not null)
            {
                subscription.SetPaymentProfile(persistedPaymentProfile, unchangedAtUtc);
            }
        }

        if (subscription.IncludedWithSubscription is { } sourceSubscription)
        {
            var persistedSource = await context.Subscriptions
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == sourceSubscription.Id,
                    cancellationToken)
                .ConfigureAwait(false);
            if (persistedSource is not null)
            {
                subscription.SetIncludedWithSubscription(persistedSource, unchangedAtUtc);
            }
        }

        if (subscription.Tags.Count == 0)
        {
            return;
        }

        var suppliedTags = subscription.Tags.ToArray();
        var suppliedTagIds = suppliedTags.Select(tag => tag.Id).ToArray();
        var persistedTags = await context.Tags
            .Where(tag => suppliedTagIds.Contains(tag.Id))
            .ToDictionaryAsync(tag => tag.Id, cancellationToken)
            .ConfigureAwait(false);

        subscription.Tags.Clear();
        foreach (var suppliedTag in suppliedTags)
        {
            subscription.Tags.Add(
                persistedTags.TryGetValue(suppliedTag.Id, out var persistedTag)
                    ? persistedTag
                    : suppliedTag);
        }
    }

    private static async Task EnsureBundleRelationshipHasNoCycleAsync(
        SubscriptionDbContext context,
        Guid subscriptionId,
        Guid sourceSubscriptionId,
        CancellationToken cancellationToken)
    {
        Guid? candidateId = sourceSubscriptionId;
        var visitedIds = new HashSet<Guid>();

        while (candidateId is { } currentCandidateId)
        {
            if (currentCandidateId == subscriptionId || !visitedIds.Add(currentCandidateId))
            {
                throw new ArgumentException(
                    "A bundled subscription relationship cannot contain a cycle.",
                    nameof(sourceSubscriptionId));
            }

            candidateId = await context.Subscriptions
                .IgnoreQueryFilters()
                .Where(subscription => subscription.Id == currentCandidateId)
                .Select(subscription => subscription.IncludedWithSubscriptionId)
                .SingleOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static SubscriptionQuery ValidateAndNormalizeQuery(SubscriptionQuery query)
    {
        if (query.Category is { } category && !Enum.IsDefined(category))
        {
            throw new ArgumentOutOfRangeException(nameof(query), category, "The subscription category is invalid.");
        }

        if (query.ConfirmationStatus is { } confirmationStatus && !Enum.IsDefined(confirmationStatus))
        {
            throw new ArgumentOutOfRangeException(
                nameof(query),
                confirmationStatus,
                "The subscription confirmation status is invalid.");
        }

        if (query.LifecycleStatus is { } lifecycleStatus && !Enum.IsDefined(lifecycleStatus))
        {
            throw new ArgumentOutOfRangeException(
                nameof(query),
                lifecycleStatus,
                "The subscription lifecycle status is invalid.");
        }

        var currencyCode = NormalizeOptionalFilter(query.CurrencyCode)?.ToUpperInvariant();
        if (currencyCode is { Length: > Money.MaximumCurrencyCodeLength })
        {
            throw new ArgumentOutOfRangeException(
                nameof(query),
                currencyCode.Length,
                $"The currency code cannot exceed {Money.MaximumCurrencyCodeLength} characters.");
        }

        var accountName = NormalizeOptionalFilter(query.AccountName);
        if (accountName is { Length: > Subscription.MaximumAccountNameLength })
        {
            throw new ArgumentOutOfRangeException(
                nameof(query),
                accountName.Length,
                $"The account name cannot exceed {Subscription.MaximumAccountNameLength} characters.");
        }

        return query with
        {
            CurrencyCode = currencyCode,
            AccountName = accountName
        };
    }

    private static string? NormalizeOptionalFilter(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidateIdentifier(Guid identifier, string parameterName)
    {
        if (identifier == Guid.Empty)
        {
            throw new ArgumentException("The identifier cannot be empty.", parameterName);
        }
    }

    private static void ValidateOptionalIdentifier(Guid? identifier, string parameterName)
    {
        if (identifier == Guid.Empty)
        {
            throw new ArgumentException("The identifier cannot be empty.", parameterName);
        }
    }
}
