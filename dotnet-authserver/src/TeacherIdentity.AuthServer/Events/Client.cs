using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Events;

public record Client
{
    public required string ClientId { get; init; }
    public required string DisplayName { get; init; }
    public required string? ServiceUrl { get; init; }
    public required string[] RedirectUris { get; init; }
    public required string[] PostLogoutRedirectUris { get; init; }
    public required string[] Scopes { get; init; }

    public static Client FromDescriptor(TeacherIdentityApplicationDescriptor descriptor) => new()
    {
        ClientId = descriptor.ClientId ?? throw new ArgumentException($"{nameof(descriptor.ClientId)} cannot be null."),
        DisplayName = descriptor.DisplayName ?? throw new ArgumentException($"{nameof(descriptor.DisplayName)} cannot be null."),
        ServiceUrl = descriptor.ServiceUrl,
        RedirectUris = descriptor.RedirectUris.Select(u => u.ToString()).ToArray(),
        PostLogoutRedirectUris = descriptor.PostLogoutRedirectUris.Select(u => u.ToString()).ToArray(),
        Scopes = descriptor.Permissions.Where(p => p.StartsWith("scp:")).Select(p => p["scp:".Length..]).ToArray()
    };
}
