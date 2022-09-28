using TeacherIdentity.AuthServer.EventProcessing;
using TeacherIdentity.AuthServer.Events;

namespace TeacherIdentity.AuthServer.Tests.Infrastructure;

public class CaptureEventObserver : IEventObserver
{
    private readonly AsyncLocal<List<EventBase>> _events = new();

    public void Init() => _events.Value ??= new List<EventBase>();

    public void OnEventSaved(EventBase @event)
    {
        _events.Value ??= new List<EventBase>();
        _events.Value.Add(@event);
    }

    public void AssertEventsSaved(params Action<EventBase>[] eventInspectors)
    {
        var events = (_events.Value ?? new()).AsReadOnly();
        Assert.Collection(events, eventInspectors);
    }
}
