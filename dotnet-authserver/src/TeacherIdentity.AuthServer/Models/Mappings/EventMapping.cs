using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TeacherIdentity.AuthServer.Models.Mappings;

public class EventMapping : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");
        builder.Property<long>("event_id").IsRequired().ValueGeneratedOnAdd();
        builder.Property(e => e.EventName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Created).IsRequired().HasColumnType("jsonb");
        builder.Property(e => e.Payload).IsRequired();
        builder.HasKey("event_id");
    }
}
