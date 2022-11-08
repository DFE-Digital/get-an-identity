using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Optional;
using Optional.Unsafe;
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

    protected static JsonSerializerSettings SerializerSettings { get; } = new JsonSerializerSettings()
    {
        Converters =
        {
            new StringEnumConverter(),
            new DateOnlyJsonConverter()
        },
        ContractResolver = new ContractResolver()
    };

    protected IWebHookNotificationSender Sender { get; }

    public Task<WebHook[]> GetWebHooksForNotification(NotificationEnvelope notification) =>
        _memoryCache.GetOrCreateAsync(
            MemoryCacheKeys.WebHooks,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _webHooksCacheLifetime;

                await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
                return await dbContext.WebHooks.Where(wh => wh.Enabled).ToArrayAsync();
            })!;

    public virtual async Task PublishNotification(NotificationEnvelope notification)
    {
        var payload = SerializeNotification(notification);

        var webHooks = await GetWebHooksForNotification(notification);

        foreach (var webHook in webHooks)
        {
            await Sender.SendNotification(notification.NotificationId, webHook.Endpoint, payload, webHook.Secret);
        }
    }

    protected string SerializeNotification(NotificationEnvelope notification) =>
        JsonConvert.SerializeObject(notification, SerializerSettings);

    private class ContractResolver : CamelCasePropertyNamesContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            // If the property is an Option<T> then only serialize it if it has a value
            if (property.PropertyType?.IsGenericType == true && property.PropertyType.GetGenericTypeDefinition() == typeof(Option<>))
            {
                var innerType = property.PropertyType.GetGenericArguments()[0];
                property.ShouldSerialize = CreateShouldSerializePredicate(member.DeclaringType!, innerType, property.PropertyName!);
                property.Converter = (JsonConverter)Activator.CreateInstance(typeof(OptionJsonConverter<>).MakeGenericType(innerType))!;
            }

            return property;
        }

        private static Predicate<object> CreateShouldSerializePredicate(Type objectType, Type innerPropertyType, string propertyName)
        {
            var objParameter = Expression.Parameter(typeof(object));

            // Create a delegate that corresponds to:
            // return ((Option<TProperty>)((T)object).Property).HasValue;

            return (Predicate<object>)Expression.Lambda(
                typeof(Predicate<object>),
                Expression.Block(
                    Expression.Property(
                        Expression.Convert(
                            Expression.Property(
                                Expression.Convert(objParameter, objectType),
                                propertyName
                            ),
                            typeof(Option<>).MakeGenericType(innerPropertyType)),
                        "HasValue")),
                objParameter).Compile();
        }
    }

    private class OptionJsonConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Option<T>);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var value = serializer.Deserialize<T>(reader);
            return Option.Some(value);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            // Note this will throw if HasValue is false, but we should never get here in that case
            var option = (Option<T>)value!;
            serializer.Serialize(writer, option.ValueOrFailure());
        }
    }

    private class DateOnlyJsonConverter : JsonConverter
    {
        private const string Format = "yyyy-MM-dd";

        public override bool CanConvert(Type objectType) => objectType == typeof(DateOnly) || objectType == typeof(DateOnly?);

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var asString = reader.ReadAsString();

            if (asString is null)
            {
                return default;
            }

            return DateOnly.ParseExact(asString, Format);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
            }
            else
            {
                var asString = ((DateOnly)value).ToString(Format);
                writer.WriteValue(asString);
            }
        }
    }
}
