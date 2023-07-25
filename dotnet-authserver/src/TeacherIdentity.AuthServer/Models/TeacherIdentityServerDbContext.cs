using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;

namespace TeacherIdentity.AuthServer.Models;

public class TeacherIdentityServerDbContext : DbContext
{
    public TeacherIdentityServerDbContext(DbContextOptions<TeacherIdentityServerDbContext> options)
        : base(options)
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

    public static TeacherIdentityServerDbContext Create(string connectionString) =>
        new TeacherIdentityServerDbContext(CreateOptions(connectionString));

    public DbSet<EmailConfirmationPin> EmailConfirmationPins => Set<EmailConfirmationPin>();

    public DbSet<SmsConfirmationPin> SmsConfirmationPins => Set<SmsConfirmationPin>();

    public DbSet<Event> Events => Set<Event>();

    public DbSet<JourneyTrnLookupState> JourneyTrnLookupStates => Set<JourneyTrnLookupState>();

    public DbSet<User> Users => Set<User>();

    public DbSet<UserImportJob> UserImportJobs => Set<UserImportJob>();

    public DbSet<UserImportJobRow> UserImportJobRows => Set<UserImportJobRow>();

    public DbSet<UserSearchAttribute> UserSearchAttributes => Set<UserSearchAttribute>();

    public DbSet<WebHook> WebHooks => Set<WebHook>();

    public DbSet<EstablishmentDomain> EstablishmentDomains => Set<EstablishmentDomain>();

    public DbSet<TrnTokenModel> TrnTokens => Set<TrnTokenModel>();

    public DbSet<AuthenticationState> AuthenticationStates => Set<AuthenticationState>();

    public DbSet<AuthenticationStateSnapshot> AuthenticationStateSnapshots => Set<AuthenticationStateSnapshot>();

    public void AddEvent(EventBase @event)
    {
        Events.Add(Event.FromEventBase(@event));
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<MobileNumber>().HaveConversion<MobileNumberConverter>();
    }

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

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        UpdateSoftDeleteFlag();
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
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
                entry.CurrentValues["IsDeleted"] = true;
            }
        }
    }
}
