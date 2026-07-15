using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Core.Subscriptions;

public sealed class CloudServiceSubscriptionConfiguration : IEntityTypeConfiguration<CloudServiceSubscription>
{
    public void Configure(EntityTypeBuilder<CloudServiceSubscription> builder)
    {
        builder.ToTable("CloudServiceSubscriptions");
        builder.Property(subscription => subscription.BillingMode).IsRequired();
        builder.Property(subscription => subscription.TenantIdentifier)
            .HasMaxLength(CloudServiceSubscription.MaximumTenantIdentifierLength);
        builder.Property(subscription => subscription.ProjectIdentifier)
            .HasMaxLength(CloudServiceSubscription.MaximumProjectIdentifierLength);
        builder.HasIndex(subscription => subscription.TenantIdentifier);
        builder.HasIndex(subscription => subscription.ProjectIdentifier);
    }
}
