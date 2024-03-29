using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Oidc;

public class ClientConfiguration
{
    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; }
    public bool EnableAuthorizationCodeGrant { get; set; } = true;
    public bool EnableClientCredentialsGrant { get; set; } = false;
    public string[]? RedirectUris { get; set; }
    public string[]? PostLogoutRedirectUris { get; set; }
    public required string DisplayName { get; set; }
    public string? ServiceUrl { get; set; }
    public required TrnRequirementType TrnRequirementType { get; set; }
    public bool BlockProhibitedTeachers { get; set; }
    public required TrnMatchPolicy TrnMatchPolicy { get; set; }
    public bool RaiseTrnResolutionSupportTickets { get; set; }
    public string[]? Scopes { get; set; }
}
