using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace TeacherIdentity.AuthServer;

public static class HtmlContentExtensions
{
    public static string RenderToString(this IHtmlContent content)
    {
        using var writer = new StringWriter();
        content.WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }
}
