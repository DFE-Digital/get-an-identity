using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TeacherIdentity.AuthServer.Models.Mappings;

public class UserImportJobRowMapping : IEntityTypeConfiguration<UserImportJobRow>
{
    public void Configure(EntityTypeBuilder<UserImportJobRow> builder)
    {
        builder.ToTable("user_import_job_rows");
        builder.Property(r => r.UserImportJobId).IsRequired();
        builder.Property(r => r.Id).HasMaxLength(UserImportJobRow.IdMaxLength).IsRequired();
        builder.Property(r => r.RowNumber).IsRequired();
        builder.HasKey(r => new { r.UserImportJobId, r.RowNumber });
        builder.Property(r => r.Errors).HasColumnType("varchar[]");
        builder.HasOne(r => r.UserImportJob).WithMany(j => j.UserImportJobRows).HasForeignKey(r => r.UserImportJobId);
    }
}
