using OpenIddict.EntityFrameworkCore.Models;

namespace TeacherIdentity.AuthServer.Models;

public class Authorization : OpenIddictEntityFrameworkCoreAuthorization<string, Application, Token>
{
}
