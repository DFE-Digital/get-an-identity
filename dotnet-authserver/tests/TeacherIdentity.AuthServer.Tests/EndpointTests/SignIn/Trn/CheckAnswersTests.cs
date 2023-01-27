using Microsoft.EntityFrameworkCore;

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

    [Fact]
    public async Task Get_ValidRequest_RendersExpectedContent()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.DateOfBirthSet());
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
    public async Task Get_ValidRequestWithPreviousOfficialName_ShowsPreviousNameRow()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.DateOfBirthSet());
        var authState = authStateHelper.AuthenticationState;

        var previousFirstName = Faker.Name.First();
        var previousLastName = Faker.Name.Last();

        authState.OnHasPreviousNameSet(Pages.SignIn.Trn.OfficialName.HasPreviousNameOption.Yes);
        authState.OnOfficialNameSet(
            authState.OfficialFirstName!,
            authState.OfficialLastName!,
            previousFirstName,
            previousLastName);

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
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.DateOfBirthSet());
        var authState = authStateHelper.AuthenticationState;

        var previousFirstName = Faker.Name.First();
        var previousLastName = Faker.Name.Last();

        authState.OnHasPreviousNameSet(Pages.SignIn.Trn.OfficialName.HasPreviousNameOption.Yes);
        authState.OnOfficialNameSet(
            authState.OfficialFirstName!,
            authState.OfficialLastName!,
            previousOfficialFirstName: null,
            previousOfficialLastName: null);

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
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.DateOfBirthSet());
        var authState = authStateHelper.AuthenticationState;

        var preferredFirstName = Faker.Name.First();
        var preferredLastName = Faker.Name.Last();

        authState.OnNameSet(preferredFirstName, preferredLastName);

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
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.DateOfBirthSet());
        var authState = authStateHelper.AuthenticationState;
        Assert.Null(authState.FirstName);
        Assert.Null(authState.LastName);

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
    public async Task Get_ValidRequestWithHaveNinoAnswered_ShowsNationalInsuranceNumberRow(bool haveNino)
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.DateOfBirthSet());
        var authState = authStateHelper.AuthenticationState;

        authState.OnHaveNationalInsuranceNumberSet(haveNino);
        var nino = haveNino ? Faker.Identification.UkNationalInsuranceNumber() : null;
        authState.NationalInsuranceNumber = nino;

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/check-answers?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Equal(haveNino ? nino : "Not given", doc.GetSummaryListValueForKey("National Insurance number"));
    }

    [Fact]
    public async Task Get_ValidRequestWithoutHaveNinoAnswered_DoesNotShowNationalInsuranceNumberRow()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.DateOfBirthSet());
        var authState = authStateHelper.AuthenticationState;
        Assert.Null(authState.HaveNationalInsuranceNumber);

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
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.DateOfBirthSet());
        var authState = authStateHelper.AuthenticationState;

        authState.OnAwardedQtsSet(haveQts);
        authState.OnHaveIttProviderSet(false);

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
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.DateOfBirthSet());
        var authState = authStateHelper.AuthenticationState;

        var ittProviderName = "A Provider";

        authState.OnAwardedQtsSet(true);
        authState.OnHaveIttProviderSet(true);
        authState.IttProviderName = ittProviderName;

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
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.DateOfBirthSet());
        var authState = authStateHelper.AuthenticationState;

        authState.OnAwardedQtsSet(true);
        authState.OnHaveIttProviderSet(false);

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
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.DateOfBirthSet());
        var authState = authStateHelper.AuthenticationState;

        authState.OnAwardedQtsSet(false);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/check-answers?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Null(doc.GetSummaryListRowForKey("Did a university, SCITT or school award your QTS?"));
    }

    [Fact]
    public async Task Post_ValidRequestNullTrn_DoesNotCreateUserRedirectsToNoMatch()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.DateOfBirthSet());
        var authState = authStateHelper.AuthenticationState;

        authState.OnAwardedQtsSet(false);
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

        var authStateHelper = await CreateAuthenticationStateHelper(c => c.DateOfBirthSet());
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

        var authStateHelper = await CreateAuthenticationStateHelper(c => c.DateOfBirthSet());
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
}
