using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Core.Subscriptions;

public sealed class PhoneNumberSubscriptionConfiguration : IEntityTypeConfiguration<PhoneNumberSubscription>
{
    public void Configure(EntityTypeBuilder<PhoneNumberSubscription> builder)
    {
        builder.ToTable("PhoneNumberSubscriptions");
        builder.Property(subscription => subscription.PhoneNumber)
            .HasMaxLength(PhoneNumberSubscription.MaximumPhoneNumberLength)
            .IsRequired();
        builder.Property(subscription => subscription.PhoneNumberType).IsRequired();
        builder.Property(subscription => subscription.CarrierName)
            .HasMaxLength(PhoneNumberSubscription.MaximumCarrierNameLength)
            .IsRequired();
        builder.Property(subscription => subscription.RegionName)
            .HasMaxLength(PhoneNumberSubscription.MaximumRegionNameLength);
        builder.Property(subscription => subscription.IsPrepaid).IsRequired();
        builder.HasIndex(subscription => subscription.PhoneNumber);
    }
}
