using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TeacherIdentity.AuthServer.Models.Mappings;

public class SmsConfirmationPinMapping : IEntityTypeConfiguration<SmsConfirmationPin>
{
    public void Configure(EntityTypeBuilder<SmsConfirmationPin> builder)
    {
        builder.ToTable("sms_confirmation_pins");
        builder.HasKey(p => p.SmsConfirmationPinId);
        builder.Property(p => p.MobileNumber).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Pin).HasMaxLength(6).IsFixedLength().IsRequired();
        builder.Property(p => p.Expires).IsRequired();
        builder.Property(p => p.IsActive).IsRequired();
        builder.HasIndex(p => new { p.MobileNumber, p.Pin }).IsUnique().HasDatabaseName("ix_sms_confirmation_pins_mobile_number_pin");
    }
}
