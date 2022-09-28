using TeacherIdentity.AuthServer.Events;

namespace TeacherIdentity.AuthServer.EventProcessing;

public class NoopEventObserver : IEventObserver
{
    public void OnEventSaved(EventBase @event) { }
}
