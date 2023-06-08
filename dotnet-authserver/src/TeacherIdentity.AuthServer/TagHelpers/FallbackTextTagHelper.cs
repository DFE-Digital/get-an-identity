using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace TeacherIdentity.AuthServer.TagHelpers;

[HtmlTargetElement("span", Attributes = "fallback-text")]
public class FallbackTextTagHelper : TagHelper
{
    [HtmlAttributeName("fallback-text")]
    public string? FallbackText { get; set; }

    public override async void Process(TagHelperContext context, TagHelperOutput output)
    {
        if ((await output.GetChildContentAsync()).IsEmptyOrWhiteSpace)
        {
            output.Content.SetHtmlContent(FallbackText);
            output.AddClass("gai-summary-row-fallback-text", HtmlEncoder.Default);
        }
    }
}
