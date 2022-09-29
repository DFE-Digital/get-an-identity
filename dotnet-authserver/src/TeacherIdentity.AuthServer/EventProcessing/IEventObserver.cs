using TeacherIdentity.AuthServer.Events;

namespace TeacherIdentity.AuthServer.EventProcessing;

public interface IEventObserver
{
    Task OnEventSaved(EventBase @event);
}
