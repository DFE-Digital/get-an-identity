using Microsoft.EntityFrameworkCore;

namespace TeacherIdentity.AuthServer.Models;

public class TeacherIdentityServerDbContext : DbContext
{
    public TeacherIdentityServerDbContext(DbContextOptions<TeacherIdentityServerDbContext> options)
            : base(options)
    {
    }

    public TeacherIdentityServerDbContext(string connectionString)
        : this(CreateOptions(connectionString))
    {
    }

    public static void ConfigureOptions(DbContextOptionsBuilder optionsBuilder, string connectionString)
    {
        if (connectionString != null)
        {
            optionsBuilder.UseNpgsql(connectionString);
        }
        else
        {
            optionsBuilder.UseNpgsql();
        }

        optionsBuilder
            .UseSnakeCaseNamingConvention()
            .UseOpenIddict<Application, Authorization, Scope, Token, string>();
    }

    public DbSet<TeacherIdentityUser> Users => Set<TeacherIdentityUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TeacherIdentityServerDbContext).Assembly);

        modelBuilder.Entity<Application>().ToTable("applications");
        modelBuilder.Entity<Authorization>().ToTable("authorizations");
        modelBuilder.Entity<Scope>().ToTable("scopes");
        modelBuilder.Entity<Token>().ToTable("tokens");
    }

    private static DbContextOptions<TeacherIdentityServerDbContext> CreateOptions(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TeacherIdentityServerDbContext>();
        ConfigureOptions(optionsBuilder, connectionString);
        return optionsBuilder.Options;
    }
}
