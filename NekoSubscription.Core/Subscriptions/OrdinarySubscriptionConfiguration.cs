using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Core.Subscriptions;

public sealed class OrdinarySubscriptionConfiguration : IEntityTypeConfiguration<OrdinarySubscription>
{
    public void Configure(EntityTypeBuilder<OrdinarySubscription> builder)
    {
        builder.ToTable("OrdinarySubscriptions");
    }
}
