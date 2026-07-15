using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Core.Subscriptions;

public sealed class PaymentProfileConfiguration : IEntityTypeConfiguration<PaymentProfile>
{
    public void Configure(EntityTypeBuilder<PaymentProfile> builder)
    {
        builder.ToTable("PaymentProfiles", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_PaymentProfiles_ArchiveState",
                "(IsArchived = 0 AND ArchivedAtUtc IS NULL) OR (IsArchived = 1 AND ArchivedAtUtc IS NOT NULL)");
        });

        builder.HasKey(paymentProfile => paymentProfile.Id);
        builder.Property(paymentProfile => paymentProfile.Id).ValueGeneratedNever();
        builder.Property(paymentProfile => paymentProfile.DisplayName)
            .HasMaxLength(PaymentProfile.MaximumDisplayNameLength)
            .IsRequired();
        builder.Property(paymentProfile => paymentProfile.Channel).IsRequired();
        builder.Property(paymentProfile => paymentProfile.AccountIdentifier)
            .HasMaxLength(PaymentProfile.MaximumAccountIdentifierLength);
        builder.Property(paymentProfile => paymentProfile.ProviderName)
            .HasMaxLength(PaymentProfile.MaximumProviderNameLength);
        builder.Property(paymentProfile => paymentProfile.Notes)
            .HasMaxLength(PaymentProfile.MaximumNotesLength);
        builder.Property(paymentProfile => paymentProfile.IsArchived).IsRequired();
        builder.Property(paymentProfile => paymentProfile.CreatedAtUtc).IsRequired();
        builder.Property(paymentProfile => paymentProfile.UpdatedAtUtc).IsRequired();
        builder.HasIndex(paymentProfile => paymentProfile.Channel);
        builder.HasIndex(paymentProfile => paymentProfile.IsArchived);
    }
}
