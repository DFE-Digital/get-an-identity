using OpenIddict.EntityFrameworkCore.Models;

namespace TeacherIdentityServer.Models;

public class Token : OpenIddictEntityFrameworkCoreToken<string, Application, Authorization>
{
}
