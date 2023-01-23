namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Trn;

public class CheckAnswersTests : TestBase
{
    public CheckAnswersTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/trn/check-answers");
    }

    [Fact]
    public async Task Get_MissingAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/trn/check-answers");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(HttpMethod.Get, "/sign-in/trn/check-answers");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(c => c.OfficialNameSet(), HttpMethod.Get, "/sign-in/trn/check-answers");
    }

    [Theory]
    [MemberData(nameof(TrnIdentityData))]
    public async Task Get_ValidRequest_RendersExpectedContent(
        bool haveNiNumber = false,
        bool awardedQts = false,
        bool haveIttProvider = false)
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.TrnIdentityJourneyComplete());
        var authState = authStateHelper.AuthenticationState;

        authState.OnHaveNationalInsuranceNumberSet(haveNiNumber);
        authState.OnAwardedQtsSet(awardedQts);
        authState.OnHaveIttProviderSet(haveIttProvider);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/check-answers?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Equal(authState.EmailAddress, doc.GetSummaryListValueForKey("Email address"));
        Assert.Equal($"{authState.OfficialFirstName} {authState.OfficialLastName}", doc.GetSummaryListValueForKey("Name"));
        Assert.Equal($"{authState.PreviousOfficialFirstName} {authState.PreviousOfficialLastName}", doc.GetSummaryListValueForKey("Previous name"));
        Assert.Equal($"{authState.FirstName} {authState.LastName}", doc.GetSummaryListValueForKey("Preferred name"));
        Assert.Equal(authState.DateOfBirth?.ToString("dd MMMM yyyy"), doc.GetSummaryListValueForKey("Date of birth"));
        Assert.Equal(haveNiNumber ? authState.NationalInsuranceNumber : "Not given", doc.GetSummaryListValueForKey("National Insurance number"));
        Assert.Equal(awardedQts ? "Yes" : "No", doc.GetSummaryListValueForKey("Have you been awarded QTS?"));

        if (awardedQts)
        {
            Assert.Equal(haveIttProvider ? authState.IttProviderName : "No, I was awarded QTS another way", doc.GetSummaryListValueForKey("Did a university, SCITT or school award your QTS?"));
        }
        else
        {
            Assert.Null(doc.GetSummaryListRowForKey("Did a university, SCITT or school award your QTS?"));
        }
    }

    public static TheoryData<bool, bool, bool> TrnIdentityData { get; } = new()
    {
        { true, true, true },
        { true, true, false },
        { true, false, true },
        { true, false, false },
        { false, true, true },
        { false, true, false },
        { false, false, true },
        { false, false, false },
    };
}
