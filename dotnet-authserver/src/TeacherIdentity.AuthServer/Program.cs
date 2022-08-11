using System.Security.Cryptography;
using GovUk.Frontend.AspNetCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Prometheus;
using Sentry.AspNetCore;
using Serilog;
using TeacherIdentity.AuthServer.Configuration;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.State;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog((ctx, config) => config.ReadFrom.Configuration(ctx.Configuration));

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.AddServerHeader = false;
        });

        builder.Configuration
            .AddJsonEnvironmentVariable("VCAP_SERVICES", configurationKeyPrefix: "VCAP_SERVICES")
            .AddJsonEnvironmentVariable("VCAP_APPLICATION", configurationKeyPrefix: "VCAP_APPLICATION");

        if (builder.Environment.IsProduction())
        {
            builder.WebHost.UseSentry();
        }

        MetricLabels.ConfigureLabels(builder.Configuration);

        builder.Services.AddAntiforgery(options => options.Cookie.Name = "tis-antiforgery");

        builder.Services.AddGovUkFrontend(options => options.AddImportsToHtml = false);

        builder.Services.AddAuthentication()
            .AddCookie(options =>
            {
                options.Cookie.Name = "tis-auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;

                options.ExpireTimeSpan = TimeSpan.FromMinutes(5);

                options.Events.OnRedirectToLogin = ctx =>
                {
                    var authStateId = Guid.NewGuid();
                    var authState = new AuthenticationState(authStateId, ctx.Properties.RedirectUri!);
                    ctx.HttpContext.Features.Set(new AuthenticationStateFeature(authState));

                    var urlHelperFactory = ctx.HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Mvc.Routing.IUrlHelperFactory>();
                    var actionContext = ctx.HttpContext.RequestServices.GetRequiredService<IActionContextAccessor>().ActionContext!;
                    var urlHelper = urlHelperFactory.GetUrlHelper(actionContext);

                    var redirectUrl = authState.GetNextHopUrl(urlHelper);
                    ctx.Response.Redirect(redirectUrl);

                    return Task.CompletedTask;
                };
            });

        builder.Services.AddControllersWithViews();

        builder.Services.AddRazorPages(options =>
        {
            // Every page within the SignIn folder must have AuthenticationState passed to it
            options.Conventions.AddFolderApplicationModelConvention("/SignIn", model => model.Filters.Add(new RequireAuthenticationStateFilter()));
        });

        builder.Services.AddSession(options =>
        {
            options.Cookie.Name = "tis-session";
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

        var pgConnectionString = GetPostgresConnectionString();

        var healthCheckBuilder = builder.Services.AddHealthChecks()
            .AddNpgSql(pgConnectionString);

        builder.Services.AddDbContext<TeacherIdentityServerDbContext>(options =>
        {
            TeacherIdentityServerDbContext.ConfigureOptions(options, pgConnectionString);
        });

        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<TeacherIdentityServerDbContext>()
                    .ReplaceDefaultEntities<Application, Authorization, Scope, Token, string>();
            })
            .AddServer(options =>
            {
                options
                    .SetAuthorizationEndpointUris("/connect/authorize")
                    //.SetLogoutEndpointUris
                    .SetTokenEndpointUris("/connect/token")
                    .SetUserinfoEndpointUris("/connect/userinfo");

                options.RegisterScopes(Scopes.Email, Scopes.Profile);

                options
                    .AllowAuthorizationCodeFlow()
                    .AllowImplicitFlow()
                    .AllowHybridFlow()
                    .AllowClientCredentialsFlow();

                if (builder.Environment.IsDevelopment() ||
                    builder.Environment.IsUnitTests() ||
                    builder.Environment.IsEndToEndTests())
                {
                    options
                        .AddDevelopmentEncryptionCertificate()
                        .AddDevelopmentSigningCertificate();
                }
                else
                {
                    options
                        .AddEncryptionKey(LoadKey("EncryptionKey"))
                        .AddSigningKey(LoadKey("SigningKey"));
                }

                options.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableLogoutEndpointPassthrough()
                    .EnableTokenEndpointPassthrough()
                    .EnableUserinfoEndpointPassthrough()
                    .EnableStatusCodePagesIntegration();

                options.DisableAccessTokenEncryption();

                options.RegisterClaims(CustomClaims.Trn);
                options.RegisterScopes(CustomScopes.Trn);
            });

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
        });

        builder.Services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

        builder.Services.AddOptions<FindALostTrnIntegrationOptions>()
            .Bind(builder.Configuration.GetSection("FindALostTrnIntegration"))
            .ValidateDataAnnotations();

        builder.Services.AddTransient<FindALostTrnIntegrationHelper>();

        builder.Services.AddSingleton<IAuthenticationStateProvider, SessionAuthenticationStateProvider>();

        builder.Services.Configure<SentryAspNetCoreOptions>(options =>
        {
            var paasEnvironmentName = builder.Configuration["PaasEnvironment"];
            if (!string.IsNullOrEmpty(paasEnvironmentName))
            {
                options.Environment = paasEnvironmentName;
            }

            var gitSha = builder.Configuration["GitSha"];
            if (!string.IsNullOrEmpty(gitSha))
            {
                options.Release = gitSha;
            }
        });

        var app = builder.Build();

        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseMigrationsEndPoint();
        }
        else if (!app.Environment.IsUnitTests())
        {
            app.UseExceptionHandler("/error");
            app.UseStatusCodePagesWithReExecute("/error", "?code={0}");
            app.UseForwardedHeaders();
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.UseStaticFiles();

        app.UseSession();

        app.UseMiddleware<AuthenticationStateMiddleware>();

        app.UseRouting();

        if (builder.Environment.IsProduction())
        {
            app.UseSentryTracing();
        }

        app.UseHttpMetrics();

        app.UseHealthChecks("/status");

        app.UseAuthentication();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapRazorPages();
            endpoints.MapMetrics();

            endpoints.MapGet("/health", async context =>
            {
                await context.Response.WriteAsync("OK");
            });

            // TODO Remove the stub Find endpoints for production deployments
        });

        app.Run();

        string GetPostgresConnectionString()
        {
            return builder.Configuration.GetConnectionString("DefaultConnection") ?? GetConnectionStringForPaasService();

            string GetConnectionStringForPaasService()
            {
                var connStrBuilder = new NpgsqlConnectionStringBuilder()
                {
                    Host = builder.Configuration.GetValue<string>("VCAP_SERVICES:postgres:0:credentials:host"),
                    Database = builder.Configuration.GetValue<string>("VCAP_SERVICES:postgres:0:credentials:name"),
                    Username = builder.Configuration.GetValue<string>("VCAP_SERVICES:postgres:0:credentials:username"),
                    Password = builder.Configuration.GetValue<string>("VCAP_SERVICES:postgres:0:credentials:password"),
                    Port = builder.Configuration.GetValue<int>("VCAP_SERVICES:postgres:0:credentials:port"),
                    SslMode = SslMode.Require,
                    TrustServerCertificate = true
                };

                return connStrBuilder.ConnectionString;
            }
        }

        SecurityKey LoadKey(string configurationKey)
        {
            var rsa = RSA.Create();
            rsa.FromXmlString(builder.Configuration[configurationKey]);

            return new RsaSecurityKey(rsa);
        }
    }
}
