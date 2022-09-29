using TeacherIdentity.AuthServer.Events;

namespace TeacherIdentity.AuthServer.EventProcessing;

public class NoopEventObserver : IEventObserver
{
    public Task OnEventSaved(EventBase @event) => Task.CompletedTask;
}
