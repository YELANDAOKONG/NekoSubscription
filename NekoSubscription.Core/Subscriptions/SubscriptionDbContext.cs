using Microsoft.EntityFrameworkCore;

using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Core.Subscriptions;

public sealed class SubscriptionDbContext(DbContextOptions<SubscriptionDbContext> options) : DbContext(options)
{
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<OrdinarySubscription> OrdinarySubscriptions => Set<OrdinarySubscription>();

    public DbSet<PhoneNumberSubscription> PhoneNumberSubscriptions => Set<PhoneNumberSubscription>();

    public DbSet<DomainSubscription> DomainSubscriptions => Set<DomainSubscription>();

    public DbSet<CloudServiceSubscription> CloudServiceSubscriptions => Set<CloudServiceSubscription>();

    public DbSet<CustomSubscription> CustomSubscriptions => Set<CustomSubscription>();

    public DbSet<PaymentProfile> PaymentProfiles => Set<PaymentProfile>();

    public DbSet<Tag> Tags => Set<Tag>();

    public DbSet<CustomField> CustomFields => Set<CustomField>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new SubscriptionConfiguration());
        modelBuilder.ApplyConfiguration(new OrdinarySubscriptionConfiguration());
        modelBuilder.ApplyConfiguration(new PhoneNumberSubscriptionConfiguration());
        modelBuilder.ApplyConfiguration(new DomainSubscriptionConfiguration());
        modelBuilder.ApplyConfiguration(new CloudServiceSubscriptionConfiguration());
        modelBuilder.ApplyConfiguration(new CustomSubscriptionConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentProfileConfiguration());
        modelBuilder.ApplyConfiguration(new TagConfiguration());
        modelBuilder.ApplyConfiguration(new CustomFieldConfiguration());
    }
}
