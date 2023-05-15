using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services.DqtApi;
using TeacherIdentity.AuthServer.State;
using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Oidc;

[Collection(nameof(DisableParallelization))]  // We rely on TestAuthenticationStateProvider not changing underneath us
public class AuthorizeTests : TestBase
{
    private static readonly TeacherIdentityApplicationDescriptor _client = TestClients.DefaultClient;
    private static readonly string _redirectUri = _client.RedirectUris.First().ToString();

    public AuthorizeTests(HostFixture hostFixture)
        : base(hostFixture)
    {
        AuthenticationStateProvider.ClearAllAuthenticationState();
    }

    private TestAuthenticationStateProvider AuthenticationStateProvider =>
        (TestAuthenticationStateProvider)HostFixture.Services.GetRequiredService<IAuthenticationStateProvider>();

    [Fact]
    public async Task InvalidScope_ReturnsInvalidScopeError()
    {
        // Arrange
        // trn requires UserType.Default but user:write requires UserType.Staff
        var authorizeEndpoint = GetAuthorizeEndpoint(scope: "email profile openid trn user:write");

        // Act
        var response = await HttpClient.GetAsync(authorizeEndpoint);

        // Assert
        AssertResponseIsErrorCallback(response, "invalid_scope", "Scopes combination is not valid.");
    }

    [Fact]
    public async Task NoAuthenticationState_CreatesNewAuthenticationState()
    {
        // Arrange
        var authorizeEndpoint = GetAuthorizeEndpoint(scope: "trn");
        var initialStateCount = AuthenticationStateProvider.GetAllAuthenticationState().Count;

        // Act
        var response = await HttpClient.GetAsync(authorizeEndpoint);

        // Assert
        Assert.True((int)response.StatusCode < 400);
        var newStateCount = AuthenticationStateProvider.GetAllAuthenticationState().Count;
        Assert.True(newStateCount > initialStateCount);
    }

    [Fact]
    public async Task AuthenticationStateDoesNotHaveOAuthState_ReturnsBadRequest()
    {
        // Arrange
        var authenticationState = new AuthenticationState(
            journeyId: Guid.NewGuid(),
            UserRequirements.None,
            postSignInUrl: "/some/url",
            startedAt: Clock.UtcNow,
            oAuthState: null);

        AuthenticationStateProvider.SetAuthenticationState(authenticationState);

        var authorizeEndpoint = GetAuthorizeEndpoint(scope: "email profile openid", asid: authenticationState.JourneyId);

        // Act
        var response = await HttpClient.GetAsync(authorizeEndpoint);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task AuthenticationStateWithMismatchingClientId_ReturnsBadRequest()
    {
        // Arrange
        var scope = "email profile openid";
        var redirectUri = _redirectUri;

        var authenticationState = new AuthenticationState(
            journeyId: Guid.NewGuid(),
            UserRequirements.None,
            postSignInUrl: "/some/url",
            startedAt: Clock.UtcNow,
            oAuthState: new OAuthAuthorizationState(TestClients.RaiseTrnResolutionSupportTickets.ClientId!, scope, redirectUri));

        AuthenticationStateProvider.SetAuthenticationState(authenticationState);

        var authorizeEndpoint = GetAuthorizeEndpoint(scope, asid: authenticationState.JourneyId, redirectUri);

        // Act
        var response = await HttpClient.GetAsync(authorizeEndpoint);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task AuthenticationStateWithMismatchingScope_ReturnsBadRequest()
    {
        // Arrange
        var scope = "email profile openid";
        var redirectUri = _redirectUri;

        var authenticationState = new AuthenticationState(
            journeyId: Guid.NewGuid(),
            UserRequirements.None,
            postSignInUrl: "/some/url",
            startedAt: Clock.UtcNow,
            oAuthState: new OAuthAuthorizationState(_client.ClientId!, scope + " trn", redirectUri));

        AuthenticationStateProvider.SetAuthenticationState(authenticationState);

        var authorizeEndpoint = GetAuthorizeEndpoint(scope, asid: authenticationState.JourneyId, redirectUri);

        // Act
        var response = await HttpClient.GetAsync(authorizeEndpoint);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task AuthenticationStateWithMismatchingRedirectUri_ReturnsBadRequest()
    {
        // Arrange
        var scope = "email profile openid";
        var redirectUri = _redirectUri;

        var authenticationState = new AuthenticationState(
            journeyId: Guid.NewGuid(),
            UserRequirements.None,
            postSignInUrl: "/some/url",
            startedAt: Clock.UtcNow,
            oAuthState: new OAuthAuthorizationState(_client.ClientId!, scope, redirectUri + "more"));

        AuthenticationStateProvider.SetAuthenticationState(authenticationState);

        var authorizeEndpoint = GetAuthorizeEndpoint(scope, asid: authenticationState.JourneyId);

        // Act
        var response = await HttpClient.GetAsync(authorizeEndpoint);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Theory]
    [InlineData("email profile openid", UserRequirements.DefaultUserType)]
    [InlineData("email profile openid dqt:read", UserRequirements.DefaultUserType | UserRequirements.TrnHolder)]
    [InlineData("email profile openid user:read", UserRequirements.StaffUserType | UserRequirements.GetAnIdentitySupport)]
    public async Task ValidScope_AssignsUserRequirementsToAuthenticationState(string scope, UserRequirements expectedRequirements)
    {
        // Arrange
        var authorizeEndpoint = GetAuthorizeEndpoint(scope);

        // Act
        var response = await HttpClient.GetAsync(authorizeEndpoint);

        // Assert
        AssertResponseIsNotErrorCallback(response, out Guid journeyId);
        var authenticationState = AuthenticationStateProvider.GetAuthenticationState(journeyId);
        Assert.Equal(expectedRequirements, authenticationState?.UserRequirements);
    }

    [Fact]
    public async Task PromptNone_ReturnsForbidden()
    {
        // Arrange
        var authorizeEndpoint = GetAuthorizeEndpoint(scope: "email profile openid") + "&prompt=none";

        // Act
        var response = await HttpClient.GetAsync(authorizeEndpoint);

        // Assert
        AssertResponseIsErrorCallback(response, "invalid_request", "prompt=none is not currently supported.");
    }

    [Fact]
    public async Task TrnRequirementTypeNotSpecified_SetsClientsTrnRequirementTypeOnAuthenticationState()
    {
        // Arrange
        var authorizeEndpoint = GetAuthorizeEndpoint(scope: "email profile openid dqt:read");

        // Act
        var response = await HttpClient.GetAsync(authorizeEndpoint);

        // Assert
        AssertResponseIsNotErrorCallback(response, out Guid journeyId);
        var authenticationState = AuthenticationStateProvider.GetAuthenticationState(journeyId);
        Assert.Equal(_client.TrnRequirementType, authenticationState?.OAuthState?.TrnRequirementType);
    }

    [Fact]
    public async Task TrnRequirementTypeSpecified_OverridesClientsTrnRequirementTypeOnAuthenticationState()
    {
        // Arrange
        var trnRequirement = TrnRequirementType.Required;
        Debug.Assert(_client.TrnRequirementType != trnRequirement);
        var authorizeEndpoint = GetAuthorizeEndpoint(scope: "email profile openid dqt:read") +
            $"&trn_requirement={trnRequirement}";

        // Act
        var response = await HttpClient.GetAsync(authorizeEndpoint);

        // Assert
        AssertResponseIsNotErrorCallback(response, out Guid journeyId);
        var authenticationState = AuthenticationStateProvider.GetAuthenticationState(journeyId);
        Assert.Equal(trnRequirement, authenticationState?.OAuthState?.TrnRequirementType);
    }

    [Fact]
    public async Task TrnRequirementTypeSpecifiedButInvalid_ReturnsError()
    {
        // Arrange
        var authorizeEndpoint = GetAuthorizeEndpoint(scope: "email profile openid dqt:read") +
            $"&trn_requirement=invalid-requirement";

        // Act
        var response = await HttpClient.GetAsync(authorizeEndpoint);

        // Assert
        AssertResponseIsErrorCallback(response, "invalid_request", "Invalid trn_requirement specified.");
    }

    [Fact]
    public async Task JourneyDoesNotRequireTrnLookup_DoesNotSetTrnRequirementTypeOnAuthenticationState()
    {
        // Arrange
        var authorizeEndpoint = GetAuthorizeEndpoint(scope: "email profile openid");

        // Act
        var response = await HttpClient.GetAsync(authorizeEndpoint);

        // Assert
        AssertResponseIsNotErrorCallback(response, out Guid journeyId);
        var authenticationState = AuthenticationStateProvider.GetAuthenticationState(journeyId);
        Assert.NotNull(authenticationState?.OAuthState);
        Assert.Null(authenticationState.OAuthState.TrnRequirementType);
    }

    [Fact]
    public async Task TrnRequirementTypeSpecifiedForNonTrnLookupRequiringJourney_DoesNotSetTrnRequirementTypeOnAuthenticationState()
    {
        // Arrange
        var trnRequirement = TrnRequirementType.Required;
        Debug.Assert(_client.TrnRequirementType != trnRequirement);
        var authorizeEndpoint = GetAuthorizeEndpoint(scope: "email profile openid") +
            $"&trn_requirement={trnRequirement}";

        // Act
        var response = await HttpClient.GetAsync(authorizeEndpoint);

        // Assert
        AssertResponseIsNotErrorCallback(response, out Guid journeyId);
        var authenticationState = AuthenticationStateProvider.GetAuthenticationState(journeyId);
        Assert.NotNull(authenticationState?.OAuthState);
        Assert.Null(authenticationState.OAuthState.TrnRequirementType);
    }

    [Fact]
    public async Task ValidTrnToken_SetsDataOnAuthenticationState()
    {
        // Arrange
        var trn = TestData.GenerateTrn();
        var trnToken = await GenerateTrnToken(trn, expires: Clock.UtcNow.AddDays(1));

        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());

        MockDqtApiResponse(trn, firstName, lastName, dateOfBirth);

        var authorizeEndpoint = GetAuthorizeEndpoint(scope: "email profile openid dqt:read") +
                                $"&trn_token={trnToken.TrnToken}";
        // Act
        var response = await HttpClient.GetAsync(authorizeEndpoint);

        // Assert
        AssertResponseIsNotErrorCallback(response, out Guid journeyId);
        var authenticationState = AuthenticationStateProvider.GetAuthenticationState(journeyId);

        Assert.Equal(trn, authenticationState?.Trn);
        Assert.Equal(firstName, authenticationState?.FirstName);
        Assert.Equal(lastName, authenticationState?.LastName);
        Assert.Equal(dateOfBirth, authenticationState?.DateOfBirth);
        Assert.Equal(trnToken.Email, authenticationState?.EmailAddress);
    }

    [Fact]
    public async Task TrnTokenSpecifiedButNotFound_DoesNotSetTrnOnAuthenticationState()
    {
        // Arrange
        MockDqtApiResponse();

        var authorizeEndpoint = GetAuthorizeEndpoint(scope: "email profile openid dqt:read") +
                                $"&trn_token={TestData.GenerateUniqueTrnTokenValue()}";
        // Act
        var response = await HttpClient.GetAsync(authorizeEndpoint);

        // Assert
        AssertResponseIsNotErrorCallback(response, out Guid journeyId);
        var authenticationState = AuthenticationStateProvider.GetAuthenticationState(journeyId);
        Assert.Null(authenticationState?.Trn);
    }

    [Fact]
    public async Task TrnTokenSpecifiedButExpired_DoesNotSetTrnOnAuthenticationState()
    {
        // Arrange
        var trn = TestData.GenerateTrn();
        var trnToken = await GenerateTrnToken(trn, expires: Clock.UtcNow.AddDays(-1));

        MockDqtApiResponse(trn);

        var authorizeEndpoint = GetAuthorizeEndpoint(scope: "email profile openid dqt:read") +
                                $"&trn_token={trnToken.TrnToken}";
        // Act
        var response = await HttpClient.GetAsync(authorizeEndpoint);

        // Assert
        AssertResponseIsNotErrorCallback(response, out Guid journeyId);
        var authenticationState = AuthenticationStateProvider.GetAuthenticationState(journeyId);
        Assert.Null(authenticationState?.Trn);
    }

    [Fact]
    public async Task TrnTokenSpecifiedButTrnExists_DoesNotSetTrnOnAuthenticationState()
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: true);
        var trnToken = await GenerateTrnToken(user.Trn!, expires: Clock.UtcNow.AddDays(1));

        MockDqtApiResponse(user.Trn);

        var authorizeEndpoint = GetAuthorizeEndpoint(scope: "email profile openid dqt:read") +
                                $"&trn_token={trnToken.TrnToken}";
        // Act
        var response = await HttpClient.GetAsync(authorizeEndpoint);

        // Assert
        AssertResponseIsNotErrorCallback(response, out Guid journeyId);
        var authenticationState = AuthenticationStateProvider.GetAuthenticationState(journeyId);
        Assert.Null(authenticationState?.Trn);
    }

    [Fact]
    public async Task TrnTokenSpecifiedForNonTrnLookupRequiringJourney_DoesNotSetTrnOnAuthenticationState()
    {
        // Arrange
        var trn = TestData.GenerateTrn();
        var trnToken = await GenerateTrnToken(trn, expires: Clock.UtcNow.AddDays(1));

        MockDqtApiResponse(trn);

        var authorizeEndpoint = GetAuthorizeEndpoint(scope: "email profile openid") +
                                $"&trn_token={trnToken.TrnToken}";

        // Act
        var response = await HttpClient.GetAsync(authorizeEndpoint);

        // Assert
        AssertResponseIsNotErrorCallback(response, out Guid journeyId);
        var authenticationState = AuthenticationStateProvider.GetAuthenticationState(journeyId);
        Assert.Null(authenticationState?.Trn);
    }

    [Fact]
    public async Task TrnTokenSpecified_TeacherNotFoundInDqt_DoesNotSetTrnOnAuthenticationState()
    {
        // Arrange
        var trn = TestData.GenerateTrn();
        var trnToken = await GenerateTrnToken(trn, expires: Clock.UtcNow.AddDays(1));

        HostFixture.DqtApiClient
            .Setup(mock => mock.GetTeacherByTrn(trn, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeacherInfo?)null);

        var authorizeEndpoint = GetAuthorizeEndpoint(scope: "email profile openid dqt:read") +
                                $"&trn_token={trnToken.TrnToken}";

        // Act
        var response = await HttpClient.GetAsync(authorizeEndpoint);

        // Assert
        AssertResponseIsNotErrorCallback(response, out Guid journeyId);
        var authenticationState = AuthenticationStateProvider.GetAuthenticationState(journeyId);
        Assert.Null(authenticationState?.Trn);
    }

    private static void AssertResponseIsErrorCallback(
        HttpResponseMessage response,
        string expectedError,
        string expectedErrorDescription)
    {
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);

        var callbackUrl = response.Headers.Location?.ToString();
        Assert.NotNull(callbackUrl);
        Assert.StartsWith(_redirectUri, callbackUrl);

        var qs = QueryHelpers.ParseQuery(new Uri(callbackUrl).Query);
        Assert.Equal(expectedError, qs["error"]);
        Assert.Equal(expectedErrorDescription, qs["error_description"]);
    }

    private static void AssertResponseIsNotErrorCallback(HttpResponseMessage response, out Guid journeyId)
    {
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);

        var callbackUrl = response.Headers.Location?.ToString();
        Assert.NotNull(callbackUrl);
        Assert.True(!callbackUrl.StartsWith(_redirectUri));

        journeyId = GetJourneyIdFromUrl(response.Headers.Location!.OriginalString);
    }

    private static string GetAuthorizeEndpoint(string scope, Guid? asid = null, string? redirectUri = null)
    {
        var codeChallenge = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes("12345")));

        return $"/connect/authorize" +
            $"?client_id={_client.ClientId}" +
            $"&response_type=code" +
            $"&scope=" + Uri.EscapeDataString(scope) +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri ?? _redirectUri)}" +
            $"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
            $"&code_challenge_method=S256" +
            $"&response_mode=query" +
            (asid is not null ? $"&{AuthenticationStateMiddleware.IdQueryParameterName}={asid}" : string.Empty);
    }

    private static Guid GetJourneyIdFromUrl(string url)
    {
        var query = url[url.IndexOf("?")..];

        var asid = QueryHelpers.ParseQuery(query)[AuthenticationStateMiddleware.IdQueryParameterName].ToString() ??
            throw new Exception("URL is missing journey ID parameter.");

        return Guid.Parse(asid);
    }

    private async Task<TrnTokenModel> GenerateTrnToken(string trn, DateTime expires)
    {
        var trnToken = new TrnTokenModel()
        {
            TrnToken = TestData.GenerateUniqueTrnTokenValue(),
            Trn = trn,
            Email = TestData.GenerateUniqueEmail(),
            CreatedUtc = Clock.UtcNow,
            ExpiresUtc = expires
        };

        await TestData.WithDbContext(async dbContext =>
        {
            dbContext.TrnTokens.Add(trnToken);
            await dbContext.SaveChangesAsync();
        });

        return trnToken;
    }

    private void MockDqtApiResponse(string? trn = null, string? firstName = null, string? lastName = null, DateOnly? dateOfBirth = null)
    {
        trn ??= TestData.GenerateTrn();

        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(trn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthServer.Services.DqtApi.TeacherInfo()
            {
                DateOfBirth = dateOfBirth ?? DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
                FirstName = firstName ?? Faker.Name.First(),
                MiddleName = "",
                LastName = lastName ?? Faker.Name.Last(),
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                Trn = trn,
                PendingDateOfBirthChange = false,
                PendingNameChange = false
            });
    }
}
