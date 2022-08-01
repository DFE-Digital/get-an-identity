using OpenIddict.EntityFrameworkCore.Models;

namespace TeacherIdentityServer.Models;

public class Application : OpenIddictEntityFrameworkCoreApplication<string, Authorization, Token>
{
}
