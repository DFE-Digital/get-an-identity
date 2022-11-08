using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.State;
using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests;

public sealed class AuthenticationStateHelper
{
    private readonly Guid _journeyId;
    private readonly TestAuthenticationStateProvider _authenticationStateProvider;
    private readonly IIdentityLinkGenerator _linkGenerator;

    private AuthenticationStateHelper(
        Guid journeyId,
        TestAuthenticationStateProvider authenticationStateProvider,
        IIdentityLinkGenerator linkGenerator)
    {
        _journeyId = journeyId;
        _authenticationStateProvider = authenticationStateProvider;
        _linkGenerator = linkGenerator;
    }

    public static async Task<AuthenticationStateHelper> Create(
        Func<Configure, Func<AuthenticationState, Task>> configureAuthenticationState,
        HostFixture hostFixture,
        string? additionalScopes)
    {
        var authenticationStateProvider = (TestAuthenticationStateProvider)hostFixture.Services.GetRequiredService<IAuthenticationStateProvider>();

        var journeyId = Guid.NewGuid();
        var codeChallenge = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes("12345")));
        var client = TestClients.Client1;
        var fullScope = $"email profile {additionalScopes}";
        var redirectUri = client.RedirectUris.First().ToString();

        var splitScopes = new HashSet<string>(fullScope.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries), StringComparer.OrdinalIgnoreCase);
        var userRequirements = UserRequirementsExtensions.GetUserRequirementsForScopes(
            hasScope: s => splitScopes.Contains(s));

        var authorizationUrl = $"/connect/authorize" +
            $"?client_id={client.ClientId}" +
            $"&response_type=code" +
            $"&scope=" + Uri.EscapeDataString(fullScope) +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
            $"&code_challenge_method=S256" +
            $"&response_mode=form_post";

        var authenticationState = new AuthenticationState(
            journeyId,
            userRequirements,
            authorizationUrl,
            startedAt: hostFixture.Services.GetRequiredService<IClock>().UtcNow,
            new OAuthAuthorizationState(client.ClientId!, fullScope, redirectUri));

        var configure = new Configure(hostFixture);
        await configureAuthenticationState.Invoke(configure)(authenticationState);

        authenticationStateProvider.SetAuthenticationState(httpContext: null, authenticationState);

        var linkGenerator = hostFixture.Services.GetRequiredService<LinkGenerator>();
        var identityLinkGenerator = new TestIdentityLinkGenerator(authenticationState, linkGenerator);

        return new AuthenticationStateHelper(journeyId, authenticationStateProvider, identityLinkGenerator);
    }

    public AuthenticationState AuthenticationState => _authenticationStateProvider.GetAuthenticationState(_journeyId)!;

    public string GetNextHopUrl() => AuthenticationState.GetNextHopUrl(_linkGenerator);

    public string ToQueryParam() => $"{AuthenticationStateMiddleware.IdQueryParameterName}={Uri.EscapeDataString(AuthenticationState.JourneyId.ToString())}";

    public class Configure
    {
        internal Configure(HostFixture hostFixture)
        {
            HostFixture = hostFixture;
            TestData = hostFixture.Services.GetRequiredService<TestData>();
            Clock = hostFixture.Services.GetRequiredService<IClock>();
        }

        public HostFixture HostFixture { get; }

        public TestData TestData { get; }

        public IClock Clock { get; }

        public Func<AuthenticationState, Task> Start() =>
            s => Task.CompletedTask;

        public Func<AuthenticationState, Task> EmailSet(string? email = null) =>
            s =>
            {
                s.OnEmailSet(email ?? Faker.Internet.Email());
                return Task.CompletedTask;
            };

        public Func<AuthenticationState, Task> EmailVerified(string? email = null, User? user = null) =>
            async s =>
            {
                if (email is not null && user is not null && email != user?.EmailAddress)
                {
                    throw new ArgumentException("Email does not match user's email.", nameof(email));
                }

                await EmailSet(email ?? user?.EmailAddress)(s);
                s.OnEmailVerified(user);
            };

        public Func<AuthenticationState, Task> TrnLookupCallbackCompleted(
            string email,
            string? trn,
            DateOnly dateOfBirth,
            string officialFirstName,
            string officialLastName,
            string? preferredFirstName = null,
            string? preferredLastName = null) => async s =>
            {
                await EmailVerified(email)(s);
                Debug.Assert(s.UserId is null);

                await TestData.WithDbContext(async dbContext =>
                {
                    dbContext.JourneyTrnLookupStates.Add(new JourneyTrnLookupState()
                    {
                        JourneyId = s.JourneyId,
                        Created = Clock.UtcNow,
                        DateOfBirth = dateOfBirth,
                        OfficialFirstName = officialFirstName,
                        OfficialLastName = officialLastName,
                        Trn = trn,
                        NationalInsuranceNumber = null,
                        PreferredFirstName = preferredFirstName,
                        PreferredLastName = preferredLastName
                    });

                    await dbContext.SaveChangesAsync();
                });
            };

        public Func<AuthenticationState, Task> TrnLookupCompletedForNewTrn(User user, string? officialFirstName = null, string? officialLastName = null) =>
            async s =>
            {
                await TrnLookupCallbackCompleted(
                    user.EmailAddress,
                    user.Trn,
                    user.DateOfBirth!.Value,
                    officialFirstName ?? user.FirstName,
                    officialLastName ?? user.LastName,
                    user.FirstName,
                    user.LastName)(s);

                s.OnTrnLookupCompletedAndUserRegistered(user);
            };

        public Func<AuthenticationState, Task> TrnLookupCompletedForExistingTrn(string newEmail, User trnOwner, string? preferredFirstName = null, string? preferredLastName = null) =>
            async s =>
            {
                await EmailVerified(newEmail)(s);
                Debug.Assert(s.UserId is null);

                await TestData.WithDbContext(async dbContext =>
                {
                    dbContext.JourneyTrnLookupStates.Add(new JourneyTrnLookupState()
                    {
                        JourneyId = s.JourneyId,
                        Created = Clock.UtcNow,
                        DateOfBirth = trnOwner.DateOfBirth!.Value,
                        OfficialFirstName = trnOwner.FirstName,
                        OfficialLastName = trnOwner.LastName,
                        Trn = trnOwner.Trn,
                        NationalInsuranceNumber = null,
                        PreferredFirstName = preferredFirstName,
                        PreferredLastName = preferredLastName
                    });

                    await dbContext.SaveChangesAsync();
                });

                s.OnTrnLookupCompletedForTrnAlreadyInUse(trnOwner.EmailAddress);
            };

        public Func<AuthenticationState, Task> TrnLookupCompletedForExistingTrnAndOwnerEmailVerified(string newEmail, User trnOwner, string? preferredFirstName = null, string? preferredLastName = null) =>
            async s =>
            {
                await TrnLookupCompletedForExistingTrn(newEmail, trnOwner, preferredFirstName, preferredLastName)(s);

                s.OnEmailVerifiedOfExistingAccountForTrn();
            };

        public Func<AuthenticationState, Task> TrnLookup(AuthenticationState.TrnLookupState state, User? user) =>
            async s =>
            {
                if (state == AuthenticationState.TrnLookupState.None)
                {
                    await EmailVerified()(s);
                    return;
                }

                if (user is null)
                {
                    throw new ArgumentNullException(nameof(user));
                }

                if (state == AuthenticationState.TrnLookupState.Complete)
                {
                    await TrnLookupCompletedForNewTrn(user)(s);
                }
                else if (state == AuthenticationState.TrnLookupState.ExistingTrnFound)
                {
                    await TrnLookupCompletedForExistingTrn(newEmail: Faker.Internet.Email(), user)(s);
                }
                else if (state == AuthenticationState.TrnLookupState.EmailOfExistingAccountForTrnVerified)
                {
                    await TrnLookupCompletedForExistingTrnAndOwnerEmailVerified(newEmail: Faker.Internet.Email(), user)(s);
                }
                else
                {
                    throw new NotImplementedException($"Unrecognised {nameof(AuthenticationState.TrnLookupState)}: '{state}'.");
                }
            };

        public Func<AuthenticationState, Task> Completed(User user, bool firstTimeSignIn = false, bool haveResumedCompletedJourney = false) =>
            async s =>
            {
                if (firstTimeSignIn && user.UserType == UserType.Staff)
                {
                    throw new NotSupportedException("Staff user registration is not implemented.");
                }

                if (firstTimeSignIn)
                {
                    Debug.Assert(user.UserType == UserType.Default);
                    await TrnLookupCompletedForNewTrn(user)(s);
                }
                else
                {
                    await EmailVerified(user: user)(s);
                }

                Debug.Assert(s.FirstTimeSignInForEmail == firstTimeSignIn);

                s.EnsureOAuthState();
                s.OAuthState.SetAuthorizationResponse(
                    new[]
                    {
                        new KeyValuePair<string, string>("code", "abc"),
                        new KeyValuePair<string, string>("state", "syz")
                    },
                    responseMode: "form_post");

                if (haveResumedCompletedJourney)
                {
                    s.OnHaveResumedCompletedJourney();
                }

                if (user.Trn is not null)
                {
                    HostFixture.DqtApiClient
                        .Setup(mock => mock.GetTeacherByTrn(user.Trn))
                        .ReturnsAsync(new AuthServer.Services.DqtApi.TeacherInfo()
                        {
                            Trn = user.Trn,
                            FirstName = user.FirstName,
                            LastName = user.LastName
                        });
                }
            };
    }
}
