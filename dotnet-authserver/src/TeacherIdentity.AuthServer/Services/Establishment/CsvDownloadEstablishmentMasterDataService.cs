using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace TeacherIdentity.AuthServer.Services.Establishment;

public class CsvDownloadEstablishmentMasterDataService : IEstablishmentMasterDataService
{
    private readonly HttpClient _httpClient;
    private readonly IClock _clock;

    public CsvDownloadEstablishmentMasterDataService(
        HttpClient httpClient,
        IClock clock)
    {
        _httpClient = httpClient;
        _clock = clock;
    }

    public async IAsyncEnumerable<string?> GetEstablishmentWebsites()
    {
        var filename = GetLatestEstablishmentsCsvFilename();
        using var response = await _httpClient.GetAsync(filename, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true });

        await foreach (var item in csv.GetRecordsAsync<EstablishmentCsvRowMinimum>())
        {
            yield return item.SchoolWebsite;
        }
    }

    private string GetLatestEstablishmentsCsvFilename()
    {
        var filename = $"edubasealldata{_clock.UtcNow:yyyyMMdd}.csv";
        return filename;
    }
}
