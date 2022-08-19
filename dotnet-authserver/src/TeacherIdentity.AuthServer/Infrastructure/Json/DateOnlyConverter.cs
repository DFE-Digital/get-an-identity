using System.Text.Json;
using System.Text.Json.Serialization;

namespace TeacherIdentity.AuthServer.Infrastructure.Json;

public class DateOnlyConverter : JsonConverter<DateOnly>
{
    private const string DefaultDateFormat = "yyyy-MM-dd";

    private readonly string _dateFormat;

    public DateOnlyConverter()
        : this(DefaultDateFormat)
    {
    }

    public DateOnlyConverter(string dateFormat)
    {
        _dateFormat = dateFormat;
    }

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new InvalidOperationException($"Cannot get the value of a token type '{reader.TokenType}' as a string.");
        }

        var asString = reader.GetString();
        if (!DateOnly.TryParseExact(asString, _dateFormat, out var value))
        {
            throw new JsonException("The JSON value is not in a supported DateOnly format.");
        }

        return value;
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        var asString = value.ToString(_dateFormat);
        writer.WriteStringValue(asString);
    }
}
