using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Core.Subscriptions;

public sealed class CustomFieldConfiguration : IEntityTypeConfiguration<CustomField>
{
    public void Configure(EntityTypeBuilder<CustomField> builder)
    {
        builder.ToTable("CustomFields", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_CustomFields_TypedValue",
                $"(FieldType = {(int)CustomFieldType.Text} AND TextValue IS NOT NULL AND NumberValue IS NULL AND BooleanValue IS NULL AND DateValue IS NULL AND UrlValue IS NULL) OR " +
                $"(FieldType = {(int)CustomFieldType.Number} AND TextValue IS NULL AND NumberValue IS NOT NULL AND BooleanValue IS NULL AND DateValue IS NULL AND UrlValue IS NULL) OR " +
                $"(FieldType = {(int)CustomFieldType.Boolean} AND TextValue IS NULL AND NumberValue IS NULL AND BooleanValue IS NOT NULL AND DateValue IS NULL AND UrlValue IS NULL) OR " +
                $"(FieldType = {(int)CustomFieldType.Date} AND TextValue IS NULL AND NumberValue IS NULL AND BooleanValue IS NULL AND DateValue IS NOT NULL AND UrlValue IS NULL) OR " +
                $"(FieldType = {(int)CustomFieldType.Url} AND TextValue IS NULL AND NumberValue IS NULL AND BooleanValue IS NULL AND DateValue IS NULL AND UrlValue IS NOT NULL)");
        });

        builder.HasKey(field => field.Id);
        builder.Property(field => field.Id).ValueGeneratedNever();
        builder.Property(field => field.Name)
            .HasMaxLength(CustomField.MaximumNameLength)
            .IsRequired();
        builder.Property(field => field.FieldType).IsRequired();
        builder.Property(field => field.SortOrder).IsRequired();
        builder.Property(field => field.TextValue)
            .HasMaxLength(CustomField.MaximumTextValueLength);
        builder.Property(field => field.NumberValue)
            .HasPrecision(28, 8);
        builder.Property(field => field.UrlValue)
            .HasMaxLength(CustomField.MaximumUrlValueLength);
        builder.HasIndex(field => new { field.CustomSubscriptionId, field.SortOrder });
        builder.HasIndex(field => field.FieldType);
    }
}
