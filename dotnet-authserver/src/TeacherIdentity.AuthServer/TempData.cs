using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace TeacherIdentity.AuthServer;

public static class TempDataKeys
{
    public const string FlashSuccess = "FlashSuccess";
}

public static class TempDataExtensions
{
    public static void SetFlashSuccess(this ITempDataDictionary tempData, string heading)
    {
        tempData.Add(TempDataKeys.FlashSuccess, heading);
    }
}
