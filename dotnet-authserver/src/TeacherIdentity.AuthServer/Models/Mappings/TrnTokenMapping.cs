using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TeacherIdentity.AuthServer.Models.Mappings;

public class TrnTokenMapping : IEntityTypeConfiguration<TrnTokenModel>
{
    public void Configure(EntityTypeBuilder<TrnTokenModel> builder)
    {
        builder.ToTable("trn_tokens");
        builder.Property(t => t.TrnToken).HasMaxLength(128);
        builder.HasKey(t => t.TrnToken);
        builder.Property(t => t.Email).HasMaxLength(TrnTokenModel.EmailAddressMaxLength).IsRequired().UseCollation("case_insensitive");
        builder.HasIndex(u => u.Email).HasDatabaseName(TrnTokenModel.EmailAddressUniqueIndexName);
        builder.Property(t => t.Trn).HasMaxLength(7).IsFixedLength();
        builder.Property(t => t.CreatedUtc).IsRequired();
        builder.Property(t => t.ExpiresUtc).IsRequired();
    }
}
