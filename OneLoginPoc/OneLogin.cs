using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace OneLoginPoc;

public class OneLoginAuthenticationOptions
{
    public string? MetadataAddress { get; set; }

    public string? ClientId { get; set; }

    public ICollection<string> Scope { get; } = new List<string>()
    {
        "openid",
        "email"
    };

    public string? UiLocales { get; set; }

    public string? VectorsOfTrust { get; set; }

    public ICollection<string> Claims { get; } = new List<string>()
    {
        "https://vocab.account.gov.uk/v1/coreIdentityJWT"
    };

    public string? SignInScheme { get; set; }

    public SigningCredentials? ClientAuthenticationCredentials { get; set; }

    public string? ClientAssertionJwtAudience { get; set; }

    public TimeSpan ClientAssertionJwtExpiry { get; set; } = TimeSpan.FromMinutes(5);  // One Login docs recommend 5 minutes
}

public static class OneLoginAuthenticationDefaults
{
    public const string AuthenticationScheme = "OneLogin";
}

public class ConfigureOpenIdConnectFromOneLoginOptions : IPostConfigureOptions<OpenIdConnectOptions>
{
    private readonly IOptionsMonitor<OneLoginAuthenticationOptions> _oneLoginOptionsAccessor;

    public ConfigureOpenIdConnectFromOneLoginOptions(IOptionsMonitor<OneLoginAuthenticationOptions> oneLoginOptions)
    {
        _oneLoginOptionsAccessor = oneLoginOptions;
    }

    public void PostConfigure(string? name, OpenIdConnectOptions options)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        var oneLoginOptions = _oneLoginOptionsAccessor.Get(name);
        if (oneLoginOptions is null)
        {
            return;
        }

        options.MetadataAddress = oneLoginOptions.MetadataAddress;
        options.ClientId = oneLoginOptions.ClientId;

        // RedirectPost renders a form that's automatically submitted via JavaScript;
        // to save us having to GDS-ify that for users who don't have JavaScript, use RedirectGet instead.
        options.AuthenticationMethod = OpenIdConnectRedirectBehavior.RedirectGet;

        options.SignInScheme = oneLoginOptions.SignInScheme;
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.ResponseMode = OpenIdConnectResponseMode.Query;
        options.ProtocolValidator.RequireNonce = true;
        options.UsePkce = false;
        //options.UseTokenLifetime = true;  // TODO make configurable?
        options.GetClaimsFromUserInfoEndpoint = true;
        options.SaveTokens = false;
        options.MapInboundClaims = false;
        options.TokenValidationParameters.NameClaimType = "sub";  // TODO Maybe get this from the identity JWT instead?
        //options.TokenValidationParameters.IssuerSigningKey  // TODO get from configuration
        options.TokenValidationParameters.ValidateAudience = true;
        options.TokenValidationParameters.ValidateIssuer = true;
        options.TokenValidationParameters.AuthenticationType = "GOV.UK One Login";
        //options.CallbackPath  // TODO add configuration option for this
        //options.SignedOutCallbackPath  // TODO add configuration option for this

        options.Scope.Clear();
        foreach (var scope in oneLoginOptions.Scope)
        {
            options.Scope.Add(scope);
        }

        var claimsRequest = GenerateClaimsRequest(oneLoginOptions.Claims);

        options.Events.OnRedirectToIdentityProvider = ctx =>
        {
            ctx.ProtocolMessage.Parameters.Add("vtr", oneLoginOptions.VectorsOfTrust);
            ctx.ProtocolMessage.Parameters.Add("claims", claimsRequest);

            if (oneLoginOptions.UiLocales is not null)
            {
                ctx.ProtocolMessage.Parameters.Add("ui_locales", oneLoginOptions.UiLocales);
            }

            return Task.CompletedTask;
        };

        options.Events.OnRemoteFailure = async ctx =>
        {
            // TODO handle access_denied
            // See https://docs.sign-in.service.gov.uk/integrate-with-integration-environment/integrate-with-code-flow/#error-handling-for-make-an-authorisation-request
        };

        options.Events.OnAuthorizationCodeReceived = ctx =>
        {
            // private_key_jwt authentication
            // https://openid.net/specs/openid-connect-core-1_0.html#ClientAuthentication

            var jwt = CreateClientAssertionJwt();

            ctx.TokenEndpointRequest!.RemoveParameter("client_secret");
            ctx.TokenEndpointRequest.SetParameter("client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer");
            ctx.TokenEndpointRequest.SetParameter("client_assertion", jwt);

            return Task.CompletedTask;
        };

        options.Events.OnTicketReceived = async ctx =>
        {
            // TODO Upsert user into DB, add their user ID to Principal etc.
            //((System.Security.Claims.ClaimsIdentity)ctx.Principal.Identity).AddClaim(...)
            // or maybe use an IClaimsTransformation instead?
        };

        string CreateClientAssertionJwt()
        {
            // https://docs.sign-in.service.gov.uk/integrate-with-integration-environment/integrate-with-code-flow/#create-a-jwt

            // TODO Move this validation
            if (oneLoginOptions.ClientAssertionJwtAudience is null)
            {
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
                throw new ArgumentException($"Options.{nameof(CreateClientAssertionJwt)} must be provided.", nameof(OneLoginAuthenticationOptions.ClientAssertionJwtAudience));
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
            }

            var handler = new JsonWebTokenHandler();

            var jwtId = Guid.NewGuid().ToString("N");

            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                Claims = new Dictionary<string, object>()
                {
                    { "aud", oneLoginOptions.ClientAssertionJwtAudience },
                    { "iss", options.ClientId! },
                    { "sub", options.ClientId! },
                    { "exp", DateTimeOffset.UtcNow.Add(oneLoginOptions.ClientAssertionJwtExpiry).ToUnixTimeSeconds() },
                    { "jti", jwtId },
                    { "iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
                },
                SigningCredentials = oneLoginOptions.ClientAuthenticationCredentials
            };

            return handler.CreateToken(tokenDescriptor);
        }

        static string GenerateClaimsRequest(IEnumerable<string> claims)
        {
            // https://openid.net/specs/openid-connect-core-1_0.html#ClaimsParameter
            // https://docs.sign-in.service.gov.uk/integrate-with-integration-environment/integrate-with-code-flow/#create-a-url-encoded-json-object-for-lt-claims-request-gt

            var userinfo = new JsonObject();

            foreach (var claim in claims)
            {
                userinfo.Add(claim, JsonValue.Create((string?)null));
            }

            var root = new JsonObject
            {
                { "userinfo", userinfo }
            };

            return root.ToString();
        }
    }
}

public static class Extensions
{
    public static AuthenticationBuilder AddOneLogin(
        this AuthenticationBuilder builder,
        Action<OneLoginAuthenticationOptions> configureOptions)
    {
        return AddOneLogin(builder, OneLoginAuthenticationDefaults.AuthenticationScheme, configureOptions);
    }

    public static AuthenticationBuilder AddOneLogin(
        this AuthenticationBuilder builder,
        string authenticationScheme,
        Action<OneLoginAuthenticationOptions> configureOptions)
    {
        builder.Services.Configure<OneLoginAuthenticationOptions>(authenticationScheme, configureOptions);

        // Inserting at index 0 ensures ConfigureOpenIdConnectFromOneLoginOptions runs before OpenIdConnect's own IPostConfigureOptions
        builder.Services.Insert(0, ServiceDescriptor.Transient<IPostConfigureOptions<OpenIdConnectOptions>, ConfigureOpenIdConnectFromOneLoginOptions>());

        return builder.AddOpenIdConnect(authenticationScheme, _ => { });
    }
}