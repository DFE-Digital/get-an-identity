using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TeacherIdentity.AuthServer.Models.Mappings;

public class UserMapping : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.UserId);
        builder.Property(u => u.EmailAddress).HasMaxLength(200).IsRequired();
        builder.HasIndex(u => u.EmailAddress).IsUnique().HasFilter("is_deleted = true");
        builder.HasIndex(u => u.Trn).IsUnique().HasFilter("is_deleted = true and trn is not null");
        builder.Property(u => u.FirstName).HasMaxLength(200).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(200).IsRequired();
        builder.Property(u => u.DateOfBirth);
        builder.Property(u => u.Created).IsRequired();
        builder.Property(u => u.CompletedTrnLookup);
        builder.Property(u => u.UserType).IsRequired();
        builder.Property(u => u.Trn).HasMaxLength(7).IsFixedLength();
        builder.Property<bool>("is_deleted").IsRequired().HasDefaultValue(false);
        builder.HasQueryFilter(u => EF.Property<bool>(u, "is_deleted") == false);
    }
}
