using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TeacherIdentity.AuthServer.Models.Mappings;

public class AuthenticationStateSnapshotMapping : IEntityTypeConfiguration<AuthenticationStateSnapshot>
{
    public void Configure(EntityTypeBuilder<AuthenticationStateSnapshot> builder)
    {
        builder.HasKey(s => s.SnapshotId);
        builder.Property(s => s.JourneyId).IsRequired();
        builder.Property(e => e.Payload).IsRequired().HasColumnType("jsonb");
        builder.Property(e => e.Created).IsRequired();
        builder.HasIndex(e => e.Payload).HasMethod("gin");
    }
}
