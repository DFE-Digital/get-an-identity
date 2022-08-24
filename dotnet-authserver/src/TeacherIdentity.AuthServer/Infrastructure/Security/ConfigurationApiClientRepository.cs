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

        var clients = new List<ApiClient>();
        var clientIds = new HashSet<string>();
        var apiKeys = new HashSet<string>();

        foreach (var s in section.GetChildren().AsEnumerable())
        {
            var client = new ApiClient();
            s.Bind(client);
            client.ApiKeys ??= Array.Empty<string>();

            if (string.IsNullOrEmpty(client.ClientId))
            {
                throw new Exception($"Missing {nameof(client.ClientId)}.");
            }

            if (!clientIds.Add(client.ClientId))
            {
                throw new Exception($"Duplicate client configuration found for '{client.ClientId}'.");
            }

            foreach (var apiKey in client.ApiKeys)
            {
                if (!apiKeys.Add(apiKey))
                {
                    throw new Exception($"Duplicate API key found '{apiKey}'.");
                }
            }

            clients.Add(client);
        }

        return clients.ToArray();
    }
}
