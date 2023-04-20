using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.State;
using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests;

public delegate Func<AuthenticationState, Task> AuthenticationStateConfiguration(AuthenticationStateHelper.Configure config);

public sealed class AuthenticationStateHelper
{
    private readonly Guid _journeyId;
    private readonly TestAuthenticationStateProvider _authenticationStateProvider;
    private readonly IdentityLinkGenerator _linkGenerator;

    private AuthenticationStateHelper(
        Guid journeyId,
        TestAuthenticationStateProvider authenticationStateProvider,
        IdentityLinkGenerator linkGenerator)
    {
        _journeyId = journeyId;
        _authenticationStateProvider = authenticationStateProvider;
        _linkGenerator = linkGenerator;
    }

    public static async Task<AuthenticationStateHelper> Create(
        AuthenticationStateConfiguration configureAuthenticationState,
        HostFixture hostFixture,
        string? additionalScopes,
        TeacherIdentityApplicationDescriptor? client)
    {
        var authenticationStateProvider = (TestAuthenticationStateProvider)hostFixture.Services.GetRequiredService<IAuthenticationStateProvider>();

        client ??= TestClients.DefaultClient;

        var journeyId = Guid.NewGuid();
        var codeChallenge = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes("12345")));
        var fullScope = $"email profile {additionalScopes}".Trim();
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
            oAuthState: new OAuthAuthorizationState(client.ClientId!, fullScope, redirectUri)
            {
                TrnRequirementType = client.TrnRequirementType
            });

        var configure = new Configure(hostFixture);
        await configureAuthenticationState.Invoke(configure)(authenticationState);

        authenticationStateProvider.SetAuthenticationState(httpContext: null, authenticationState);

        var identityLinkGenerator = ActivatorUtilities.CreateInstance<TestIdentityLinkGenerator>(hostFixture.Services, authenticationState);

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
            Trn = new(this);
        }

        public ConfigureTrn Trn { get; }

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
                if (email is not null && user is not null && email != user.EmailAddress)
                {
                    throw new ArgumentException("Email does not match user's email.", nameof(email));
                }

                await EmailSet(email ?? user?.EmailAddress)(s);
                s.OnEmailVerified(user);
            };

        public Func<AuthenticationState, Task> MobileNumberSet(
            string? mobileNumber = null,
            string? email = null,
            User? user = null) =>
            async s =>
            {
                await EmailVerified(email, user)(s);
                s.OnMobileNumberSet(mobileNumber ?? TestData.GenerateUniqueMobileNumber());
            };

        public Func<AuthenticationState, Task> MobileVerified(
            string? mobileNumber = null,
            string? email = null,
            User? user = null) =>
            async s =>
            {
                if (mobileNumber is not null && user is not null && mobileNumber != user.MobileNumber)
                {
                    throw new ArgumentException("Mobile number does not match user's mobile number.", nameof(mobileNumber));
                }

                await MobileNumberSet(mobileNumber, email, user)(s);
                s.OnMobileNumberVerified(user);
            };

        public Func<AuthenticationState, Task> RegisterNameSet(
            string? firstName = null,
            string? lastName = null,
            string? mobileNumber = null,
            string? email = null,
            User? user = null) =>
            async s =>
            {
                await MobileVerified(mobileNumber, email, user)(s);
                s.OnNameSet(firstName ?? Faker.Name.First(), lastName ?? Faker.Name.Last());
            };

        public Func<AuthenticationState, Task> RegisterDateOfBirthSet(
            DateOnly? dateOfBirth = null,
            string? firstName = null,
            string? lastName = null,
            string? mobileNumber = null,
            string? email = null,
            User? user = null) =>
            async s =>
            {
                await RegisterNameSet(firstName, lastName, mobileNumber, email, user)(s);
                s.OnDateOfBirthSet(dateOfBirth ?? DateOnly.FromDateTime(Faker.Identification.DateOfBirth()));
            };

        public Func<AuthenticationState, Task> RegisterHasNiNumberSet(
            DateOnly? dateOfBirth = null,
            string? firstName = null,
            string? lastName = null,
            string? mobileNumber = null,
            string? email = null,
            User? user = null) =>
            async s =>
            {
                await RegisterDateOfBirthSet(dateOfBirth, firstName, lastName, mobileNumber, email, user)(s);
                s.OnHasNationalInsuranceNumberSet(true);
            };

        public Func<AuthenticationState, Task> RegisterNiNumberSet(
            DateOnly? dateOfBirth = null,
            string? firstName = null,
            string? lastName = null,
            string? mobileNumber = null,
            string? email = null,
            User? user = null) =>
            async s =>
            {
                await RegisterHasNiNumberSet(dateOfBirth, firstName, lastName, mobileNumber, email, user)(s);
                s.OnNationalInsuranceNumberSet(Faker.Identification.UkNationalInsuranceNumber());
            };

        public Func<AuthenticationState, Task> RegisterHasTrnSet(
            DateOnly? dateOfBirth = null,
            string? firstName = null,
            string? lastName = null,
            string? mobileNumber = null,
            string? email = null,
            User? user = null) =>
            async s =>
            {
                await RegisterNiNumberSet(dateOfBirth, firstName, lastName, mobileNumber, email, user)(s);
                s.OnHasTrnSet(true);
            };

        public Func<AuthenticationState, Task> RegisterTrnSet(
            DateOnly? dateOfBirth = null,
            string? firstName = null,
            string? lastName = null,
            string? mobileNumber = null,
            string? email = null,
            User? user = null) =>
            async s =>
            {
                await RegisterHasTrnSet(dateOfBirth, firstName, lastName, mobileNumber, email, user)(s);
                s.OnTrnSet(TestData.GenerateTrn());
            };

        public Func<AuthenticationState, Task> RegisterHasQtsSet(
            DateOnly? dateOfBirth = null,
            string? firstName = null,
            string? lastName = null,
            string? mobileNumber = null,
            string? email = null,
            User? user = null,
            bool? awardedQts = null) =>
            async s =>
            {
                await RegisterTrnSet(dateOfBirth, firstName, lastName, mobileNumber, email, user)(s);
                s.OnAwardedQtsSet(awardedQts == true);
            };

        public Func<AuthenticationState, Task> RegisterIttProviderSet(
            DateOnly? dateOfBirth = null,
            string? firstName = null,
            string? lastName = null,
            string? mobileNumber = null,
            string? email = null,
            User? user = null,
            bool? awardedQts = false,
            string? ittProviderName = "provider") =>
            async s =>
            {
                await RegisterHasQtsSet(dateOfBirth, firstName, lastName, mobileNumber, email, user, awardedQts)(s);
                s.OnHasIttProviderSet(hasIttProvider: ittProviderName is not null, ittProviderName);
            };

        public Func<AuthenticationState, Task> RegisterExistingUserAccountMatch(
            User? existingUserAccount = null,
            DateOnly? dateOfBirth = null,
            string? firstName = null,
            string? lastName = null,
            string? mobileNumber = null,
            string? email = null) =>
            async s =>
            {
                await RegisterDateOfBirthSet(dateOfBirth, firstName, lastName, mobileNumber, email)(s);
                s.OnExistingAccountFound(existingUserAccount ?? await TestData.CreateUser());
            };

        public Func<AuthenticationState, Task> RegisterExistingUserAccountChosen(
            User? existingUserAccount = null,
            DateOnly? dateOfBirth = null,
            string? firstName = null,
            string? lastName = null,
            string? mobileNumber = null,
            string? email = null) =>
            async s =>
            {
                await RegisterExistingUserAccountMatch(existingUserAccount, dateOfBirth, firstName, lastName, mobileNumber, email)(s);
                s.OnExistingAccountChosen(true);
            };

        public Func<AuthenticationState, Task> TrnLookupCompletedForNewTrn(User user, string? officialFirstName = null, string? officialLastName = null) =>
            async s =>
            {
                await Trn.IttProviderSet(
                    user.EmailAddress,
                    statedTrn: null,
                    officialFirstName,
                    officialLastName,
                    previousOfficialFirstName: null,
                    previousOfficialLastName: null,
                    preferredFirstName: null,
                    preferredLastName: null,
                    user.DateOfBirth,
                    nationalInsuranceNumber: null,
                    ittProviderName: null)(s);
                s.OnTrnLookupCompletedAndUserRegistered(user);
            };

        public Func<AuthenticationState, Task> TrnLookupCompletedForExistingTrn(string newEmail, User trnOwner, string? preferredFirstName = null, string? preferredLastName = null) =>
            async s =>
            {
                await EmailVerified(newEmail)(s);
                Debug.Assert(s.UserId is null);

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
            };
    }

    public class ConfigureTrn
    {
        private readonly Configure _configure;

        public ConfigureTrn(Configure configure)
        {
            _configure = configure;
        }

        public Func<AuthenticationState, Task> HasTrnSet(
            string? email = null,
            string? statedTrn = null) =>
            async s =>
            {
                await _configure.EmailVerified(email)(s);
                s.OnTrnSet(statedTrn);
            };

        public Func<AuthenticationState, Task> OfficialNameSet(
            string? email = null,
            string? statedTrn = null,
            string? officialFirstName = null,
            string? officialLastName = null,
            string? previousOfficialFirstName = null,
            string? previousOfficialLastName = null) =>
            async s =>
            {
                await HasTrnSet(email, statedTrn)(s);
                s.OnOfficialNameSet(
                    officialFirstName ?? Faker.Name.First(),
                    officialLastName ?? Faker.Name.Last(),
                    previousOfficialFirstName is not null ? AuthenticationState.HasPreviousNameOption.Yes : AuthenticationState.HasPreviousNameOption.No,
                    previousOfficialFirstName,
                    previousOfficialLastName);
            };

        public Func<AuthenticationState, Task> PreferredNameSet(
            string? email = null,
            string? statedTrn = null,
            string? officialFirstName = null,
            string? officialLastName = null,
            string? previousOfficialFirstName = null,
            string? previousOfficialLastName = null,
            string? preferredFirstName = null,
            string? preferredLastName = null) =>
            async s =>
            {
                await OfficialNameSet(
                    email,
                    statedTrn,
                    officialFirstName,
                    officialLastName,
                    previousOfficialFirstName,
                    previousOfficialLastName)(s);
                s.OnNameSet(preferredFirstName, preferredLastName);
            };

        public Func<AuthenticationState, Task> DateOfBirthSet(
            string? email = null,
            string? statedTrn = null,
            string? officialFirstName = null,
            string? officialLastName = null,
            string? previousOfficialFirstName = null,
            string? previousOfficialLastName = null,
            string? preferredFirstName = null,
            string? preferredLastName = null,
            DateOnly? dateOfBirth = null) =>
            async s =>
            {
                await PreferredNameSet(
                    email,
                    statedTrn,
                    officialFirstName,
                    officialLastName,
                    previousOfficialFirstName,
                    previousOfficialLastName,
                    preferredFirstName,
                    preferredLastName)(s);
                s.OnDateOfBirthSet(dateOfBirth ?? DateOnly.FromDateTime(Faker.Identification.DateOfBirth()));
            };

        public Func<AuthenticationState, Task> HasNationalInsuranceNumberSet(
            string? email = null,
            string? statedTrn = null,
            string? officialFirstName = null,
            string? officialLastName = null,
            string? previousOfficialFirstName = null,
            string? previousOfficialLastName = null,
            string? preferredFirstName = null,
            string? preferredLastName = null,
            DateOnly? dateOfBirth = null,
            bool hasNationalInsuranceNumber = true) =>
            async s =>
            {
                await DateOfBirthSet(
                    email,
                    statedTrn,
                    officialFirstName,
                    officialLastName,
                    previousOfficialFirstName,
                    previousOfficialLastName,
                    preferredFirstName,
                    preferredLastName,
                    dateOfBirth)(s);
                s.OnHasNationalInsuranceNumberSet(hasNationalInsuranceNumber);
            };

        public Func<AuthenticationState, Task> NationalInsuranceNumberSet(
            string? email = null,
            string? statedTrn = null,
            string? officialFirstName = null,
            string? officialLastName = null,
            string? previousOfficialFirstName = null,
            string? previousOfficialLastName = null,
            string? preferredFirstName = null,
            string? preferredLastName = null,
            DateOnly? dateOfBirth = null,
            string? nationalInsuranceNumber = null) =>
            async s =>
            {
                await HasNationalInsuranceNumberSet(
                    email,
                    statedTrn,
                    officialFirstName,
                    officialLastName,
                    previousOfficialFirstName,
                    previousOfficialLastName,
                    preferredFirstName,
                    preferredLastName,
                    dateOfBirth,
                    hasNationalInsuranceNumber: true)(s);
                s.OnNationalInsuranceNumberSet(nationalInsuranceNumber ?? Faker.Identification.UkNationalInsuranceNumber());
            };

        public Func<AuthenticationState, Task> AwardedQtsSet(
            string? email = null,
            string? statedTrn = null,
            string? officialFirstName = null,
            string? officialLastName = null,
            string? previousOfficialFirstName = null,
            string? previousOfficialLastName = null,
            string? preferredFirstName = null,
            string? preferredLastName = null,
            DateOnly? dateOfBirth = null,
            string? nationalInsuranceNumber = null,
            bool awardedQts = true) =>
            async s =>
            {
                var haveNationalInsuranceNumber = nationalInsuranceNumber is not null;

                await HasNationalInsuranceNumberSet(
                    email,
                    statedTrn,
                    officialFirstName,
                    officialLastName,
                    previousOfficialFirstName,
                    previousOfficialLastName,
                    preferredFirstName,
                    preferredLastName,
                    dateOfBirth,
                    hasNationalInsuranceNumber: haveNationalInsuranceNumber)(s);

                if (haveNationalInsuranceNumber)
                {
                    s.OnNationalInsuranceNumberSet(nationalInsuranceNumber!);
                }

                s.OnAwardedQtsSet(awardedQts);
            };

        public Func<AuthenticationState, Task> IttProviderSet(
            string? email = null,
            string? statedTrn = null,
            string? officialFirstName = null,
            string? officialLastName = null,
            string? previousOfficialFirstName = null,
            string? previousOfficialLastName = null,
            string? preferredFirstName = null,
            string? preferredLastName = null,
            DateOnly? dateOfBirth = null,
            string? nationalInsuranceNumber = null,
            string? ittProviderName = null) =>
            async s =>
            {
                await AwardedQtsSet(
                    email,
                    statedTrn,
                    officialFirstName,
                    officialLastName,
                    previousOfficialFirstName,
                    previousOfficialLastName,
                    preferredFirstName,
                    preferredLastName,
                    dateOfBirth,
                    nationalInsuranceNumber,
                    awardedQts: true)(s);

                s.OnHasIttProviderSet(hasIttProvider: ittProviderName is not null, ittProviderName);
            };
    }
}
