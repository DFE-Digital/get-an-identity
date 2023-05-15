using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TeacherIdentity.AuthServer.EventProcessing;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.TestCommon;

public class PublishEventsDbCommandInterceptor : SaveChangesInterceptor
{
    private readonly IEventObserver _eventObserver;

    public PublishEventsDbCommandInterceptor(IEventObserver eventObserver)
    {
        _eventObserver = eventObserver;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        throw new NotImplementedException();
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        var events = eventData.Context!.ChangeTracker.Entries<Event>();

        foreach (var e in events)
        {
            if (e.State == EntityState.Added)
            {
                e.Property(e => e.Published).CurrentValue = true;

                eventData.Context.SavedChanges += OnEventSaved;

                void OnEventSaved(object? sender, SavedChangesEventArgs args)
                {
                    _eventObserver.OnEventSaved(e.Entity.ToEventBase()).GetAwaiter().GetResult();
                    eventData.Context.SavedChanges -= OnEventSaved;
                }
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
