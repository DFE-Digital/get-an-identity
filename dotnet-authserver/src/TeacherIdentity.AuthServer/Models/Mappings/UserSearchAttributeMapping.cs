using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TeacherIdentity.AuthServer.Models.Mappings;

public class UserSearchAttributeMapping : IEntityTypeConfiguration<UserSearchAttribute>
{
    public void Configure(EntityTypeBuilder<UserSearchAttribute> builder)
    {
        builder.ToTable("user_search_attributes");
        builder.Property(a => a.UserSearchAttributeId).IsRequired().ValueGeneratedOnAdd();
        builder.HasKey(a => a.UserSearchAttributeId);
        builder.Property(a => a.UserId).IsRequired();
        builder.HasIndex(a => a.UserId).HasDatabaseName(UserSearchAttribute.UserIdIndexName);
        builder.Property(a => a.AttributeType).IsRequired();
        builder.Property(a => a.AttributeValue).IsRequired().UseCollation("case_insensitive");
        builder.HasIndex(a => new { a.AttributeType, a.AttributeValue }).HasDatabaseName(UserSearchAttribute.AttributeTypeAndValueIndexName);
    }
}
