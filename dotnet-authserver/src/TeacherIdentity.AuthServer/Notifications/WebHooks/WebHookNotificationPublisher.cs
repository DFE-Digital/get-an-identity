using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer.Infrastructure.Json;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Notifications.Messages;

namespace TeacherIdentity.AuthServer.Notifications.WebHooks;

public class WebHookNotificationPublisher : INotificationPublisher
{
    private readonly IDbContextFactory<TeacherIdentityServerDbContext> _dbContextFactory;
    private readonly IMemoryCache _memoryCache;
    private readonly TimeSpan _webHooksCacheLifetime;

    public WebHookNotificationPublisher(
        IWebHookNotificationSender sender,
        IDbContextFactory<TeacherIdentityServerDbContext> dbContextFactory,
        IMemoryCache memoryCache,
        IOptions<WebHookOptions> optionsAccessor)
    {
        Sender = sender;
        _dbContextFactory = dbContextFactory;
        _memoryCache = memoryCache;
        _webHooksCacheLifetime = TimeSpan.FromSeconds(optionsAccessor.Value.WebHooksCacheDurationSeconds);
    }

    protected static JsonSerializerOptions SerializerOptions { get; } = new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        Converters =
        {
            new JsonStringEnumConverter(),
            new NotificationMessageConverter()
        },
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        {
            Modifiers =
            {
                Modifiers.OptionProperties
            }
        }
    };

    protected IWebHookNotificationSender Sender { get; }

    public async Task<WebHook[]> GetWebHooksForNotification(NotificationEnvelope notification)
    {
        var webhooks = await _memoryCache.GetOrCreateAsync(
            MemoryCacheKeys.WebHooks,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _webHooksCacheLifetime;

                await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
                return await dbContext.WebHooks.Where(wh => wh.Enabled).ToArrayAsync();
            });

        return webhooks!
            .Where(wh => wh.WebHookMessageTypes.HasFlag(MapNotificationMessageTypeToWebHookMessageType(notification.MessageType)))
            .ToArray();
    }

    public virtual async Task PublishNotification(NotificationEnvelope notification)
    {
        var payload = SerializeNotification(notification);

        var webHooks = await GetWebHooksForNotification(notification);

        foreach (var webHook in webHooks)
        {
            await Sender.SendNotification(notification.NotificationId, webHook.Endpoint, payload, webHook.Secret);
        }
    }

    private static WebHookMessageTypes MapNotificationMessageTypeToWebHookMessageType(string notificationMessageType) =>
        notificationMessageType switch
        {
            UserUpdatedMessage.MessageTypeName => WebHookMessageTypes.UserUpdated,
            UserMergedMessage.MessageTypeName => WebHookMessageTypes.UserMerged,
            UserCreatedMessage.MessageTypeName => WebHookMessageTypes.UserCreated,
            _ => WebHookMessageTypes.All,
        };

    protected string SerializeNotification(NotificationEnvelope notification) =>
        JsonSerializer.Serialize(notification, SerializerOptions);

    private class NotificationMessageConverter : JsonConverter<INotificationMessage>
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
