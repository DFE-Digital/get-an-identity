using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.State;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer.Controllers;

public class AuthorizationController : Controller
{
    private readonly TeacherIdentityApplicationManager _applicationManager;
    private readonly IOpenIddictAuthorizationManager _authorizationManager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly SignInJourneyProvider _signInJourneyProvider;
    private readonly UserClaimHelper _userClaimHelper;
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;
    private readonly TrnTokenHelper _trnTokenHelper;
    private readonly IConfiguration _configuration;

    public AuthorizationController(
        TeacherIdentityApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager,
        IOpenIddictScopeManager scopeManager,
        SignInJourneyProvider signInJourneyProvider,
        UserClaimHelper userClaimHelper,
        TeacherIdentityServerDbContext dbContext,
        IClock clock,
        TrnTokenHelper trnTokenHelper,
        IConfiguration configuration)
    {
        _applicationManager = applicationManager;
        _authorizationManager = authorizationManager;
        _scopeManager = scopeManager;
        _signInJourneyProvider = signInJourneyProvider;
        _userClaimHelper = userClaimHelper;
        _dbContext = dbContext;
        _clock = clock;
        _trnTokenHelper = trnTokenHelper;
        _configuration = configuration;
    }

    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        // Try to retrieve the user principal stored in the authentication cookie and redirect
        // the user agent to the login page (or to an external provider) in the following cases:
        //
        //  - If the user principal can't be extracted or the cookie is too old.
        //  - If prompt=login was specified by the client application.
        //  - If a max_age parameter was provided and the authentication cookie is not considered "fresh" enough.
        //  - If the user is signed in but we're missing some information (e.g more claims have been specified).

        var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!UserRequirementsExtensions.TryGetUserRequirementsForScopes(
                request.HasScope,
                out var userRequirements,
                out _))
        {
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>()
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidScope,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Scopes combination is not valid."
                }));
        }

        var trnToken = userRequirements.HasFlag(UserRequirements.TrnHolder) ? await _trnTokenHelper.GetValidTrnToken(request) : null;
        var cookiesPrincipal = authenticateResult.Principal;

        // Ensure we have a journey to store state for this request.
        // This also tracks when we have enough information about the user to satisfy the request.
        if (!HttpContext.TryGetAuthenticationState(out var authenticationState))
        {
            var journeyId = Guid.NewGuid();

            TrnRequirementType? trnRequirementType = null;
            TrnMatchPolicy? trnMatchPolicy = null;

            if (userRequirements.HasFlag(UserRequirements.TrnHolder))
            {
                var client = (await _applicationManager.FindByClientIdAsync(request.ClientId!))!;
                var allowTrnConfigurationOverrides = client.ClientId == "testclient" || _configuration.GetValue<bool>("AllowTrnConfigurationOverrides", false);

                if (allowTrnConfigurationOverrides)
                {
                    var requestedTrnRequirement = request["trn_requirement"];
                    if (requestedTrnRequirement.HasValue)
                    {
                        if (Enum.TryParse<TrnRequirementType>(requestedTrnRequirement?.Value as string, out var parsedTrnRequirementType))
                        {
                            trnRequirementType = parsedTrnRequirementType;
                        }
                        else
                        {
                            return Forbid(
                                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                                properties: new AuthenticationProperties(new Dictionary<string, string?>()
                                {
                                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidRequest,
                                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                                        "Invalid trn_requirement specified."
                                }));
                        }
                    }

                    var requestedTrnMatchPolicy = request["trn_match_policy"];
                    if (requestedTrnMatchPolicy.HasValue)
                    {
                        if (Enum.TryParse<TrnMatchPolicy>(requestedTrnMatchPolicy?.Value as string, out var parsedTrnMatchPolicy))
                        {
                            trnMatchPolicy = parsedTrnMatchPolicy;
                        }
                        else
                        {
                            return Forbid(
                                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                                properties: new AuthenticationProperties(new Dictionary<string, string?>()
                                {
                                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidRequest,
                                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                                        "Invalid trn_match_policy specified."
                                }));
                        }
                    }
                }

                trnRequirementType ??= client.TrnRequirementType;
                trnMatchPolicy ??= client.TrnMatchPolicy;
            }

            var sessionId = request["session_id"]?.Value as string;

            authenticationState = new AuthenticationState(
                journeyId,
                userRequirements,
                GetCallbackUrl(journeyId),
                startedAt: _clock.UtcNow,
                sessionId,
                oAuthState: new OAuthAuthorizationState(request.ClientId!, request.Scope!, request.RedirectUri)
                {
                    TrnRequirementType = trnRequirementType,
                    TrnMatchPolicy = trnMatchPolicy
                },
                authenticateResult.Succeeded != true);

            var signedInUser = await GetSignedInUser(authenticateResult, userRequirements);
            var existingUserClaimsPrincipal = await InitializeAuthenticationState(signedInUser, trnToken, authenticationState);
            cookiesPrincipal ??= existingUserClaimsPrincipal;

            HttpContext.Features.Set(new AuthenticationStateFeature(authenticationState));
        }
        else
        {
            // Got an existing journey, check its OAuthState matches what's in this request
            if (authenticationState.OAuthState is null ||
                authenticationState.OAuthState.ClientId != request.ClientId ||
                authenticationState.OAuthState.Scope != request.Scope ||
                authenticationState.OAuthState.RedirectUri != request.RedirectUri)
            {
                return BadRequest();
            }
        }

        if (request.HasPrompt(Prompts.None))
        {
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>()
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidRequest,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "prompt=none is not currently supported."
                }));
        }

        var maxAge = AuthenticationState.AuthCookieLifetime;
        if (request.MaxAge is long maxAgeSeconds && maxAgeSeconds < maxAge.TotalSeconds)
        {
            maxAge = TimeSpan.FromSeconds(maxAgeSeconds);
        }

        var authTicketIsTooOld = authenticateResult.Properties?.IssuedUtc != null &&
            DateTimeOffset.UtcNow - authenticateResult.Properties.IssuedUtc > maxAge;

        if (authTicketIsTooOld || request.HasPrompt(Prompts.Login))
        {
            authenticationState.Reset(_clock.UtcNow);
        }

        var signInJourney = _signInJourneyProvider.GetSignInJourney(authenticationState, HttpContext);

        if (!signInJourney.IsCompleted())
        {
            // If the client application requested promptless authentication,
            // return an error indicating that the user is not logged in.
            if (request.HasPrompt(Prompts.None))
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>()
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.LoginRequired,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is not logged in."
                    }));
            }

            return Redirect(signInJourney.GetStartStepUrl());
        }

        if (trnToken is not null)
        {
            // If we have a signed in user we may never enter the TRN token sign in journey, so we have to apply
            // the token here
            await _trnTokenHelper.ApplyTrnTokenToUser(authenticationState.UserId, trnToken.TrnToken);
        }

        Debug.Assert(signInJourney.IsCompleted());

        // It's possible that the user doesn't have a 'signed in' cookie but the sign in journey is completed
        // (if, say, the user retried the page that sent the response cookie and they never got it).
        // Make sure the user is signed in if the journey is done.
        cookiesPrincipal ??= await authenticationState.SignIn(HttpContext);

        // If it's a Staff user verify their permissions
        if (cookiesPrincipal.GetUserType() == UserType.Staff &&
            !authenticationState.UserRequirements.VerifyStaffUserRequirements(cookiesPrincipal))
        {
            return Forbid(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        var subject = cookiesPrincipal.FindFirstValue(Claims.Subject)!;
        var userId = cookiesPrincipal.GetUserId();

        // Retrieve the application details from the database.
        var application = await _applicationManager.FindByClientIdAsync(request.ClientId!) ??
            throw new InvalidOperationException("The client application cannot be found.");

        // Retrieve the permanent authorizations associated with the user and the calling client application.
        var authorizations = await _authorizationManager.FindAsync(
            subject: subject,
            client: (await _applicationManager.GetIdAsync(application))!,
            status: Statuses.Valid,
            type: AuthorizationTypes.Permanent,
            scopes: request.GetScopes()).ToListAsync();

        switch (await _applicationManager.GetConsentTypeAsync(application))
        {
            // If the consent is external (e.g when authorizations are granted by a sysadmin),
            // immediately return an error if no authorization can be found in the database.
            case ConsentTypes.External when !authorizations.Any():
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>()
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.ConsentRequired,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The logged in user is not allowed to access this client application."
                    }));

            // If the consent is implicit or if an authorization was found,
            // return an authorization response without displaying the consent form.
            case ConsentTypes.Implicit:
            case ConsentTypes.External when authorizations.Any():
            case ConsentTypes.Explicit when authorizations.Any() && !request.HasPrompt(Prompts.Consent):
                var claims = await _userClaimHelper.GetPublicClaims(userId, authenticationState.OAuthState!.TrnMatchPolicy);

                // Create the claims-based identity that will be used by OpenIddict to generate tokens.
                var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                identity.AddClaims(claims);

                var principal = new ClaimsPrincipal(identity);

                principal.SetScopes(request.GetScopes());
                principal.SetResources(await _scopeManager.ListResourcesAsync(principal.GetScopes()).ToListAsync());

                // Automatically create a permanent authorization to avoid requiring explicit consent
                // for future authorization or token requests containing the same scopes.
                var authorization = authorizations.LastOrDefault();
                if (authorization is null)
                {
                    authorization = await _authorizationManager.CreateAsync(
                        principal: principal,
                        subject: subject,
                        client: (await _applicationManager.GetIdAsync(application))!,
                        type: AuthorizationTypes.Permanent,
                        scopes: principal.GetScopes());
                }

                principal.SetAuthorizationId(await _authorizationManager.GetIdAsync(authorization));

                foreach (var claim in principal.Claims)
                {
                    claim.SetDestinations(GetDestinations(claim, principal));
                }

                if (authenticationState.OAuthState!.TrnMatchPolicy is TrnMatchPolicy trnMatchPolicy)
                {
                    principal.SetClaim(CustomClaims.Private.TrnMatchPolicy, trnMatchPolicy.ToString());
                }

                await HttpContext.SaveUserSignedInEvent(principal);

                return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            // At this point, no authorization was found in the database and an error must be returned
            // if the client application specified prompt=none in the authorization request.
            case ConsentTypes.Explicit when request.HasPrompt(Prompts.None):
            case ConsentTypes.Systematic when request.HasPrompt(Prompts.None):
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>()
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.ConsentRequired,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "Interactive user consent is required."
                    }));

            // In every other case, render the consent form.
            default:
                throw new NotImplementedException();
        }

        string GetCallbackUrl(Guid journeyId)
        {
            // Creates a URL back to this action
            // without the 'login' prompt to avoid going in circles continually prompting a user to sign in

            var prompt = string.Join(" ", request.GetPrompts().Remove(Prompts.Login));

            var ignoredParameters = new List<string> { "trn_token", Parameters.Prompt };

            var parameters = Request.HasFormContentType ?
                Request.Form.Where(parameter => !ignoredParameters.Contains(parameter.Key)).ToList() :
                Request.Query.Where(parameter => !ignoredParameters.Contains(parameter.Key)).ToList();

            parameters.Add(KeyValuePair.Create(Parameters.Prompt, new StringValues(prompt)));

            parameters.Add(KeyValuePair.Create(AuthenticationStateMiddleware.IdQueryParameterName, new StringValues(journeyId.ToString())));

            return Request.PathBase + Request.Path + QueryString.Create(parameters);
        }
    }

    [HttpPost("~/connect/token"), Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
        {
            // Retrieve the claims principal stored in the authorization code/refresh token.
            var principal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal!;

            foreach (var claim in principal.Claims)
            {
                claim.SetDestinations(GetDestinations(claim, principal));
            }

            // Returning a SignInResult will ask OpenIddict to issue the appropriate access/identity tokens.
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
        else if (request.IsClientCredentialsGrantType())
        {
            var application = await _applicationManager.FindByClientIdAsync(request.ClientId!);
            if (application == null)
            {
                throw new InvalidOperationException("The application details cannot be found in the database.");
            }

            // Create a new ClaimsIdentity containing the claims that
            // will be used to create an id_token, a token or a code.
            var identity = new ClaimsIdentity(
                TokenValidationParameters.DefaultAuthenticationType,
                Claims.Name, Claims.Role);

            // Use the client_id as the subject identifier.
            identity.AddClaim(Claims.Subject, (await _applicationManager.GetClientIdAsync(application))!,
                Destinations.AccessToken, Destinations.IdentityToken);

            identity.AddClaim(Claims.Name, (await _applicationManager.GetDisplayNameAsync(application))!,
                Destinations.AccessToken, Destinations.IdentityToken);

            // Note: In the original OAuth 2.0 specification, the client credentials grant
            // doesn't return an identity token, which is an OpenID Connect concept.
            //
            // As a non-standardized extension, OpenIddict allows returning an id_token
            // to convey information about the client application when the "openid" scope
            // is granted (i.e specified when calling principal.SetScopes()). When the "openid"
            // scope is not explicitly set, no identity token is returned to the client application.

            // Set the list of scopes granted to the client application in access_token.
            var principal = new ClaimsPrincipal(identity);
            principal.SetScopes(request.GetScopes());
            principal.SetResources(await _scopeManager.ListResourcesAsync(principal.GetScopes()).ToListAsync());

            foreach (var claim in principal.Claims)
            {
                claim.SetDestinations(GetDestinations(claim, principal));
            }

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        throw new InvalidOperationException("The specified grant type is not supported.");
    }

    [HttpGet("~/connect/signout")]
    public new IActionResult SignOut() => View();

    [ActionName(nameof(SignOut)), HttpPost("~/connect/signout")]
    public async Task<IActionResult> SignOutPost()
    {
        var redirectUri = HttpContext.TryGetAuthenticationState(out var authenticationState) ?
            authenticationState.PostSignInUrl :
            "/";

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return SignOut(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties
            {
                RedirectUri = redirectUri
            });
    }

    private static IEnumerable<string> GetDestinations(Claim claim, ClaimsPrincipal principal)
    {
        switch (claim.Type)
        {
            case Claims.Subject:
                yield return Destinations.AccessToken;
                yield return Destinations.IdentityToken;
                yield break;

            case Claims.Name:
            case Claims.GivenName:
            case Claims.MiddleName:
            case Claims.FamilyName:
            case Claims.Birthdate:
            case CustomClaims.PreferredName:
            case CustomClaims.PreviousUserId:
                if (principal.HasScope(Scopes.Profile))
                {
                    yield return Destinations.IdentityToken;
                }

                yield break;

            case Claims.Email:
            case Claims.EmailVerified:
                if (principal.HasScope(Scopes.Email))
                {
                    yield return Destinations.IdentityToken;
                }

                yield break;

            case Claims.PhoneNumber:
            case Claims.PhoneNumberVerified:
                if (principal.HasScope(Scopes.Phone))
                {
                    yield return Destinations.IdentityToken;
                }

                yield break;

            case CustomClaims.Trn:
                yield return Destinations.AccessToken;
                yield return Destinations.IdentityToken;
                yield break;

            case CustomClaims.TrnLookupStatus:
            case CustomClaims.NiNumber:
            case CustomClaims.TrnMatchNiNumber:
                // These are only added by UserClaimHelper if the scope and TrnRequirementLevel are appropriate
                yield return Destinations.IdentityToken;
                yield break;

            default:
                yield break;
        }
    }

    private async Task<User?> GetSignedInUser(AuthenticateResult authenticateResult, UserRequirements userRequirements)
    {
        var signedInUserId = authenticateResult.Principal?.GetUserId();

        if (signedInUserId is null)
        {
            return null;
        }

        var signedInUser = await _dbContext.Users.SingleOrDefaultAsync(u => u.UserId == signedInUserId);

        // If the user is signed in with an incompatible UserType then force the user to sign in again
        if (signedInUser?.UserType is UserType userType && !userRequirements.GetPermittedUserTypes().Contains(userType))
        {
            return null;
        }

        return signedInUser;
    }

    private async Task<ClaimsPrincipal?> InitializeAuthenticationState(User? signedInUser, EnhancedTrnToken? trnToken,
        AuthenticationState authenticationState)
    {
        if (trnToken is null)
        {
            authenticationState.OnSignedInUserProvided(signedInUser);
            return null;
        }

        if (signedInUser is not null)
        {
            _trnTokenHelper.InitializeAuthenticationStateForSignedInUser(signedInUser, authenticationState, trnToken);
            return null;
        }

        var existingValidUser = await _trnTokenHelper.GetExistingValidUserForToken(trnToken);

        if (existingValidUser is not null)
        {
            if (existingValidUser.Trn is null || existingValidUser.Trn == trnToken.Trn)
            {
                _trnTokenHelper.InitializeAuthenticationStateForExistingUser(existingValidUser, authenticationState, trnToken);
                return await authenticationState.SignIn(HttpContext);
            }
        }
        else
        {
            var existingAccountMatch = await _trnTokenHelper.GetExistingAccountMatchForToken(trnToken);
            authenticationState.OnExistingAccountSearch(existingAccountMatch);
            authenticationState.OnTrnTokenProvided(trnToken);
        }

        return null;
    }
}
