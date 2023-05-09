using System.ComponentModel;
using System.Text.Json;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore.Models;

namespace TeacherIdentity.AuthServer.Models;

public class Application : OpenIddictEntityFrameworkCoreApplication<string, Authorization, Token>
{
    private const string EmptyJsonArray = "[]";

    public string? ServiceUrl { get; set; }

    [DefaultValue(TrnRequirementType.Legacy)]
    public TrnRequirementType TrnRequirementType { get; set; }

    [DefaultValue(false)]
    public bool RaiseTrnResolutionSupportTickets { get; set; }

    public string[] GetGrantTypes() => GetPermissions()
        .Where(p => p.StartsWith(OpenIddictConstants.Permissions.Prefixes.GrantType))
        .Select(p => p[OpenIddictConstants.Permissions.Prefixes.GrantType.Length..])
        .ToArray();

    public string[] GetPermissions() => DeserializeJsonStringArray(Permissions ?? EmptyJsonArray).ToArray();

    public string[] GetScopes() => GetPermissions()
        .Where(p => p.StartsWith(OpenIddictConstants.Permissions.Prefixes.Scope))
        .Select(p => p[OpenIddictConstants.Permissions.Prefixes.Scope.Length..])
        .ToArray();

    public string[] GetRedirectUris() => DeserializeJsonStringArray(RedirectUris ?? EmptyJsonArray).ToArray();

    public string[] GetPostLogoutRedirectUris() => DeserializeJsonStringArray(PostLogoutRedirectUris ?? EmptyJsonArray).ToArray();

    private static string[] DeserializeJsonStringArray(string value) =>
        JsonSerializer.Deserialize<string[]>(value) ?? Array.Empty<string>();
}
