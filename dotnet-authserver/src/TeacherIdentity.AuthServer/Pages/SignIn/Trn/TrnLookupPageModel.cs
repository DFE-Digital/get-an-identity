using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Trn;

public abstract class TrnLookupPageModel : PageModel
{
    private static readonly TimeSpan _trnLookupTimeout = TimeSpan.FromSeconds(5);

    private readonly ILogger<TrnLookupPageModel> _logger;

    protected TrnLookupPageModel(
        IIdentityLinkGenerator linkGenerator,
        IDqtApiClient dqtApiClient,
        ILogger<TrnLookupPageModel> logger)
    {
        LinkGenerator = linkGenerator;
        DqtApiClient = dqtApiClient;
        _logger = logger;
    }

    public IIdentityLinkGenerator LinkGenerator { get; }

    public IDqtApiClient DqtApiClient { get; }

    protected async Task<IActionResult?> TryFindTrn()
    {
        var authenticationState = HttpContext.GetAuthenticationState();

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(_trnLookupTimeout);

        string? lookupResult = null;

        try
        {
            var lookupResponse = await DqtApiClient.FindTeachers(
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

        return lookupResult is not null ? new RedirectResult(LinkGenerator.TrnCheckAnswers()) : null;
    }

    public static TrnLookupStatus GetTrnLookupStatus(string? trnLookupResult, AuthenticationState authenticationState) =>
        trnLookupResult is not null ? TrnLookupStatus.Found :
            authenticationState.StatedTrn is not null || authenticationState.AwardedQts == true ? TrnLookupStatus.Pending :
            TrnLookupStatus.None;
}
