using System.Text.Json;
using System.Text.Json.Serialization;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Notifications.Messages;

namespace TeacherIdentity.AuthServer.Notifications.WebHooks;

public class WebHookNotificationPublisher : INotificationPublisher
{
    public WebHookNotificationPublisher(
        IWebHookNotificationSender sender)
    {
        Sender = sender;
    }

    protected static JsonSerializerOptions SerializerOptions { get; } = new JsonSerializerOptions()
    {
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false),
            new NotificationMessageSerializer()
        },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected IWebHookNotificationSender Sender { get; }

    public Task<WebHook[]> GetWebHooksForNotification(NotificationEnvelope notification)
    {
        // TODO Get this from DB
        return Task.FromResult(Array.Empty<WebHook>());
    }

    public virtual async Task PublishNotification(NotificationEnvelope notification)
    {
        var payload = SerializeNotification(notification);

        var webHooks = await GetWebHooksForNotification(notification);

        foreach (var webHook in webHooks)
        {
            await Sender.SendNotification(webHook.Endpoint, payload);
        }
    }

    protected string SerializeNotification(NotificationEnvelope notification) =>
        JsonSerializer.Serialize(notification, options: SerializerOptions);

    private class NotificationMessageSerializer : JsonConverter<INotificationMessage>
    {
        public override INotificationMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }

        public override void Write(Utf8JsonWriter writer, INotificationMessage value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}
