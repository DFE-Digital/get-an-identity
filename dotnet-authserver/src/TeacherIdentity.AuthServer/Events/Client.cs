using OpenIddict.Abstractions;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Events;

public record Client
{
    public required string ClientId { get; init; }
    public required string? DisplayName { get; init; }
    public required string? ServiceUrl { get; init; }
    public required TrnRequirementType TrnRequirementType { get; init; }
    public required TrnMatchPolicy TrnMatchPolicy { get; set; }
    public required bool RaiseTrnResolutionSupportTickets { get; init; }
    public required string[] RedirectUris { get; init; }
    public required string[] PostLogoutRedirectUris { get; init; }
    public required string[] Scopes { get; init; }

    public static Client FromDescriptor(TeacherIdentityApplicationDescriptor descriptor) => new()
    {
        ClientId = descriptor.ClientId ?? throw new ArgumentException($"{nameof(descriptor.ClientId)} cannot be null."),
        DisplayName = descriptor.DisplayName,
        ServiceUrl = descriptor.ServiceUrl,
        TrnRequirementType = descriptor.TrnRequirementType,
        TrnMatchPolicy = descriptor.TrnMatchPolicy,
        RaiseTrnResolutionSupportTickets = descriptor.RaiseTrnResolutionSupportTickets,
        RedirectUris = descriptor.RedirectUris.Select(u => u.ToString()).ToArray(),
        PostLogoutRedirectUris = descriptor.PostLogoutRedirectUris.Select(u => u.ToString()).ToArray(),
        Scopes = descriptor.Permissions
            .Where(p => p.StartsWith(OpenIddictConstants.Permissions.Prefixes.Scope))
            .Select(p => p[OpenIddictConstants.Permissions.Prefixes.Scope.Length..])
            .ToArray()
    };

    public static Client FromModel(Models.Application model) => new()
    {
        ClientId = model.ClientId!,
        DisplayName = model.DisplayName,
        ServiceUrl = model.ServiceUrl,
        TrnRequirementType = model.TrnRequirementType,
        TrnMatchPolicy = model.TrnMatchPolicy,
        RaiseTrnResolutionSupportTickets = model.RaiseTrnResolutionSupportTickets,
        RedirectUris = model.GetRedirectUris(),
        PostLogoutRedirectUris = model.GetPostLogoutRedirectUris(),
        Scopes = model.GetScopes()
    };

    public static implicit operator Client(Models.Application model) => FromModel(model);
}
