using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TeacherIdentity.AuthServer.Models.Mappings;

public class JourneyTrnLookupStateMapping : IEntityTypeConfiguration<JourneyTrnLookupState>
{
    public void Configure(EntityTypeBuilder<JourneyTrnLookupState> builder)
    {
        builder.ToTable("journey_trn_lookup_states");
        builder.HasKey(s => s.JourneyId);
        builder.Property(s => s.OfficialFirstName).IsRequired();
        builder.Property(s => s.OfficialLastName).IsRequired();
        builder.Property(s => s.DateOfBirth).IsRequired();
        builder.Property(s => s.Trn).HasMaxLength(7).IsFixedLength();
        builder.Property(s => s.NationalInsuranceNumber).HasMaxLength(9).IsFixedLength();
        builder.Property(s => s.SupportTicketCreated).IsRequired();
        builder.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId);
    }
}
