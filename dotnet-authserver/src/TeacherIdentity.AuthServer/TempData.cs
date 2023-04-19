using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace TeacherIdentity.AuthServer;

public static class TempDataKeys
{
    public const string FlashSuccess = "FlashSuccess";
}

public static class TempDataExtensions
{
    public static void SetFlashSuccess(this ITempDataDictionary tempData, string heading, string? message = null)
    {
        tempData.Add(TempDataKeys.FlashSuccess, new FlashSuccessData(heading, message).Serialize());
    }

    public static bool TryGetFlashSuccess(this ITempDataDictionary tempData, [NotNullWhen(true)] out (string Heading, string? Message)? data)
    {
        data = null;
        if (tempData.TryGetValue(TempDataKeys.FlashSuccess, out object? flashSuccessObject) && flashSuccessObject is string flashSuccessString)
        {
            var flashSuccessData = FlashSuccessData.Deserialize(flashSuccessString);
            data = (flashSuccessData.Heading, flashSuccessData.Message);
            return true;
        }
        return false;
    }

    private class FlashSuccessData
    {
        public string Heading { get; set; }
        public string? Message { get; set; }

        public FlashSuccessData(string heading, string? message = null)
        {
            Heading = heading;
            Message = message;
        }

        public string Serialize() => JsonSerializer.Serialize(this);

        public static FlashSuccessData Deserialize(string serialized) =>
            JsonSerializer.Deserialize<FlashSuccessData>(serialized) ??
            throw new ArgumentException($"Serialized {nameof(FlashSuccessData)} is not valid.", nameof(serialized));
    }
}

