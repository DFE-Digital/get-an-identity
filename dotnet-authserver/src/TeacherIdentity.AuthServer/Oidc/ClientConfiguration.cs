namespace TeacherIdentity.AuthServer.Oidc;

public class ClientConfiguration
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string[]? RedirectUris { get; set; }
    public string? DisplayName { get; set; }
    public string? ServiceUrl { get; set; }
    public string[]? Scopes { get; set; }
}
