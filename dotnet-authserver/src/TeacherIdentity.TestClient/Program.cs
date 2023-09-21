using System.IdentityModel.Tokens.Jwt;
using GovUk.Frontend.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;

namespace TeacherIdentity.TestClient;

public class Program
{
    public static void Main(string[] args)
    {
        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllersWithViews();

        builder.Services.AddGovUkFrontend();

        builder.Services.AddDistributedMemoryCache();

        builder.Services.AddSession(options =>
        {
            options.Cookie.Name = "testclient-session";
            options.Cookie.IsEssential = true;
        });

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

                options.GetClaimsFromUserInfoEndpoint = true;
                options.SaveTokens = true;

                // Log the access token to the console for debugging
                options.Events.OnTokenResponseReceived = ctx =>
                {
                    Console.WriteLine(ctx.TokenEndpointResponse.AccessToken);
                    return Task.CompletedTask;
                };

                options.Events.OnRedirectToIdentityProvider = ctx =>
                {
                    var customScope = ctx.HttpContext.Request.Query["scope"].ToString();
                    if (!string.IsNullOrEmpty(customScope))
                    {
                        ctx.ProtocolMessage.Scope = customScope;
                    }

                    var trnRequirement = ctx.HttpContext.Request.Query["trn_requirement"].ToString();
                    if (!string.IsNullOrEmpty(trnRequirement))
                    {
                        ctx.ProtocolMessage.SetParameter("trn_requirement", trnRequirement);
                    }

                    var trnMatchPolicy = ctx.HttpContext.Request.Query["trn_match_policy"].ToString();
                    if (!string.IsNullOrEmpty(trnMatchPolicy))
                    {
                        ctx.ProtocolMessage.SetParameter("trn_match_policy", trnMatchPolicy);
                    }

                    var trnToken = ctx.HttpContext.Request.Query["trn_token"].ToString();
                    if (!string.IsNullOrEmpty(trnToken))
                    {
                        ctx.ProtocolMessage.SetParameter("trn_token", trnToken);
                    }

                    ctx.ProtocolMessage.SetParameter("session_id", ctx.HttpContext.Session.Id);

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

        if (app.Environment.IsProduction())
        {
            app.UseForwardedHeaders();
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseSession();

        // Force the session cookie to be created
        app.Use(async (ctx, next) =>
        {
            ctx.Session.Set("dummy", new byte[] { 42 });
            await next(ctx);
        });

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
