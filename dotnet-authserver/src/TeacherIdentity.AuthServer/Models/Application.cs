using OpenIddict.EntityFrameworkCore.Models;

namespace TeacherIdentity.AuthServer.Models;

public class Application : OpenIddictEntityFrameworkCoreApplication<string, Authorization, Token>
{
    public string? ServiceUrl { get; set; }
    public string? PostSignInMessage { get; set; }

    public string GetPostSignInMessageAsTitleCase()
    {
        if (string.IsNullOrEmpty(PostSignInMessage))
        {
            return string.Empty;
        }

        var titlecase = string.Concat(char.ToUpper(PostSignInMessage[0]), PostSignInMessage[1..]);
        return titlecase;
    }
}
