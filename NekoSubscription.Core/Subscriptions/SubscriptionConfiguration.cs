using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Core.Subscriptions;

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_Subscriptions_ArchiveState",
                "(IsArchived = 0 AND ArchivedAtUtc IS NULL) OR (IsArchived = 1 AND ArchivedAtUtc IS NOT NULL)");
            tableBuilder.HasCheckConstraint(
                "CK_Subscriptions_DeleteState",
                "(IsDeleted = 0 AND DeletedAtUtc IS NULL) OR (IsDeleted = 1 AND DeletedAtUtc IS NOT NULL)");
        });

        builder.HasKey(subscription => subscription.Id);
        builder.Property(subscription => subscription.Id).ValueGeneratedNever();
        builder.Property(subscription => subscription.Category).IsRequired();
        builder.Property(subscription => subscription.ProviderName)
            .HasMaxLength(Subscription.MaximumProviderNameLength)
            .IsRequired();
        builder.Property(subscription => subscription.ServiceName)
            .HasMaxLength(Subscription.MaximumServiceNameLength)
            .IsRequired();
        builder.Property(subscription => subscription.PlanName)
            .HasMaxLength(Subscription.MaximumPlanNameLength);
        builder.Property(subscription => subscription.AccountName)
            .HasMaxLength(Subscription.MaximumAccountNameLength);
        builder.Property(subscription => subscription.ConfirmationStatus).IsRequired();
        builder.Property(subscription => subscription.LifecycleStatus).IsRequired();
        builder.Property(subscription => subscription.Importance).IsRequired();
        builder.Property(subscription => subscription.Notes)
            .HasMaxLength(Subscription.MaximumNotesLength);
        builder.Property(subscription => subscription.ManagementUrl)
            .HasMaxLength(Subscription.MaximumManagementUrlLength);
        builder.Property(subscription => subscription.IsArchived).IsRequired();
        builder.Property(subscription => subscription.IsDeleted).IsRequired();
        builder.Property(subscription => subscription.CreatedAtUtc).IsRequired();
        builder.Property(subscription => subscription.UpdatedAtUtc).IsRequired();
        builder.Ignore(subscription => subscription.ParticipatesInBudget);

        builder.OwnsOne(subscription => subscription.BillingAmount, moneyBuilder =>
        {
            moneyBuilder.Property(money => money.Amount)
                .HasColumnName("BillingAmount")
                .HasPrecision(28, 18)
                .IsRequired();
            moneyBuilder.Property(money => money.CurrencyCode)
                .HasColumnName("CurrencyCode")
                .HasMaxLength(Money.MaximumCurrencyCodeLength)
                .IsRequired();
            moneyBuilder.Property(money => money.CurrencyKind)
                .HasColumnName("CurrencyKind")
                .IsRequired();
        });
        builder.Navigation(subscription => subscription.BillingAmount).IsRequired();

        builder.OwnsOne(subscription => subscription.BillingSchedule, scheduleBuilder =>
        {
            scheduleBuilder.Property(schedule => schedule.Cadence)
                .HasColumnName("BillingCadence")
                .IsRequired();
            scheduleBuilder.Property(schedule => schedule.IntervalUnit)
                .HasColumnName("BillingIntervalUnit");
            scheduleBuilder.Property(schedule => schedule.IntervalCount)
                .HasColumnName("BillingIntervalCount");
            scheduleBuilder.Property(schedule => schedule.StartsOn)
                .HasColumnName("StartsOn");
            scheduleBuilder.Property(schedule => schedule.NextBillingOn)
                .HasColumnName("NextBillingOn");
            scheduleBuilder.Property(schedule => schedule.EndsOn)
                .HasColumnName("EndsOn");
            scheduleBuilder.Property(schedule => schedule.AutomaticallyRenews)
                .HasColumnName("AutomaticallyRenews")
                .IsRequired();
            scheduleBuilder.HasIndex(schedule => schedule.NextBillingOn);
        });
        builder.Navigation(subscription => subscription.BillingSchedule).IsRequired();

        builder.OwnsOne(subscription => subscription.PaymentDeferralPolicy, policyBuilder =>
        {
            policyBuilder.Property(policy => policy.ProviderGracePeriodDays)
                .HasColumnName("ProviderGracePeriodDays");
            policyBuilder.Property(policy => policy.BudgetToleranceDays)
                .HasColumnName("BudgetToleranceDays");
        });

        builder.HasOne(subscription => subscription.PaymentProfile)
            .WithMany(paymentProfile => paymentProfile.Subscriptions)
            .HasForeignKey(subscription => subscription.PaymentProfileId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(subscription => subscription.IncludedWithSubscription)
            .WithMany(subscription => subscription.IncludedSubscriptions)
            .HasForeignKey(subscription => subscription.IncludedWithSubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(subscription => subscription.Tags)
            .WithMany(tag => tag.Subscriptions)
            .UsingEntity<Dictionary<string, object>>(
                "SubscriptionTags",
                right => right
                    .HasOne<Tag>()
                    .WithMany()
                    .HasForeignKey("TagId")
                    .OnDelete(DeleteBehavior.Cascade),
                left => left
                    .HasOne<Subscription>()
                    .WithMany()
                    .HasForeignKey("SubscriptionId")
                    .OnDelete(DeleteBehavior.Cascade),
                joinBuilder =>
                {
                    joinBuilder.ToTable("SubscriptionTags");
                    joinBuilder.HasKey("SubscriptionId", "TagId");
                });

        builder.HasIndex(subscription => subscription.PaymentProfileId);
        builder.HasIndex(subscription => subscription.IncludedWithSubscriptionId);
        builder.HasIndex(subscription => subscription.ConfirmationStatus);
        builder.HasIndex(subscription => subscription.IsArchived);
        builder.HasQueryFilter(subscription => !subscription.IsDeleted);
    }
}
