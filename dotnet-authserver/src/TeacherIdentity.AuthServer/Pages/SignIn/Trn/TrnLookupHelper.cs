using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

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

        string? lookupResult = null;

        try
        {
            var lookupResponse = await _dqtApiClient.FindTeachers(
                new FindTeachersRequest()
                {
                    DateOfBirth = authenticationState.DateOfBirth,
                    EmailAddress = authenticationState.EmailAddress,
                    FirstName = authenticationState.OfficialFirstName,
                    LastName = authenticationState.OfficialLastName,
                    IttProviderName = authenticationState.IttProviderName,
                    NationalInsuranceNumber = authenticationState.NationalInsuranceNumber,
                    PreviousFirstName = authenticationState.PreviousOfficialFirstName,
                    PreviousLastName = authenticationState.PreviousOfficialLastName,
                    Trn = authenticationState.StatedTrn
                },
                cts.Token);

            lookupResult = lookupResponse.Results.Length == 1 ? lookupResponse.Results[0].Trn : null;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "Timeout calling DQT API.");
            return null;
        }
        finally
        {
            // We're deliberately setting the result here, even on failure to ensure we don't hold onto a
            // previously-found TRN that may now be invalid.

            var trnLookupStatus = GetTrnLookupStatus(lookupResult, authenticationState);
            authenticationState.OnTrnLookupCompleted(lookupResult, trnLookupStatus);
        }

        return lookupResult;
    }

    public TrnLookupStatus GetTrnLookupStatus(string? trnLookupResult, AuthenticationState authenticationState) =>
        trnLookupResult is not null ? TrnLookupStatus.Found :
            authenticationState.StatedTrn is not null || authenticationState.AwardedQts == true ? TrnLookupStatus.Pending :
            TrnLookupStatus.None;
}
