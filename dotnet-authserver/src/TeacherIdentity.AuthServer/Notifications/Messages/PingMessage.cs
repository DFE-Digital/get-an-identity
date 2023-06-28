using System.Text.Json.Serialization;

namespace TeacherIdentity.AuthServer.Notifications.Messages;

public record PingMessage : INotificationMessage
{
    public const string MessageTypeName = "Ping";
    [JsonIgnore]
    public Guid WebHookId { get; set; }
}
