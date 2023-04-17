using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace TeacherIdentity.AuthServer.Tests;

public static partial class AssertEx
{
    public static JsonSerializerOptions SerializerOptions { get; } = new JsonSerializerOptions()
    {
        Converters =
        {
            new JsonStringEnumConverter()
        },
        PropertyNameCaseInsensitive = true
    };

    public static void JsonEquals(string expected, string actual) =>
        JsonEquals(JsonNode.Parse(expected), JsonNode.Parse(actual));

    public static void JsonObjectEquals(JsonDocument expected, object actual) =>
        JsonEquals(JsonSerializer.SerializeToNode(expected.RootElement), JsonSerializer.SerializeToNode(actual, SerializerOptions));

    public static void JsonObjectEquals(object expected, object actual) =>
        JsonEquals(JsonSerializer.SerializeToNode(expected, SerializerOptions), JsonSerializer.SerializeToNode(actual, SerializerOptions));

    public static void JsonEquals(JsonNode? expected, JsonNode? actual)
    {
        if (expected is null && actual is null)
        {
            return;
        }
        else if (expected is null && actual is not null)
        {
            var path = GetPrintablePath(expected?.GetPath());
            throw new JsonNotEqualException($"JSON not equal:\nExpected: null\nActual: <not null>\nPath: {path}");
        }
        else if (expected is not null && actual is null)
        {
            var path = GetPrintablePath(expected?.GetPath());
            throw new JsonNotEqualException($"JSON not equal:\nExpected: <not null>\nActual: null\nPath: {path}");
        }

        Debug.Assert(expected is not null);
        Debug.Assert(actual is not null);

        if (expected is JsonValue expectedJValue)
        {
            if (actual is not JsonValue actualJValue)
            {
                ThrowMismatchedTypeException("JsonValue", actual.GetType().Name, GetPrintablePath(expected.GetPath()));
            }

            // We don't care if the types are different, only if the generated JSON is different
            var expectedValue = expectedJValue.ToString();
            var actualValue = actualJValue.ToString();

            if (StringComparer.Ordinal.Compare(expectedValue, actualValue) != 0)
            {
                ThrowNotEqualException(expectedValue, actualValue, GetPrintablePath(expected.GetPath()));
            }
        }
        else if (expected is JsonArray expectedJArray)
        {
            if (actual is not JsonArray actualJArray)
            {
                ThrowMismatchedTypeException("JsonArray", actual.GetType().Name, GetPrintablePath(expected.GetPath()));
            }

            if (expectedJArray.Count != actualJArray.Count)
            {
                throw new JsonNotEqualException(
                    $"JSON not equal\nExpected {expectedJArray.Count} elements\nGot: {actualJArray.Count}\nPath: {GetPrintablePath(expected.GetPath())}");
            }

            for (var i = 0; i < expectedJArray.Count; i++)
            {
                var expectedElement = expectedJArray[i]!;
                var actualElement = actualJArray[i]!;

                JsonEquals(expectedElement, actualElement);
            }
        }
        else if (expected is JsonObject expectedJObject)
        {
            if (actual is not JsonObject actualJObject)
            {
                ThrowMismatchedTypeException("JsonObject", actual.GetType().Name, GetPrintablePath(expected.GetPath()));
            }

            // Compare each property on `expected` with `actual`
            foreach (var (name, expectedProp) in (IDictionary<string, JsonNode?>)expectedJObject)
            {
                var actualProp = actualJObject[name];
                if (actualProp == null)
                {
                    throw new JsonNotEqualException(
                        $"JSON not equal\nMissing property: '{name}'\nPath: {GetPrintablePath(expected.GetPath())}");
                }

                JsonEquals(expectedProp, actualProp);
            }

            // Check `actual` doesn't have extra properties over `expected`
            foreach (var (name, actualProp) in (IDictionary<string, JsonNode?>)actualJObject)
            {
                var expectedProp = expectedJObject[name];
                if (expectedProp == null)
                {
                    throw new JsonNotEqualException(
                        $"JSON not equal\nExtra property {name}\nPath: {GetPrintablePath(expected.GetPath())}");
                }
            }
        }
        else
        {
            throw new NotSupportedException($"Don't know how to compare JTokens of type '{expected.GetType().Name}'.");
        }

        string GetPrintablePath(string? path) => string.IsNullOrEmpty(path) ? "(root)" : path;

        [DoesNotReturn]
        void ThrowNotEqualException(string expectedValue, string actualValue, string path) =>
            throw new JsonNotEqualException($"JSON not equal:\nExpected: {expectedValue}\nActual: {actualValue}\nPath: {path}");

        [DoesNotReturn]
        void ThrowMismatchedTypeException(string expectedType, string actualType, string path) =>
            throw new JsonNotEqualException($"Token types not equal\nExpected: {expectedType}\nActual: {actualType}\nPath: {path}");
    }

    public class JsonNotEqualException : Exception
    {
        public JsonNotEqualException(string message)
            : base(message)
        {
        }
    }
}
