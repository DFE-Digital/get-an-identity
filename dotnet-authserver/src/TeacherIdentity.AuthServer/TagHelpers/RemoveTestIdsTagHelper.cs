using Microsoft.AspNetCore.Razor.TagHelpers;

namespace TeacherIdentity.AuthServer.TagHelpers;

[HtmlTargetElement(Attributes = "data-testid")]
public class RemoveTestIdsTagHelper : TagHelper
{
    private readonly IWebHostEnvironment _environment;

    public RemoveTestIdsTagHelper(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public override int Order => int.MinValue;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (_environment.IsProduction())
        {
            output.Attributes.RemoveAll("data-testid");
        }
    }
}
