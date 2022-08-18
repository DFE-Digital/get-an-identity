using OpenIddict.Abstractions;

namespace TeacherIdentity.AuthServer.Oidc;

public class TeacherIdentityApplicationDescriptor : OpenIddictApplicationDescriptor
{
    public string? ServiceUrl { get; set; }
}
