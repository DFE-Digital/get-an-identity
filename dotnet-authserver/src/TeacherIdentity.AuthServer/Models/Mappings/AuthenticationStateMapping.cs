using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TeacherIdentity.AuthServer.Models.Mappings;

public class AuthenticationStateMapping : IEntityTypeConfiguration<AuthenticationState>
{
    public void Configure(EntityTypeBuilder<AuthenticationState> builder)
    {
        builder.HasKey(s => s.JourneyId);
        builder.Property(e => e.Payload).IsRequired().HasColumnType("jsonb");
        builder.Property(e => e.Created).IsRequired();
        builder.Property(e => e.LastAccessed).IsRequired();
        builder.HasIndex(e => e.Payload).HasMethod("gin");
    }
}
