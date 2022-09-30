using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TeacherIdentity.AuthServer.Models.Mappings;

public class WebHookMapping : IEntityTypeConfiguration<WebHook>
{
    public void Configure(EntityTypeBuilder<WebHook> builder)
    {
        builder.ToTable("webhooks");
        builder.Property(w => w.WebHookId).ValueGeneratedOnAdd();
        builder.Property(w => w.Endpoint).IsRequired().HasMaxLength(200);
        builder.Property(w => w.Endpoint).IsRequired();
        builder.HasKey(w => w.WebHookId);
    }
}
