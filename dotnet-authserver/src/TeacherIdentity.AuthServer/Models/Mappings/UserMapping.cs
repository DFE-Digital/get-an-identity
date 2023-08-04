using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TeacherIdentity.AuthServer.Models.Mappings;

public class UserMapping : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.UserId);
        builder.Property(u => u.EmailAddress).HasMaxLength(EmailAddress.EmailAddressMaxLength).IsRequired().UseCollation("case_insensitive");
        builder.HasIndex(u => u.EmailAddress).IsUnique().HasDatabaseName(User.EmailAddressUniqueIndexName).HasFilter("is_deleted = false");
        builder.HasIndex(u => u.Trn).IsUnique().HasFilter("is_deleted = false and trn is not null");
        builder.Property(u => u.FirstName).HasMaxLength(User.FirstNameMaxLength).IsRequired();
        builder.Property(u => u.MiddleName).HasMaxLength(User.FirstNameMaxLength);
        builder.Property(u => u.LastName).HasMaxLength(User.LastNameMaxLength).IsRequired();
        builder.Property(u => u.PreferredName).HasMaxLength(User.PreferredNameMaxLength);
        builder.Property(u => u.DateOfBirth).HasDefaultValueSql("NULL");
        builder.Property(u => u.Created).IsRequired();
        builder.Property(u => u.CompletedTrnLookup);
        builder.Property(u => u.UserType).IsRequired();
        builder.Property(u => u.Trn).HasMaxLength(7).IsFixedLength();
        builder.Property(u => u.TrnAssociationSource);
        builder.Property(u => u.StaffRoles).HasColumnType("varchar[]");
        builder.Property(u => u.RegisteredWithClientId).HasMaxLength(100);
        builder.Property(u => u.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(u => u.TrnLookupStatus);
        builder.Property(u => u.TrnVerificationLevel);
        builder.Property(u => u.MergedWithUserId);
        builder.Property(u => u.MobileNumber).HasMaxLength(100);
        builder.Property(u => u.NormalizedMobileNumber).HasMaxLength(15);
        builder.HasIndex(u => u.NormalizedMobileNumber).IsUnique().HasDatabaseName(User.MobileNumberUniqueIndexName).HasFilter("is_deleted = false and normalized_mobile_number is not null");
        builder.HasOne(u => u.MergedWithUser).WithMany(u => u.MergedUsers).HasForeignKey(u => u.MergedWithUserId);
        builder.HasOne(u => u.RegisteredWithClient).WithMany().HasForeignKey(u => u.RegisteredWithClientId).HasPrincipalKey(a => a.ClientId);
        builder.HasQueryFilter(u => EF.Property<bool>(u, "IsDeleted") == false);
    }
}
