using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

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

    public DbSet<EmailConfirmationPin> EmailConfirmationPins => Set<EmailConfirmationPin>();

    public DbSet<JourneyTrnLookupState> JourneyTrnLookupStates => Set<JourneyTrnLookupState>();

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TeacherIdentityServerDbContext).Assembly);

        modelBuilder.Entity<Application>().ToTable("applications");
        modelBuilder.Entity<Authorization>().ToTable("authorizations");
        modelBuilder.Entity<Scope>().ToTable("scopes");
        modelBuilder.Entity<Token>().ToTable("tokens");
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        UpdateSoftDeleteFlag();

        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        UpdateSoftDeleteFlag();

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private static DbContextOptions<TeacherIdentityServerDbContext> CreateOptions(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TeacherIdentityServerDbContext>();
        ConfigureOptions(optionsBuilder, connectionString);
        return optionsBuilder.Options;
    }

    private void UpdateSoftDeleteFlag()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is User && entry.State == EntityState.Deleted)
            {
                entry.CurrentValues["is_deleted"] = true;
            }
        }
    }
}
