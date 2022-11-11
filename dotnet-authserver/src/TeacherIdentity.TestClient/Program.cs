using System.IdentityModel.Tokens.Jwt;
using GovUk.Frontend.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Logging;

namespace TeacherIdentity.TestClient;

public class Program
{
    public static void Main(string[] args)
    {
        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

        var builder = WebApplication.CreateBuilder(args);

        if (builder.Environment.IsDevelopment())
        {
            IdentityModelEventSource.ShowPII = true;
        }

        // Add services to the container.
        builder.Services.AddControllersWithViews();

        builder.Services.AddGovUkFrontend();

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = "Cookies";
                options.DefaultChallengeScheme = "oidc";
            })
            .AddCookie("Cookies", options =>
            {
                options.ExpireTimeSpan = TimeSpan.FromSeconds(30);
            })
            .AddOpenIdConnect("oidc", options =>
            {
                options.Authority = builder.Configuration.GetValue<string>("SignInAuthority");

                options.ClientId = builder.Configuration.GetValue<string>("ClientId");
                options.ClientSecret = builder.Configuration.GetValue<string>("ClientSecret");
                options.ResponseType = "code";
                options.CallbackPath = new PathString("/oidc/callback");
                options.SignedOutCallbackPath = new PathString("/oidc/signout-callback");
                options.UsePkce = true;

                options.Scope.Clear();
                options.Scope.Add("email");
                options.Scope.Add("openid");
                options.Scope.Add("profile");

                options.SaveTokens = true;

                var oidcBackChannelAuthority = builder.Configuration["OidcBackChannelAuthority"];
                if (!string.IsNullOrEmpty(oidcBackChannelAuthority))
                {
                    options.BackchannelHttpHandler = new RewriteRequestDomainDelegatingHandler("https", oidcBackChannelAuthority)
                    {
                        InnerHandler = new SocketsHttpHandler()
                        {
                            SslOptions = new()
                            {
                                RemoteCertificateValidationCallback = delegate { return true; }
                            }
                        }
                    };
                }

                // Log the access token to the console for debugging
                options.Events.OnTokenResponseReceived = ctx =>
                {
                    Console.WriteLine(ctx.TokenEndpointResponse.AccessToken);
                    return Task.CompletedTask;
                };

                options.Events.OnRedirectToIdentityProvider = ctx =>
                {
                    var customScope = ctx.HttpContext.Request.Query["scope"].ToString();
                    if (string.IsNullOrEmpty(customScope))
                    {
                        customScope = "trn";
                    }

                    ctx.ProtocolMessage.Scope += " " + customScope;

                    return Task.CompletedTask;
                };

                options.Events.OnRemoteFailure = async ctx =>
                {
                    if (ctx.Failure?.Message == "Correlation failed.")
                    {
                        // This will happen when the back button is used to go back to AuthServer and the authorization is completed again.
                        // In such a case, check if the user is signed in (they should be) and redirect onwards if they are.
                        // If they're not signed in, let the error bubble up.

                        var cookieAuthResult = await ctx.HttpContext.AuthenticateAsync("Cookies");
                        if (cookieAuthResult.Succeeded)
                        {
                            ctx.HandleResponse();
                            ctx.Response.Redirect(ctx.Properties?.RedirectUri ?? "/");
                        }
                    }
                };

                if (!builder.Environment.IsProduction())
                {
                    options.RequireHttpsMetadata = false;
                    options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                    options.NonceCookie.SameSite = SameSiteMode.Lax;
                }
            });

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.All;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsProduction())
        {
            app.UseForwardedHeaders();
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }

    private class RewriteRequestDomainDelegatingHandler : DelegatingHandler
    {
        private readonly string _scheme;
        private readonly string _authority;

        public RewriteRequestDomainDelegatingHandler(string scheme, string authority)
        {
            _scheme = scheme;
            _authority = authority;
        }

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri is null || !request.RequestUri.IsAbsoluteUri)
            {
                throw new NotSupportedException();
            }

            request.RequestUri = new Uri($"{_scheme}://{_authority}{request.RequestUri.GetComponents(UriComponents.PathAndQuery, UriFormat.UriEscaped)}");

            return base.SendAsync(request, cancellationToken);
        }
    }
}
