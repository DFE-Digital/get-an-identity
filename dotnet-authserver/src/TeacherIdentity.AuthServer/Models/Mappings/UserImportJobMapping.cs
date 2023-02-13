using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TeacherIdentity.AuthServer.Models.Mappings;

public class UserImportJobMapping : IEntityTypeConfiguration<UserImportJob>
{
    public void Configure(EntityTypeBuilder<UserImportJob> builder)
    {
        builder.ToTable("user_import_jobs");
        builder.Property(j => j.UserImportJobId).IsRequired();
        builder.HasKey(j => j.UserImportJobId);
        builder.Property(j => j.StoredFilename).IsRequired();
        builder.Property(j => j.OriginalFilename).IsRequired();
        builder.Property(j => j.UserImportJobStatus).IsRequired();
        builder.Property(j => j.Uploaded).IsRequired();
    }
}
