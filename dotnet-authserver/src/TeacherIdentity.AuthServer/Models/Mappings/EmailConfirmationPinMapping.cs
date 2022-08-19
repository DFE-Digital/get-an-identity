using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TeacherIdentity.AuthServer.Models.Mappings;

public class EmailConfirmationPinMapping : IEntityTypeConfiguration<EmailConfirmationPin>
{
    public void Configure(EntityTypeBuilder<EmailConfirmationPin> builder)
    {
        builder.ToTable("email_confirmation_pins");
        builder.HasKey(p => p.EmailConfirmationPinId);
        builder.Property(p => p.Email).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Pin).HasMaxLength(6).IsFixedLength().IsRequired();
        builder.Property(p => p.Expires).IsRequired();
        builder.Property(p => p.IsActive).IsRequired();
        builder.HasIndex(p => new { p.Email, p.Pin }).IsUnique().HasDatabaseName("ix_email_confirmation_pins_email_pin");
    }
}
