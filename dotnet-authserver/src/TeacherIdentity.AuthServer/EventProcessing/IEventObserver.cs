using TeacherIdentity.AuthServer.Events;

namespace TeacherIdentity.AuthServer.EventProcessing;

public interface IEventObserver
{
    void OnEventSaved(EventBase @event);
}
