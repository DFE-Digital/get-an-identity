using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TeacherIdentity.AuthServer.Models.Mappings;

public class JourneyTrnLookupStateMapping : IEntityTypeConfiguration<JourneyTrnLookupState>
{
    public void Configure(EntityTypeBuilder<JourneyTrnLookupState> builder)
    {
        builder.ToTable("journey_trn_lookup_states");
        builder.HasKey(s => s.JourneyId);
        builder.Property(s => s.FirstName).IsRequired();
        builder.Property(s => s.LastName).IsRequired();
        builder.Property(s => s.DateOfBirth).IsRequired();
        builder.Property(s => s.Trn).HasMaxLength(7).IsFixedLength();
        builder.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId);
    }
}
