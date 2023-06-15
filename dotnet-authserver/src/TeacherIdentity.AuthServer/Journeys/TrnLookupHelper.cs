using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Journeys;

public class TrnLookupHelper
{
    private static readonly TimeSpan _trnLookupTimeout = TimeSpan.FromSeconds(5);

    private readonly IDqtApiClient _dqtApiClient;
    private readonly ILogger<TrnLookupHelper> _logger;

    public TrnLookupHelper(
        IDqtApiClient dqtApiClient,
        ILogger<TrnLookupHelper> logger)
    {
        _dqtApiClient = dqtApiClient;
        _logger = logger;
    }

    public async Task<string?> LookupTrn(AuthenticationState authenticationState)
    {
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(_trnLookupTimeout);

        FindTeachersResponseResult? findTeachersResult;
        FindTeachersResponseResult[] findTeachersResults = Array.Empty<FindTeachersResponseResult>();

        try
        {
            var lookupResponse = await _dqtApiClient.FindTeachers(
                new FindTeachersRequest()
                {
                    DateOfBirth = authenticationState.DateOfBirth,
                    EmailAddress = authenticationState.EmailAddress,
                    FirstName = authenticationState.OfficialFirstName ?? authenticationState.FirstName,
                    LastName = authenticationState.OfficialLastName ?? authenticationState.LastName,
                    IttProviderName = authenticationState.IttProviderName,
                    NationalInsuranceNumber = NormalizeNino(authenticationState.NationalInsuranceNumber),
                    PreviousFirstName = authenticationState.PreviousOfficialFirstName,
                    PreviousLastName = authenticationState.PreviousOfficialLastName,
                    Trn = NormalizeTrn(authenticationState.StatedTrn)
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
            authenticationState.OnTrnLookupCompleted(findTeachersResult, trnLookupStatus);
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
}
