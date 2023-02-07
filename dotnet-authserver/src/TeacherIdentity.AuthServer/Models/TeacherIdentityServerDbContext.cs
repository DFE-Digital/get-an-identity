using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TeacherIdentity.AuthServer.EventProcessing;
using TeacherIdentity.AuthServer.Events;

namespace TeacherIdentity.AuthServer.Models;

public class TeacherIdentityServerDbContext : DbContext
{
    private readonly IEventObserver? _eventObserver;
    private readonly List<EventBase> _events = new();

    public TeacherIdentityServerDbContext(DbContextOptions<TeacherIdentityServerDbContext> options, IEventObserver? eventObserver)
            : base(options)
    {
        _eventObserver = eventObserver;
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

    public static TeacherIdentityServerDbContext Create(string connectionString, IEventObserver? eventObserver = null) =>
        new TeacherIdentityServerDbContext(CreateOptions(connectionString), eventObserver);

    public DbSet<EmailConfirmationPin> EmailConfirmationPins => Set<EmailConfirmationPin>();

    public DbSet<Event> Events => Set<Event>();

    public DbSet<JourneyTrnLookupState> JourneyTrnLookupStates => Set<JourneyTrnLookupState>();

    public DbSet<User> Users => Set<User>();

    public DbSet<UserImportJob> UserImportJobs => Set<UserImportJob>();

    public DbSet<UserSearchAttribute> UserSearchAttributes => Set<UserSearchAttribute>();

    public DbSet<WebHook> WebHooks => Set<WebHook>();

    public void AddEvent(EventBase @event)
    {
        Events.Add(Event.FromEventBase(@event));
        _events.Add(@event);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TeacherIdentityServerDbContext).Assembly);

        modelBuilder.Entity<Application>().ToTable("applications");
        modelBuilder.Entity<Authorization>().ToTable("authorizations");
        modelBuilder.Entity<Scope>().ToTable("scopes");
        modelBuilder.Entity<Token>().ToTable("tokens");
    }

    [Obsolete($"Use {nameof(SaveChangesAsync)} instead.")]
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        throw new NotSupportedException($"Use {nameof(SaveChangesAsync)} instead.");
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        UpdateSoftDeleteFlag();

        try
        {
            var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

            await PublishEvents();

            return result;
        }
        finally
        {
            _events.Clear();
        }
    }

    private static DbContextOptions<TeacherIdentityServerDbContext> CreateOptions(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TeacherIdentityServerDbContext>();
        ConfigureOptions(optionsBuilder, connectionString);
        return optionsBuilder.Options;
    }

    private async Task PublishEvents()
    {
        if (_eventObserver is null)
        {
            return;
        }

        foreach (var e in _events)
        {
            await _eventObserver.OnEventSaved(e);
        }
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
