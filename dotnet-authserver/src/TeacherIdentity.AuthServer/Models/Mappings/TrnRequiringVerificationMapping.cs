using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TeacherIdentity.AuthServer.Models.Mappings;

public class TrnRequiringVerificationMapping : IEntityTypeConfiguration<TrnRequiringVerification>
{
    public void Configure(EntityTypeBuilder<TrnRequiringVerification> builder)
    {
        builder.HasKey(e => e.Trn);
        builder.Property(e => e.Trn).HasMaxLength(7).IsFixedLength();
    }
}
