using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Core.Subscriptions;

public sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");
        builder.HasKey(tag => tag.Id);
        builder.Property(tag => tag.Id).ValueGeneratedNever();
        builder.Property(tag => tag.Name)
            .UseCollation("NOCASE")
            .HasMaxLength(Tag.MaximumNameLength)
            .IsRequired();
        builder.HasIndex(tag => tag.Name).IsUnique();
    }
}
