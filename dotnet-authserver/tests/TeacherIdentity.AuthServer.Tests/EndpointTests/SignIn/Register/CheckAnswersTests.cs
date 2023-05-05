using AngleSharp.Html.Dom;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Oidc;
using User = TeacherIdentity.AuthServer.Models.User;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Register;

public class CheckAnswersTests : TestBase
{
    public CheckAnswersTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/register/check-answers");
    }

    [Fact]
    public async Task Get_MissingAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/register/check-answers");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(CustomScopes.DqtRead, trnRequirementType: null, HttpMethod.Get, "/sign-in/register/check-answers");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(_currentPageAuthenticationState(), CustomScopes.DqtRead, trnRequirementType: null, HttpMethod.Get, "/sign-in/register/check-answers");
    }

    [Theory]
    [MemberData(nameof(CheckAnswersState))]
    public async Task Get_ValidRequest_RendersExpectedContent(
        bool requiresTrnLookup,
        RegisterJourneyPage registerJourneyStage,
        bool awardedQts)
    {
        // Arrange
        var additionalScopes = requiresTrnLookup ? CustomScopes.DqtRead : null;

        var currentAuthenticationStateConfig = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(registerJourneyStage);

        var authStateHelper = await CreateAuthenticationStateHelper(currentAuthenticationStateConfig(awardedQts: awardedQts), additionalScopes);
        var authState = authStateHelper.AuthenticationState;

        if (requiresTrnLookup)
        {
            authState.OnTrnLookupCompleted(TestData.GenerateTrn(), TrnLookupStatus.Found);
        }

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/register/check-answers?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        var doc = await response.GetDocument();
        Assert.Equal(authState.EmailAddress, doc.GetSummaryListValueForKey("Email"));
        Assert.Equal(authState.MobileNumber, doc.GetSummaryListValueForKey("Mobile phone"));
        Assert.Equal($"{authState.FirstName} {authState.LastName}", doc.GetSummaryListValueForKey("Name"));
        Assert.Equal(authState.DateOfBirth?.ToString(Constants.DateFormat), doc.GetSummaryListValueForKey("Date of birth"));

        var hasNiNumberSet = registerJourneyStage > RegisterJourneyPage.HasNiNumber;
        var awardedQtsSet = registerJourneyStage > RegisterJourneyPage.HasQts;

        AssertRowValid("National Insurance number", requiresTrnLookup && hasNiNumberSet, authState.NationalInsuranceNumber, doc);
        AssertRowValid("Have you been awarded QTS?", requiresTrnLookup && awardedQtsSet, authState.AwardedQts == true ? "Yes" : "No", doc);
        AssertRowValid("Where did you get your QTS?", requiresTrnLookup && awardedQts, authState.IttProviderName, doc);
    }

    [Fact]
    public async Task Post_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/register/check-answers");
    }

    [Fact]
    public async Task Post_MissingAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/register/check-answers");
    }

    [Fact]
    public async Task Post_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(CustomScopes.DqtRead, trnRequirementType: null, HttpMethod.Post, "/sign-in/register/check-answers");
    }

    [Fact]
    public async Task Post_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(_currentPageAuthenticationState(), CustomScopes.DqtRead, trnRequirementType: null, HttpMethod.Post, "/sign-in/register/check-answers");
    }

    [Fact]
    public async Task Post_ValidForm_CreatesUserAndRedirectsToPostSignInUrl()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/check-answers?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        User? user = null;

        await TestData.WithDbContext(async dbContext =>
        {
            user = await dbContext.Users.Where(u => u.EmailAddress == authStateHelper.AuthenticationState.EmailAddress).SingleOrDefaultAsync();
            Assert.NotNull(user);
        });

        EventObserver.AssertEventsSaved(
            e =>
            {
                var userRegisteredEvent = Assert.IsType<UserRegisteredEvent>(e);
                Assert.Equal(Clock.UtcNow, userRegisteredEvent.CreatedUtc);
                Assert.Equal(user?.UserId, userRegisteredEvent.User.UserId);
            });

        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith(authStateHelper.AuthenticationState.PostSignInUrl, response.Headers.Location?.OriginalString);
    }

    private void AssertRowValid(string rowName, bool shouldExist, string? value, IHtmlDocument doc)
    {
        if (shouldExist)
        {
            Assert.Equal(value, doc.GetSummaryListValueForKey(rowName));
        }
        else
        {
            Assert.Null(doc.GetSummaryListRowForKey(rowName));
        }
    }

    public static TheoryData<bool, RegisterJourneyPage, bool> CheckAnswersState { get; } = new()
    {
        // requiresTrnLookup, register journey stage, AwardedQts
        { false, RegisterJourneyPage.CheckAnswers, false },
        { true, RegisterJourneyPage.CheckAnswers, false },
        { true, RegisterJourneyPage.CheckAnswers, true },
        { true, RegisterJourneyPage.HasQts, false },
        { true, RegisterJourneyPage.HasNiNumber, false },
    };

    private readonly AuthenticationStateConfigGenerator _currentPageAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.CheckAnswers);
}
