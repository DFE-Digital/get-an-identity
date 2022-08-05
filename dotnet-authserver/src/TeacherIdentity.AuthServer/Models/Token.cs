using OpenIddict.EntityFrameworkCore.Models;

namespace TeacherIdentity.AuthServer.Models;

public class Token : OpenIddictEntityFrameworkCoreToken<string, Application, Authorization>
{
}
