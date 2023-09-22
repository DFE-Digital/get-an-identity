using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Elevate;

[Collection(nameof(DisableParallelization))]
public class CheckAnswersTests : TestBase
{
    public CheckAnswersTests(HostFixture hostFixture) : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/elevate/check-answers");
    }

    [Fact]
    public async Task Get_MissingAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/elevate/check-answers");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(CustomScopes.DqtRead, trnRequirementType: null, HttpMethod.Get, "/sign-in/elevate/check-answers");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        var user = await TestData.CreateUser(hasTrn: true, trnVerificationLevel: TrnVerificationLevel.Low);
        var nino = Faker.Identification.UkNationalInsuranceNumber();

        await JourneyHasExpired_RendersErrorPage(CreateConfigureAuthenticationState(user, nino, user.Trn!), CustomScopes.DqtRead, trnRequirementType: null, HttpMethod.Get, "/sign-in/elevate/check-answers", trnMatchPolicy: TrnMatchPolicy.Strict);
    }

    [Fact]
    public async Task Get_ValidRequest_ReturnsExpectedContent()
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: true, trnVerificationLevel: TrnVerificationLevel.Low);
        var nino = Faker.Identification.UkNationalInsuranceNumber();

        var authStateHelper = await CreateAuthenticationStateHelper(
            CreateConfigureAuthenticationState(user, nino, user.Trn!),
            additionalScopes: CustomScopes.DqtRead,
            trnMatchPolicy: TrnMatchPolicy.Strict,
            client: TestClients.DefaultClient);

        var authState = authStateHelper.AuthenticationState;

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/elevate/check-answers?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        var doc = await response.GetDocument();
        Assert.Equal(authState.Trn, doc.GetSummaryListValueForKey("Teacher reference number (TRN)"));
        Assert.Equal(authState.NationalInsuranceNumber, doc.GetSummaryListValueForKey("National Insurance number"));
    }

    [Fact]
    public async Task Post_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/elevate/check-answers");
    }

    [Fact]
    public async Task Post_MissingAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/elevate/check-answers");
    }

    [Fact]
    public async Task Post_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(CustomScopes.DqtRead, trnRequirementType: null, HttpMethod.Post, "/sign-in/elevate/check-answers");
    }

    [Fact]
    public async Task Post_JourneyHasExpired_RendersErrorPage()
    {
        var user = await TestData.CreateUser(hasTrn: true, trnVerificationLevel: TrnVerificationLevel.Low);
        var nino = Faker.Identification.UkNationalInsuranceNumber();

        await JourneyHasExpired_RendersErrorPage(CreateConfigureAuthenticationState(user, nino, user.Trn!), CustomScopes.DqtRead, trnRequirementType: null, HttpMethod.Post, "/sign-in/elevate/check-answers", trnMatchPolicy: TrnMatchPolicy.Strict);
    }

    [Fact]
    public async Task Post_TrnLookupFailed_UpdatesUserNinoAndRedirects()
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: true, trnVerificationLevel: TrnVerificationLevel.Low);
        var nino = Faker.Identification.UkNationalInsuranceNumber();

        var authStateHelper = await CreateAuthenticationStateHelper(
            CreateConfigureAuthenticationState(user, nino, user.Trn!),
            additionalScopes: CustomScopes.DqtRead,
            trnMatchPolicy: TrnMatchPolicy.Strict,
            client: TestClients.DefaultClient);

        var authState = authStateHelper.AuthenticationState;

        HostFixture.DqtApiClient
            .Setup(mock => mock.FindTeachers(It.Is<FindTeachersRequest>(req =>
                    req.DateOfBirth == authState.DateOfBirth &&
                    req.FirstName == authState.FirstName &&
                    req.LastName == authState.LastName &&
                    req.NationalInsuranceNumber == authState.NationalInsuranceNumber &&
                    req.Trn == authState.StatedTrn),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FindTeachersResponse()
            {
                Results = Array.Empty<FindTeachersResponseResult>()
            });

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/elevate/check-answers?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith(authState.PostSignInUrl, response.Headers.Location?.OriginalString);

        await TestData.WithDbContext(async dbContext =>
        {
            user = await dbContext.Users.SingleAsync(u => u.UserId == user.UserId);
            Assert.Equal(nino, user.NationalInsuranceNumber);
            Assert.Equal(TrnVerificationLevel.Low, user.TrnVerificationLevel);
        });
    }

    [Fact]
    public async Task Post_TrnLookupSuccessful_UpdatesUserTrnVerificationLevelAndRedirects()
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: true, trnVerificationLevel: TrnVerificationLevel.Low);
        var nino = Faker.Identification.UkNationalInsuranceNumber();

        var authStateHelper = await CreateAuthenticationStateHelper(
            CreateConfigureAuthenticationState(user, nino, user.Trn!),
            additionalScopes: CustomScopes.DqtRead,
            trnMatchPolicy: TrnMatchPolicy.Strict,
            client: TestClients.DefaultClient);

        var authState = authStateHelper.AuthenticationState;

        HostFixture.DqtApiClient
            .Setup(mock => mock.FindTeachers(It.Is<FindTeachersRequest>(req =>
                    req.DateOfBirth == authState.DateOfBirth &&
                    req.FirstName == authState.FirstName &&
                    req.LastName == authState.LastName &&
                    req.NationalInsuranceNumber == authState.NationalInsuranceNumber &&
                    req.Trn == authState.StatedTrn),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FindTeachersResponse()
            {
                Results = new[]
                {
                    new FindTeachersResponseResult()
                    {
                        DateOfBirth = authState.DateOfBirth,
                        EmailAddresses = new[] { authState.EmailAddress! },
                        FirstName = authState.FirstName!,
                        MiddleName = authState.MiddleName!,
                        LastName = authState.LastName!,
                        HasActiveSanctions = false,
                        NationalInsuranceNumber = authState.NationalInsuranceNumber,
                        Trn = user.Trn!,
                        Uid = Guid.NewGuid().ToString()
                    }
                }
            });

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/elevate/check-answers?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith(authState.PostSignInUrl, response.Headers.Location?.OriginalString);

        await TestData.WithDbContext(async dbContext =>
        {
            user = await dbContext.Users.SingleAsync(u => u.UserId == user.UserId);
            Assert.Equal(nino, user.NationalInsuranceNumber);
            Assert.Equal(TrnVerificationLevel.Medium, user.TrnVerificationLevel);
        });
    }

    private AuthenticationStateConfiguration CreateConfigureAuthenticationState(User user, string nino, string statedTrn) =>
        c => async s =>
        {
            await c.EmailVerified(user.EmailAddress, user: user)(s);
            s.OnNationalInsuranceNumberSet(nino);
            s.OnTrnSet(statedTrn);
        };
}
