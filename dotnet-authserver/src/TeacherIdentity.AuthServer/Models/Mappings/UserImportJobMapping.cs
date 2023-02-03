using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TeacherIdentity.AuthServer.Models.Mappings;

public class UserImportJobMapping : IEntityTypeConfiguration<UserImportJob>
{
    public void Configure(EntityTypeBuilder<UserImportJob> builder)
    {
        builder.ToTable("user_import_jobs");
        builder.Property(a => a.UserImportJobId).IsRequired();
        builder.HasKey(a => a.UserImportJobId);
        builder.Property(a => a.StoredFilename).IsRequired();
        builder.Property(a => a.OriginalFilename).IsRequired();
        builder.Property(a => a.UserImportJobStatus).IsRequired();
        builder.Property(a => a.Uploaded).IsRequired();
    }
}
