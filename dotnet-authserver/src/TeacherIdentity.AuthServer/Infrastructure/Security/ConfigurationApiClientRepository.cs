namespace TeacherIdentity.AuthServer.Infrastructure.Security;

public class ConfigurationApiClientRepository : IApiClientRepository
{
    private const string ConfigurationSection = "ApiClients";

    private readonly ApiClient[] _clients;

    public ConfigurationApiClientRepository(IConfiguration configuration)
    {
        _clients = GetClientsFromConfiguration(configuration);
    }

    public ApiClient? GetClientByClientId(string clientId) => _clients.SingleOrDefault(c => c.ClientId == clientId);

    public ApiClient? GetClientByKey(string apiKey) => _clients.SingleOrDefault(c => c.ApiKeys!.Any(x => x == apiKey));

    private static ApiClient[] GetClientsFromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection(ConfigurationSection);
        return section.GetChildren().AsEnumerable()
            .Select(section =>
            {
                var client = new ApiClient();
                section.Bind(client);
                return client;
            })
            .ToArray();
    }
}
