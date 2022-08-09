using AngleSharp;
using AngleSharp.Html.Dom;

namespace TeacherIdentity.AuthServer.Tests;

public static class HttpResponseMessageExtensions
{
    public static async Task<IHtmlDocument> GetDocument(this HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        var browsingContext = BrowsingContext.New(AngleSharp.Configuration.Default);
        return (IHtmlDocument)await browsingContext.OpenAsync(req => req.Content(content));
    }
}
