using AngleSharp.Html.Dom;
using Xunit.Sdk;

namespace TeacherIdentity.AuthServer.Tests;

public static partial class AssertEx
{
    public static void DocumentHasError(IHtmlDocument doc, string fieldName, string expectedMessage)
    {
        var errorElementId = $"{fieldName}-error";
        var errorElement = doc.GetElementById(errorElementId);

        if (errorElement == null)
        {
            throw new XunitException($"No error found for field '{fieldName}'.");
        }

        var vht = errorElement.GetElementsByTagName("span")[0];
        var errorMessage = errorElement.InnerHtml[vht.OuterHtml.Length..];
        Assert.Equal(expectedMessage, errorMessage);
    }

    public static async Task ResponseHasError(
        HttpResponseMessage response,
        string fieldName,
        string expectedMessage,
        int expectedStatusCode = 400)
    {
        Assert.Equal(expectedStatusCode, (int)response.StatusCode);

        var doc = await response.GetDocument();
        DocumentHasError(doc, fieldName, expectedMessage);
    }
}
