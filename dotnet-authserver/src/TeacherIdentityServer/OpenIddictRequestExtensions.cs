using System.Text;
using System.Text.Json;
using OpenIddict.Abstractions;

namespace TeacherIdentityServer;

public static class OpenIddictRequestExtensions
{
    public static OpenIddictRequest Deserialize(string serialized)
    {
        var bytes = Encoding.UTF8.GetBytes(serialized);
        var reader = new Utf8JsonReader(bytes.AsSpan());
        var element = JsonElement.ParseValue(ref reader);
        return new OpenIddictRequest(element);
    }

    public static string Serialize(this OpenIddictRequest request)
    {
        using var ms = new MemoryStream();
        using var writer = new Utf8JsonWriter(ms, new JsonWriterOptions() { Indented = false });
        request.WriteTo(writer);
        writer.Flush();
        return Encoding.UTF8.GetString(ms.ToArray());
    }
}
