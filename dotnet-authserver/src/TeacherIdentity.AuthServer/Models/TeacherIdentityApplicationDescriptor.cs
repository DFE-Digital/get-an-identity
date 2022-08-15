using OpenIddict.Abstractions;

namespace TeacherIdentity.AuthServer.Models;

public class TeacherIdentityApplicationDescriptor : OpenIddictApplicationDescriptor
{
    public string? ServiceUrl { get; set; }
}
