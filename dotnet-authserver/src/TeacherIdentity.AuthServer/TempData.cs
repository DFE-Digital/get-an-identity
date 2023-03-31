using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace TeacherIdentity.AuthServer;

public static class TempDataKeys
{
    public const string FlashSuccess = "FlashSuccess";
}

public static class TempDataExtensions
{
    public static void SetFlashSuccess(this ITempDataDictionary tempData, FlashSuccessData flashSuccessData)
    {
        tempData.Add(TempDataKeys.FlashSuccess, flashSuccessData.Serialize());
    }
}

public class FlashSuccessData
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
