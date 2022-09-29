using System.Text.Json;
using System.Text.Json.Serialization;

namespace TeacherIdentity.AuthServer.Notifications.WebHooks;

public class WebHookNotificationOptions
{
    public JsonSerializerOptions SerializerOptions { get; } = new JsonSerializerOptions()
    {
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false)
        }
    };
}
