using Microsoft.EntityFrameworkCore;

namespace TeacherIdentityServer.Models;

public class TeacherIdentityServerDbContext : DbContext
{
    public TeacherIdentityServerDbContext(DbContextOptions<TeacherIdentityServerDbContext> options)
        : base(options)
    {
    }

    public DbSet<TeacherIdentityUser> Users => Set<TeacherIdentityUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TeacherIdentityUser>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.UserId);
            entity.Property(u => u.EmailAddress).HasMaxLength(200).IsRequired();
            entity.HasIndex(u => u.EmailAddress).IsUnique();
            entity.Property(u => u.Trn).HasMaxLength(7).IsFixedLength();
            entity.Property(u => u.FirstName).HasMaxLength(200).IsRequired();
            entity.Property(u => u.LastName).HasMaxLength(200).IsRequired();
        });
    }
}
