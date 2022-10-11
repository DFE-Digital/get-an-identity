using System.Text.Json;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Infrastructure.Json;

namespace TeacherIdentity.AuthServer.Models;

public class Event
{
    private static readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions()
    {
        Converters =
        {
            new DateOnlyConverter()
        }
    };

    public required string EventName { get; init; }
    public required DateTime Created { get; init; }
    public required string Payload { get; init; }

    public static Event FromEventBase(EventBase @event)
    {
        var eventName = @event.GetType().Name;
        var payload = JsonSerializer.Serialize(@event, inputType: @event.GetType(), _serializerOptions);

        return new Event()
        {
            Created = @event.CreatedUtc,
            EventName = eventName,
            Payload = payload
        };
    }
}
