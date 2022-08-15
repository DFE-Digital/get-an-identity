using OpenIddict.EntityFrameworkCore.Models;

namespace TeacherIdentity.AuthServer.Models;

public class Application : OpenIddictEntityFrameworkCoreApplication<string, Authorization, Token>
{
    public string? ServiceUrl { get; set; }
}
