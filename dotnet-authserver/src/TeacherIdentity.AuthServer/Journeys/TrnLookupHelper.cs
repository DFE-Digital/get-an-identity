using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Journeys;

public class TrnLookupHelper
{
    private const string DebugLogsContainerName = "debug-logs";

    private static readonly TimeSpan _trnLookupTimeout = TimeSpan.FromSeconds(5);

    private readonly IDqtApiClient _dqtApiClient;
    private readonly ILogger<TrnLookupHelper> _logger;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly IClock _clock;
    private readonly bool _dqtSynchronizationEnabled;

    public TrnLookupHelper(
        IDqtApiClient dqtApiClient,
        IConfiguration configuration,
        ILogger<TrnLookupHelper> logger,
        BlobServiceClient blobServiceClient,
        IClock clock)
    {
        _dqtApiClient = dqtApiClient;
        _logger = logger;
        _blobServiceClient = blobServiceClient;
        _clock = clock;
        _dqtSynchronizationEnabled = configuration.GetValue("DqtSynchronizationEnabled", false);
    }

    public async Task<string?> LookupTrn(AuthenticationState authenticationState)
    {
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(_trnLookupTimeout);

        FindTeachersResponseResult? findTeachersResult;
        FindTeachersResponseResult[] findTeachersResults = Array.Empty<FindTeachersResponseResult>();

        try
        {
            authenticationState.TryGetOAuthState(out var oAuthState);

            var lookupResponse = await _dqtApiClient.FindTeachers(
                new FindTeachersRequest()
                {
                    DateOfBirth = authenticationState.DateOfBirth,
                    EmailAddress = authenticationState.EmailAddress,
                    FirstName = authenticationState.FirstName,
                    LastName = authenticationState.LastName,
                    IttProviderName = authenticationState.IttProviderName,
                    NationalInsuranceNumber = NormalizeNino(authenticationState.NationalInsuranceNumber),
                    Trn = NormalizeTrn(authenticationState.StatedTrn),
                    TrnMatchPolicy = oAuthState?.TrnMatchPolicy
                },
                cts.Token);

            findTeachersResults = lookupResponse.Results.ToArray();
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "Timeout calling DQT API.");
        }
        finally
        {
            // We're deliberately setting the result here, even on failure to ensure we don't hold onto a
            // previously-found TRN that may now be invalid.

            TrnLookupStatus trnLookupStatus;
            (findTeachersResult, trnLookupStatus) = ResolveTrn(findTeachersResults, authenticationState);
            if (findTeachersResult is not null)
            {
                await CheckDqtTeacherNames(findTeachersResult);
            }

            authenticationState.OnTrnLookupCompleted(findTeachersResult, trnLookupStatus, _dqtSynchronizationEnabled);
        }

        return findTeachersResult?.Trn;
    }

    public (FindTeachersResponseResult? Trn, TrnLookupStatus TrnLookupStatus) ResolveTrn(
        FindTeachersResponseResult[] findTeachersResults,
        AuthenticationState authenticationState) =>
        (findTeachersResults, authenticationState) switch
        {
            ({ Length: 1 }, _) => (findTeachersResults.Single(), TrnLookupStatus.Found),
            ({ Length: > 1 }, _) => (null, TrnLookupStatus.Pending),
            (_, { StatedTrn: not null } or { AwardedQts: true }) => (null, TrnLookupStatus.Pending),
            _ => (null, TrnLookupStatus.None)
        };

    private static string? NormalizeNino(string? nino)
    {
        if (string.IsNullOrEmpty(nino))
        {
            return null;
        }

        return new string(nino.Where(char.IsAsciiLetterOrDigit).ToArray()).ToUpper();
    }

    private static string? NormalizeTrn(string? trn)
    {
        if (string.IsNullOrEmpty(trn))
        {
            return null;
        }

        return new string(trn.Where(char.IsAsciiDigit).ToArray());
    }

    private async Task CheckDqtTeacherNames(FindTeachersResponseResult teacher)
    {
        if (string.IsNullOrEmpty(teacher.FirstName) || string.IsNullOrEmpty(teacher.LastName))
        {
            try
            {
                var blobName = $"{nameof(TrnLookupHelper)}-{_clock.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid()}.json";
                var blobClient = await GetBlobClient(blobName);
                var debugLog = JsonSerializer.Serialize(teacher);
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(debugLog));
                await blobClient.UploadAsync(stream);
            }
            catch (Exception)
            {
                // Don't want logging issues to abort whole process
            }
        }
    }

    private async Task<BlobClient> GetBlobClient(string blobName)
    {
        var blobContainerClient = await GetBlobContainerClient();
        return blobContainerClient.GetBlobClient(blobName);
    }

    private async Task<BlobContainerClient> GetBlobContainerClient()
    {
        var blobContainerClient = _blobServiceClient.GetBlobContainerClient(DebugLogsContainerName);
        await blobContainerClient.CreateIfNotExistsAsync();
        return blobContainerClient;
    }
}
