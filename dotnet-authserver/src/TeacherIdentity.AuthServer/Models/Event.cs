using System.Text.Json;
using TeacherIdentity.AuthServer.Events;

namespace TeacherIdentity.AuthServer.Models;

public class Event
{
    public string EventName { get; set; } = null!;
    public DateTime Created { get; set; }
    public string Payload { get; set; } = null!;

    public static Event FromEventBase(EventBase @event)
    {
        var eventName = @event.GetType().Name;
        var payload = JsonSerializer.Serialize(@event, inputType: @event.GetType());

        return new Event()
        {
            Created = @event.CreatedUtc,
            EventName = eventName,
            Payload = payload
        };
    }
}
