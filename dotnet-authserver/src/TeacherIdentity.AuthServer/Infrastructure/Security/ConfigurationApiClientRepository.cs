namespace TeacherIdentity.AuthServer.Infrastructure.Security;

public class ConfigurationApiClientRepository : IApiClientRepository
{
    private const string ConfigurationSection = "ApiClients";

    private readonly ApiClient[] _clients;

    public ConfigurationApiClientRepository(IConfiguration configuration)
    {
        _clients = GetClientsFromConfiguration(configuration);
    }

    public ApiClient? GetClientByKey(string apiKey) => _clients.SingleOrDefault(c => c.ApiKeys!.Any(x => x == apiKey));

    private static ApiClient[] GetClientsFromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection(ConfigurationSection);
        return section.GetChildren().AsEnumerable()
            .Select((kvp, value) =>
            {
                var clientId = kvp.Key;
                var client = new ApiClient()
                {
                    ClientId = clientId,
                    ApiKeys = Array.Empty<string>()
                };
                kvp.Bind(client);

                return client;
            })
            .ToArray();
    }
}
