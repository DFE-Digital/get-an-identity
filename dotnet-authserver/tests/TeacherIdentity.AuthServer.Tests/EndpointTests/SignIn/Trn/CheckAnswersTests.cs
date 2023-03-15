using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Oidc;
using static TeacherIdentity.AuthServer.Tests.AuthenticationStateHelper;

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
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(CustomScopes.DqtRead, HttpMethod.Get, "/sign-in/trn/check-answers");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(c => c.Trn.OfficialNameSet(), CustomScopes.DqtRead, HttpMethod.Get, "/sign-in/trn/check-answers");
    }

    [Fact]
    public async Task Get_UserRequirementsDoesNotContainTrnHolder_ReturnsForbidden()
    {
        await InvalidUserRequirements_ReturnsForbidden(c => c.Trn.IttProviderSet(), additionalScopes: "", HttpMethod.Get, "/sign-in/trn/check-answers");
    }

    [Theory]
    [IncompleteAuthenticationMilestonesData(AuthenticationState.AuthenticationMilestone.EmailVerified)]
    public async Task Get_JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(
        AuthenticationState.AuthenticationMilestone milestone)
    {
        await JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(milestone, HttpMethod.Get, "/sign-in/trn/check-answers");
    }

    [Theory]
    [MemberData(nameof(MissingAnswersData))]
    public async Task Get_MissingAnswersAndNotFoundTrn_RedirectsToPageOfFirstMissingAnswer(
        AuthenticationStateConfiguration configureAuthStateHelper,
        string expectedRedirect)
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(configureAuthStateHelper, CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/check-answers?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith(expectedRedirect, response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_ValidRequest_RendersExpectedContent()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Trn.IttProviderSet(), CustomScopes.DqtRead);
        var authState = authStateHelper.AuthenticationState;

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/check-answers?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Equal(authState.EmailAddress, doc.GetSummaryListValueForKey("Email address"));
        Assert.Equal($"{authState.OfficialFirstName} {authState.OfficialLastName}", doc.GetSummaryListValueForKey("Name"));
        Assert.Equal(authState.DateOfBirth?.ToString("dd MMMM yyyy"), doc.GetSummaryListValueForKey("Date of birth"));
    }

    [Fact]
    public async Task Get_ValidRequestWithMinimalAnswersButFoundTrn_ReturnsOk()
    {
        // Arrange
        var trn = TestData.GenerateTrn();

        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Trn.DateOfBirthSet(), CustomScopes.DqtRead);
        var authState = authStateHelper.AuthenticationState;

        authState.OnTrnLookupCompleted(trn, TrnLookupStatus.Found);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/check-answers?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidRequestWithPreviousOfficialName_ShowsPreviousNameRow()
    {
        // Arrange
        var previousFirstName = Faker.Name.First();
        var previousLastName = Faker.Name.Last();

        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.Trn.IttProviderSet(previousOfficialFirstName: previousFirstName, previousOfficialLastName: previousLastName),
            CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/check-answers?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Equal($"{previousFirstName} {previousLastName}", doc.GetSummaryListValueForKey("Previous name"));
    }

    [Fact]
    public async Task Get_ValidRequestWithoutPreviousOfficialName_DoesNotShowPreviousNameRow()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.Trn.IttProviderSet(previousOfficialFirstName: null, previousOfficialLastName: null),
            CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/check-answers?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Null(doc.GetSummaryListRowForKey("Previous name"));
    }

    [Fact]
    public async Task Get_ValidRequestWithPreferredName_ShowsPreferredNameRow()
    {
        // Arrange
        var preferredFirstName = Faker.Name.First();
        var preferredLastName = Faker.Name.Last();

        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.Trn.IttProviderSet(preferredFirstName: preferredFirstName, preferredLastName: preferredLastName),
            CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/check-answers?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Equal($"{preferredFirstName} {preferredLastName}", doc.GetSummaryListValueForKey("Preferred name"));
    }

    [Fact]
    public async Task Get_ValidRequestWithoutPreferredName_DoesNotShowPreferredNameRow()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.Trn.IttProviderSet(preferredFirstName: null, preferredLastName: null),
            CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/check-answers?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Null(doc.GetSummaryListRowForKey("Preferred name"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Get_ValidRequestWithHasNiNumberAnswered_ShowsNationalInsuranceNumberRow(bool hasNino)
    {
        // Arrange
        var nino = hasNino ? Faker.Identification.UkNationalInsuranceNumber() : null;
        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.Trn.IttProviderSet(nationalInsuranceNumber: nino),
            CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/check-answers?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Equal(hasNino ? nino : "Not given", doc.GetSummaryListValueForKey("National Insurance number"));
    }

    [Fact]
    public async Task Get_ValidRequestWithoutHasNinoAnswered_DoesNotShowNationalInsuranceNumberRow()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Trn.DateOfBirthSet(), CustomScopes.DqtRead);
        authStateHelper.AuthenticationState.OnTrnLookupCompleted(trn: TestData.GenerateTrn(), trnLookupStatus: TrnLookupStatus.Found);
        Assert.Null(authStateHelper.AuthenticationState.HasNationalInsuranceNumber);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/check-answers?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Null(doc.GetSummaryListRowForKey("National Insurance number"));
    }

    [Theory]
    [InlineData(true, "Yes")]
    [InlineData(false, "No")]
    public async Task Get_ValidRequestWithAwardedQtsAnswered_ShowsAwardedQtsRow(bool haveQts, string expectedValue)
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(
            c => haveQts ? c.Trn.IttProviderSet() : c.Trn.AwardedQtsSet(awardedQts: false),
            CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/check-answers?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Equal(expectedValue, doc.GetSummaryListValueForKey("Have you been awarded QTS?"));
    }

    [Fact]
    public async Task Get_ValidRequestWithAwardedQtsTrueAndIttProviderSpecified_ShowsIttProviderRowWithProviderName()
    {
        // Arrange
        var ittProviderName = "A Provider";

        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.Trn.IttProviderSet(ittProviderName: ittProviderName),
            CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/check-answers?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Equal(ittProviderName, doc.GetSummaryListValueForKey("Did a university, SCITT or school award your QTS?"));
    }

    [Fact]
    public async Task Get_ValidRequestWithAwardedQtsTrueAndIttProviderNotSpecified_ShowsAwardedQtsAnotherWay()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.Trn.IttProviderSet(ittProviderName: null),
            CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/check-answers?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Equal("No, I was awarded QTS another way", doc.GetSummaryListValueForKey("Did a university, SCITT or school award your QTS?"));
    }

    [Fact]
    public async Task Get_ValidRequestWithAwardedQtsFalse_DoesNotShowProviderRow()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.Trn.AwardedQtsSet(awardedQts: false),
            CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/check-answers?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Null(doc.GetSummaryListRowForKey("Did a university, SCITT or school award your QTS?"));
    }

    [Fact]
    public async Task Post_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/trn/check-answers");
    }

    [Fact]
    public async Task Post_MissingAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/trn/check-answers");
    }

    [Fact]
    public async Task Post_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(CustomScopes.DqtRead, HttpMethod.Post, "/sign-in/trn/check-answers");
    }

    [Fact]
    public async Task Post_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(c => c.Trn.OfficialNameSet(), CustomScopes.DqtRead, HttpMethod.Post, "/sign-in/trn/check-answers");
    }

    [Fact]
    public async Task Post_UserRequirementsDoesNotContainTrnHolder_ReturnsForbidden()
    {
        await InvalidUserRequirements_ReturnsForbidden(c => c.Trn.IttProviderSet(), additionalScopes: "", HttpMethod.Post, "/sign-in/trn/check-answers");
    }

    [Theory]
    [IncompleteAuthenticationMilestonesData(AuthenticationState.AuthenticationMilestone.EmailVerified)]
    public async Task Post_JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(
        AuthenticationState.AuthenticationMilestone milestone)
    {
        await JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(milestone, HttpMethod.Post, "/sign-in/trn/check-answers");
    }

    [Fact]
    public async Task Post_ValidRequestNullTrn_DoesNotCreateUserRedirectsToNoMatch()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Trn.AwardedQtsSet(awardedQts: false), CustomScopes.DqtRead);
        var authState = authStateHelper.AuthenticationState;

        authState.OnTrnLookupCompleted(trn: null, TrnLookupStatus.Pending);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/check-answers?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn/no-match", response.Headers.Location?.OriginalString);

        await TestData.WithDbContext(async dbContext =>
        {
            var user = await dbContext.Users.Where(u => u.EmailAddress == authStateHelper.AuthenticationState.EmailAddress).SingleOrDefaultAsync();
            Assert.Null(user);
        });
    }

    [Fact]
    public async Task Post_ValidRequestWithTrn_CreatesUserRedirectsToNextHop()
    {
        // Arrange
        var trn = TestData.GenerateTrn();

        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Trn.IttProviderSet(), CustomScopes.DqtRead);
        var authState = authStateHelper.AuthenticationState;

        authState.OnTrnLookupCompleted(trn, TrnLookupStatus.Found);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/check-answers?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(authStateHelper.GetNextHopUrl(), response.Headers.Location?.OriginalString);

        await TestData.WithDbContext(async dbContext =>
        {
            var user = await dbContext.Users.Where(u => u.Trn == trn).SingleOrDefaultAsync();
            Assert.NotNull(user);
        });
    }

    [Fact]
    public async Task Post_ValidRequestTrnIsAllocatedToAnExistingUser_DoesNotCreateUserRedirectsToTrnInUse()
    {
        // Arrange
        var existingUserWithTrn = await TestData.CreateUser(hasTrn: true);

        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Trn.IttProviderSet(), CustomScopes.DqtRead);
        var authState = authStateHelper.AuthenticationState;

        authState.OnTrnLookupCompleted(existingUserWithTrn.Trn, TrnLookupStatus.Found);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/check-answers?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn/different-email", response.Headers.Location?.OriginalString);

        await TestData.WithDbContext(async dbContext =>
        {
            var user = await dbContext.Users.Where(u => u.EmailAddress == authStateHelper.AuthenticationState.EmailAddress).SingleOrDefaultAsync();
            Assert.Null(user);
        });
    }

    public static TheoryData<AuthenticationStateConfiguration, string> MissingAnswersData
    {
        get
        {
            return new()
            {
                {
                    c => c.EmailVerified(),
                    "/sign-in/trn/has-trn"
                },
                {
                    c => c.Trn.HasTrnSet(),
                    "/sign-in/trn/official-name"
                },
                {
                    c => c.Trn.OfficialNameSet(),
                    "/sign-in/trn/preferred-name"
                },
                {
                    c => c.Trn.PreferredNameSet(),
                    "/sign-in/trn/date-of-birth"
                },
                {
                    c => c.Trn.DateOfBirthSet(),
                    "/sign-in/trn/has-nino"
                },
                {
                    c => c.Trn.HasNationalInsuranceNumberSet(hasNationalInsuranceNumber: true),
                    "/sign-in/trn/ni-number"
                },
                {
                    c => c.Trn.NationalInsuranceNumberSet(),
                    "/sign-in/trn/awarded-qts"
                },
                {
                    c => c.Trn.AwardedQtsSet(awardedQts: true),
                    "/sign-in/trn/itt-provider"
                }
            };
        }
    }
}
