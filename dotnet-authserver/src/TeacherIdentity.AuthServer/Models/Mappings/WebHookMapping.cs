using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TeacherIdentity.AuthServer.Models.Mappings;

public class WebHookMapping : IEntityTypeConfiguration<WebHook>
{
    public void Configure(EntityTypeBuilder<WebHook> builder)
    {
        builder.ToTable("webhooks");
        builder.Property(w => w.WebHookId).IsRequired();
        builder.Property(w => w.Endpoint).IsRequired().HasMaxLength(200);
        builder.Property(w => w.Endpoint).IsRequired();
        builder.Property(w => w.Secret).HasMaxLength(64);
        builder.Property(w => w.WebHookMessageTypes).IsRequired().HasDefaultValue(WebHookMessageTypes.None);
        builder.Property(w => w.Created).IsRequired();
        builder.Property(w => w.Updated).IsRequired();
        builder.HasKey(w => w.WebHookId);
    }
}
