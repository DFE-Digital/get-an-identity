using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TeacherIdentity.AuthServer.Models.Mappings;

public class TeacherIdentityUserMapping : IEntityTypeConfiguration<TeacherIdentityUser>
{
    public void Configure(EntityTypeBuilder<TeacherIdentityUser> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.UserId);
        builder.Property(u => u.EmailAddress).HasMaxLength(200).IsRequired();
        builder.HasIndex(u => u.EmailAddress).IsUnique();
        builder.Property(u => u.Trn).HasMaxLength(7).IsFixedLength();
        builder.Property(u => u.FirstName).HasMaxLength(200).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(200).IsRequired();
    }
}
