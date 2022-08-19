namespace TeacherIdentity.AuthServer.Clients;

public class ClientConfiguration
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string[]? RedirectUris { get; set; }
    public string? DisplayName { get; set; }
    public string? ServiceUrl { get; set; }
}
