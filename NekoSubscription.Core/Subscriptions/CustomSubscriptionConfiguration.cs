using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Core.Subscriptions;

public sealed class CustomSubscriptionConfiguration : IEntityTypeConfiguration<CustomSubscription>
{
    public void Configure(EntityTypeBuilder<CustomSubscription> builder)
    {
        builder.ToTable("CustomSubscriptions");
        builder.HasMany(subscription => subscription.Fields)
            .WithOne(field => field.CustomSubscription)
            .HasForeignKey(field => field.CustomSubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
