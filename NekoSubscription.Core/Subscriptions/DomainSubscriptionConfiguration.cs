using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Core.Subscriptions;

public sealed class DomainSubscriptionConfiguration : IEntityTypeConfiguration<DomainSubscription>
{
    public void Configure(EntityTypeBuilder<DomainSubscription> builder)
    {
        builder.ToTable("DomainSubscriptions");
        builder.Property(subscription => subscription.DomainName)
            .UseCollation("NOCASE")
            .HasMaxLength(DomainSubscription.MaximumDomainNameLength)
            .IsRequired();
        builder.Property(subscription => subscription.RegisteredOn);
        builder.Property(subscription => subscription.ExpiresOn);
        builder.HasIndex(subscription => subscription.DomainName);
        builder.HasIndex(subscription => subscription.ExpiresOn);
    }
}
