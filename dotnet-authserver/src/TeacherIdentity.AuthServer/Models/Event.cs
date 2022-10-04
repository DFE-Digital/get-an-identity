using System.Text.Json;
using TeacherIdentity.AuthServer.Events;

namespace TeacherIdentity.AuthServer.Models;

public class Event
{
    public required string EventName { get; init; }
    public required DateTime Created { get; init; }
    public required string Payload { get; init; }

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
