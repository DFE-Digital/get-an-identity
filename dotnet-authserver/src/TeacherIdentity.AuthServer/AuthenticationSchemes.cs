using Microsoft.AspNetCore.Authentication.Cookies;
using OpenIddict.Server.AspNetCore;

namespace TeacherIdentity.AuthServer;

public static class AuthenticationSchemes
{
    public const string Oidc = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme;
    public const string Cookie = CookieAuthenticationDefaults.AuthenticationScheme;
    public const string Delegated = "Delegated";
}
