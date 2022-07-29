using OpenIddict.EntityFrameworkCore.Models;

namespace TeacherIdentityServer.Models;

public class Authorization : OpenIddictEntityFrameworkCoreAuthorization<string, Application, Token>
{
}
