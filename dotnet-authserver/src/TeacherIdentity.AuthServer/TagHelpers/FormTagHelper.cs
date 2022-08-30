using Microsoft.AspNetCore.Razor.TagHelpers;

namespace TeacherIdentity.AuthServer.TagHelpers;

[HtmlTargetElement("form")]
public class FormTagHelper : TagHelper
{
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.Attributes.Add("novalidate", string.Empty);
    }
}
