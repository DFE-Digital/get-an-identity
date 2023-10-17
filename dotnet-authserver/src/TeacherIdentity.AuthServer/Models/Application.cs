using System.ComponentModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore.Models;

namespace TeacherIdentity.AuthServer.Models;

public class Application : OpenIddictEntityFrameworkCoreApplication<string, Authorization, Token>
{
    private const string EmptyJsonArray = "[]";
    private const string RedirectUriWildcardPlaceholder = "__";

    public static bool MatchUriPattern(string pattern, string uri, bool ignorePath)
    {
        if (!Uri.TryCreate(pattern, UriKind.Absolute, out _))
        {
            throw new ArgumentException("A valid absolute URI must be specified.", nameof(pattern));
        }

        if (!Uri.TryCreate(uri, UriKind.Absolute, out _))
        {
            throw new ArgumentException("A valid absolute URI must be specified.", nameof(uri));
        }

        var normalizedPattern = ignorePath ? RemovePathAndQuery(pattern) : pattern;
        var normalizedUri = ignorePath ? RemovePathAndQuery(uri) : uri;

        if (normalizedPattern.Equals(normalizedUri, StringComparison.Ordinal))
        {
            return true;
        }

        if (normalizedPattern.Contains(RedirectUriWildcardPlaceholder))
        {
            return Regex.IsMatch(normalizedUri, $"^{Regex.Escape(normalizedPattern).Replace(RedirectUriWildcardPlaceholder, ".*")}$");
        }

        return false;

        static string RemovePathAndQuery(string address) => new Uri(address).GetLeftPart(UriPartial.Authority);
    }

    public string? ServiceUrl { get; set; }

    [DefaultValue(TrnRequirementType.Required)]
    public TrnRequirementType TrnRequirementType { get; set; }

    public TrnMatchPolicy TrnMatchPolicy { get; set; }

    [DefaultValue(false)]
    public bool BlockProhibitedTeachers { get; set; }

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
