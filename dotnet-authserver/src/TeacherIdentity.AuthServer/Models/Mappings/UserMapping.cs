using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TeacherIdentity.AuthServer.Models.Mappings;

public class UserMapping : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.UserId);
        builder.Property(u => u.EmailAddress).HasMaxLength(User.EmailAddressMaxLength).IsRequired();
        builder.HasIndex(u => u.EmailAddress).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(u => u.Trn).IsUnique().HasFilter("is_deleted = false and trn is not null");
        builder.Property(u => u.FirstName).HasMaxLength(User.FirstNameAddressMaxLength).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(User.LastNameAddressMaxLength).IsRequired();
        builder.Property(u => u.DateOfBirth);
        builder.Property(u => u.Created).IsRequired();
        builder.Property(u => u.CompletedTrnLookup);
        builder.Property(u => u.UserType).IsRequired();
        builder.Property(u => u.Trn).HasMaxLength(7).IsFixedLength();
        builder.Property(u => u.TrnAssociationSource);
        builder.Property(u => u.StaffRoles).HasColumnType("varchar[]");
        builder.Property<bool>("is_deleted").IsRequired().HasDefaultValue(false);
        builder.HasQueryFilter(u => EF.Property<bool>(u, "is_deleted") == false);
    }
}
