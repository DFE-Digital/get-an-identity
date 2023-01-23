namespace TeacherIdentity.AuthServer.Services.DqtApi;

public class GetIttProvidersResponse
{
    public required IttProvider[] IttProviders { get; init; }
}

public record IttProvider
{
    public required string ProviderName { get; init; }
    public string? Ukprn { get; init; }
}
