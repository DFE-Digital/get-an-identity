using System.Security.Claims;
using System.Security.Cryptography;
using AspNetCoreRateLimit;
using AspNetCoreRateLimit.Redis;
using Dfe.Analytics.AspNetCore;
using GovUk.Frontend.AspNetCore;
using Hangfire;
using Joonasw.AspNetCore.SecurityHeaders;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Sentry.AspNetCore;
using Serilog;
using StackExchange.Redis;
using TeacherIdentity.AuthServer.Api;
using TeacherIdentity.AuthServer.EventProcessing;
using TeacherIdentity.AuthServer.Infrastructure;
using TeacherIdentity.AuthServer.Infrastructure.Filters;
using TeacherIdentity.AuthServer.Infrastructure.ModelBinding;
using TeacherIdentity.AuthServer.Infrastructure.Security;
using TeacherIdentity.AuthServer.Infrastructure.Swagger;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Notifications;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Pages.SignIn.Trn;
using TeacherIdentity.AuthServer.Services;
using TeacherIdentity.AuthServer.State;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace TeacherIdentity.AuthServer;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var hostingEnvironmentName = builder.Configuration["EnvironmentName"];
        var baseAddress = builder.Configuration["BaseAddress"] ?? throw new Exception("BaseAddress missing from configuration.");

        builder.Host.UseSerilog((ctx, config) => config.ReadFrom.Configuration(ctx.Configuration));

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.AddServerHeader = false;
        });

        if (builder.Environment.IsProduction())
        {
            builder.WebHost.UseSentry();

            builder.Services.Configure<SentryAspNetCoreOptions>(options =>
            {
                options.Environment = hostingEnvironmentName ?? throw new Exception("EnvironmentName missing from configuration.");

                var gitSha = builder.Configuration["GitSha"];
                if (!string.IsNullOrEmpty(gitSha))
                {
                    options.Release = gitSha;
                }
            });

            builder.Services.AddDataProtection()
                .PersistKeysToAzureBlobStorage(
                    connectionString: builder.Configuration.GetConnectionString("DataProtectionBlobStorage"),
                    containerName: builder.Configuration["DataProtectionKeysContainerName"],
                    blobName: "keys");

            var redisConfiguration = ConfigurationOptions.Parse(
                builder.Configuration.GetConnectionString("Redis") ?? throw new Exception("Connection string Redis is missing."));

            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.ConfigurationOptions = redisConfiguration;
            });

            builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConfiguration));

            ConfigureRateLimitServices();
        }

        builder.Services.AddAntiforgery(options =>
        {
            options.Cookie.Name = "tis-antiforgery";
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.SuppressXFrameOptionsHeader = true;
        });

        builder.Services.AddGovUkFrontend(options =>
        {
            options.AddImportsToHtml = false;
            options.DefaultButtonPreventDoubleClick = true;
        });

        builder.Services.AddAuthentication(options => options.DefaultForbidScheme = CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "tis-auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

                options.Events.OnRedirectToAccessDenied = async ctx =>
                {
                    var viewResult = new ViewResult() { ViewName = "Forbidden", StatusCode = StatusCodes.Status403Forbidden };
                    var viewResultExecutor = ctx.HttpContext.RequestServices.GetRequiredService<IActionResultExecutor<ViewResult>>();

                    var actionContext = new ActionContext(ctx.HttpContext, ctx.HttpContext.GetRouteData(), new ActionDescriptor());

                    await viewResultExecutor.ExecuteAsync(actionContext, viewResult);
                };

                options.Events.OnRedirectToLogin = ctx =>
                {
                    // If we get here then sign in is happening *outside* of an OAuth authorization flow.
                    // Sign in any user; authorization is applied separately.
                    var userRequirements = UserRequirements.None;

                    if (!ctx.HttpContext.TryGetAuthenticationState(out var authenticationState))
                    {
                        var returnUrl = QueryHelpers.ParseQuery(new Uri(ctx.RedirectUri).Query)["returnUrl"].ToString();

                        authenticationState = new AuthenticationState(
                            journeyId: Guid.NewGuid(),
                            userRequirements,
                            postSignInUrl: returnUrl,
                            sessionId: null,
                            startedAt: DateTime.UtcNow);

                        ctx.HttpContext.Features.Set(new AuthenticationStateFeature(authenticationState));
                    }

                    var linkGenerator = ctx.HttpContext.RequestServices.GetRequiredService<IIdentityLinkGenerator>();
                    ctx.Response.Redirect(authenticationState.GetNextHopUrl(linkGenerator));

                    return Task.CompletedTask;
                };

                options.Events.OnSigningOut = async ctx =>
                {
                    var httpContext = ctx.HttpContext;

                    ClaimsPrincipal? user = null;
                    string? clientId = null;

                    var oidcAuthenticateResult = await httpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                    if (oidcAuthenticateResult.Succeeded)
                    {
                        user = oidcAuthenticateResult.Principal;
                        clientId = user.FindFirstValue(Claims.Audience);
                    }
                    else
                    {
                        var cookiesAuthenticateResult = await httpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        if (cookiesAuthenticateResult.Succeeded)
                        {
                            user = cookiesAuthenticateResult.Principal;
                        }
                    }

                    if (user is not null)
                    {
                        await ctx.HttpContext.SaveUserSignedOutEvent(user, clientId);
                    }
                };
            })
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.AuthenticationScheme, _ => { })
            .AddJwtBearer(options =>
            {
                options.Authority = baseAddress;
                options.MapInboundClaims = false;
                options.TokenValidationParameters.ValidateAudience = false;

                if (builder.Environment.IsUnitTests() || builder.Environment.IsEndToEndTests())
                {
                    options.RequireHttpsMetadata = false;
                }
            });

        builder.Services.Configure<AuthenticationOptions>(options =>
            options.AddScheme(
                "Delegated",
                builder => builder.HandlerType = typeof(DelegatedAuthenticationHandler)));

        builder.Services.Configure<DelegatedAuthenticationOptions>("Delegated", options =>
        {
            options.OnUserSignedIn = async (httpContext, principal) =>
            {
                await httpContext.SaveUserSignedInEvent(principal);
            };
        });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AuthorizationPolicies.Authenticated,
                policy => policy
                    .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser());

            options.AddPolicy(
                AuthorizationPolicies.GetAnIdentityAdmin,
                policy => policy
                    .AddAuthenticationSchemes("Delegated")
                    .RequireAuthenticatedUser()
                    .RequireRole(StaffRoles.GetAnIdentityAdmin));

            options.AddPolicy(
                AuthorizationPolicies.GetAnIdentitySupport,
                policy => policy
                    .AddAuthenticationSchemes("Delegated")
                    .RequireAuthenticatedUser()
                    .RequireRole(StaffRoles.GetAnIdentityAdmin, StaffRoles.GetAnIdentitySupport));

            options.AddPolicy(
                AuthorizationPolicies.Staff,
                policy => policy
                    .AddAuthenticationSchemes("Delegated")
                    .RequireAuthenticatedUser()
                    .RequireClaim(CustomClaims.UserType, UserClaimHelper.MapUserTypeToClaimValue(UserType.Staff)));

            options.AddPolicy(
                AuthorizationPolicies.TrnLookupApi,
                policy => policy
                    .AddAuthenticationSchemes(ApiKeyAuthenticationHandler.AuthenticationScheme)
                    .RequireAuthenticatedUser());

            options.AddPolicy(
                AuthorizationPolicies.ApiUserRead,
                policy => policy
                    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .AddRequirements(new ScopeAuthorizationRequirement(CustomScopes.UserRead, CustomScopes.UserWrite)));

            options.AddPolicy(
                AuthorizationPolicies.ApiUserWrite,
                policy => policy
                    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .AddRequirements(new ScopeAuthorizationRequirement(CustomScopes.UserWrite)));
        });

        builder.Services.AddSingleton<IAuthorizationHandler, RequireScopeAuthorizationHandler>();

        builder.Services.AddControllersWithViews()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new Infrastructure.Json.DateOnlyConverter());
            });

        builder.Services.AddRazorPages(options =>
        {
            // Every page within the SignIn folder must have AuthenticationState passed to it.
            options.Conventions.AddFolderApplicationModelConvention(
                "/SignIn",
                model =>
                {
                    model.Filters.Add(new RequireAuthenticationStateFilterFactory());
                    model.Filters.Add(new NoCachePageFilter());
                });

            options.Conventions.AddFolderApplicationModelConvention(
                "/SignIn/Trn",
                model =>
                {
                    model.Filters.Add(new CheckUserRequirementsForTrnJourneyFilterFactory());
                });

            options.Conventions.AddFolderApplicationModelConvention(
                "/Admin",
                model =>
                {
                    model.Filters.Add(new AuthorizeFilter(AuthorizationPolicies.Staff));
                });

            options.Conventions.AddFolderApplicationModelConvention(
                "/Authenticated",
                model =>
                {
                    model.Filters.Add(new AuthorizeFilter(AuthorizationPolicies.Authenticated));
                });
        });

        builder.Services.AddSession(options =>
        {
            options.Cookie.Name = "tis-session";
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.IdleTimeout = TimeSpan.FromDays(5);
        });

        var pgConnectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
            throw new Exception("Connection string DefaultConnection is missing.");

        var healthCheckBuilder = builder.Services.AddHealthChecks()
            .AddNpgSql(pgConnectionString);

        builder.Services.AddDbContext<TeacherIdentityServerDbContext>(
            options => TeacherIdentityServerDbContext.ConfigureOptions(options, pgConnectionString),
            contextLifetime: ServiceLifetime.Transient,
            optionsLifetime: ServiceLifetime.Singleton);

        builder.Services.AddDbContextFactory<TeacherIdentityServerDbContext>(
            options => TeacherIdentityServerDbContext.ConfigureOptions(options, pgConnectionString));

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();
        }

        builder.Services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<TeacherIdentityServerDbContext>()
                    .ReplaceDefaultEntities<Application, Authorization, Scope, Token, string>();

                options.AddApplicationStore<TeacherIdentityApplicationStore>();
                options.ReplaceApplicationManager<TeacherIdentityApplicationManager>();

                builder.Services.AddTransient<TeacherIdentityApplicationStore>(
                    sp => (TeacherIdentityApplicationStore)sp.GetRequiredService<IOpenIddictApplicationStore<Application>>());
            })
            .AddServer(options =>
            {
                options.AddEventHandler<ApplyAuthorizationResponseContext>(
                    builder => builder.UseSingletonHandler<ProcessAuthorizationResponseHandler>());

                options.SetIssuer(new Uri(baseAddress));

                options
                    .SetAuthorizationEndpointUris("/connect/authorize")
                    .SetLogoutEndpointUris("/connect/signout")
                    .SetTokenEndpointUris("/connect/token")
                    .SetUserinfoEndpointUris("/connect/userinfo");

                options
                    .AllowAuthorizationCodeFlow()
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
                    var encryptionKeysConfig = builder.Configuration.GetSection("EncryptionKeys").Get<string[]>() ?? Array.Empty<string>();
                    var signingKeysConfig = builder.Configuration.GetSection("SigningKeys").Get<string[]>() ?? Array.Empty<string>();

                    foreach (var value in encryptionKeysConfig)
                    {
                        options.AddEncryptionKey(LoadKey(value));
                    }

                    foreach (var value in signingKeysConfig)
                    {
                        options.AddSigningKey(LoadKey(value));
                    }
                }

                options.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableLogoutEndpointPassthrough()
                    .EnableTokenEndpointPassthrough()
                    .EnableUserinfoEndpointPassthrough()
                    .EnableStatusCodePagesIntegration();

                options.DisableAccessTokenEncryption();
                options.SetAccessTokenLifetime(TimeSpan.FromHours(1));

                options.RegisterClaims(CustomClaims.Trn);
                options.RegisterScopes(
                    CustomScopes.All
                        .Append(Scopes.Email)
                        .Append(Scopes.Profile)
                        .ToArray());
            });

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.All;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        builder.Services.AddMvc(options =>
        {
            options.Conventions.Add(new Infrastructure.ApplicationModel.ApiControllerConvention());

            options.ModelBinderProviders.Insert(0, new ProtectedStringModelBinderProvider());
        });

        builder.Services.AddCsp(nonceByteAmount: 32);

        builder.Services.AddApplicationInsightsTelemetry();

        builder.Services.AddHsts(options =>
        {
            options.Preload = true;
            options.IncludeSubDomains = true;
            options.MaxAge = TimeSpan.FromDays(365);
        });

        builder.Services.AddMemoryCache();

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddSingleton<IDistributedCache, DevelopmentFileDistributedCache>();
        }

        builder.Services.AddApiSwagger();

        builder.Services.AddApplicationInsightsTelemetryProcessor<Infrastructure.ApplicationInsights.FilterDependenciesTelemetryProcessor>();

        if (builder.Environment.IsProduction())
        {
            builder.Services.AddDfeAnalytics(options =>
            {
                options.Environment = hostingEnvironmentName ?? throw new Exception("EnvironmentName missing from configuration.");
                options.UserIdClaimType = Claims.Subject;
            });
        }

        // Custom MVC filters & extensions
        builder.Services
            .AddSingleton<IActionDescriptorProvider, Infrastructure.ApplicationModel.RemoveStubFindEndpointsActionDescriptorProvider>()
            .AddTransient<RequireAuthenticationStateFilter>()
            .Decorate<ProblemDetailsFactory, Api.Validation.CamelCaseErrorKeysProblemDetailsFactory>()
            .AddSingleton<CheckUserRequirementsForTrnJourneyFilter>();

        builder.Services
            .AddSingleton<IClock, SystemClock>()
            .AddSingleton<IAuthenticationStateProvider, SessionAuthenticationStateProvider>()
            .AddTransient<IRequestClientIpProvider, RequestClientIpProvider>()
            .AddSingleton<IIdentityLinkGenerator, IdentityLinkGenerator>()
            .AddSingleton<IApiClientRepository, ConfigurationApiClientRepository>()
            .AddTransient<ICurrentClientProvider, AuthenticationStateCurrentClientProvider>()
            .AddSingleton<IEventObserver, PublishNotificationsEventObserver>()
            .AddSingleton<ProtectedStringFactory>()
            .AddTransient<ClientScopedViewHelper>()
            .AddTransient<IActionContextAccessor, ActionContextAccessor>()
            .AddTransient<TrnLookupHelper>()
            .AddTransient<UserClaimHelper>();

        builder.Services.AddNotifications(builder.Environment, builder.Configuration);

        builder.Services.AddAuthServerServices(builder.Environment, builder.Configuration, pgConnectionString);

        builder.Services.AddApiServices();

        var app = builder.Build();

        if (builder.Environment.IsProduction() &&
            Environment.GetEnvironmentVariable("WEBSITE_ROLE_INSTANCE_ID") == "0")
        {
            await MigrateDatabase();
        }

        app.UseSerilogRequestLogging();
        app.UseMiddleware<Infrastructure.Middleware.RequestLogContextMiddleware>();

        app.UseWhen(
            context => !context.Request.Path.StartsWithSegments("/api") && !context.Request.Path.StartsWithSegments("/connect"),
            a =>
            {
                if (app.Environment.IsDevelopment())
                {
                    a.UseDeveloperExceptionPage();
                }
                else if (!app.Environment.IsUnitTests())
                {
                    a.UseExceptionHandler("/error");
                    a.UseStatusCodePagesWithReExecute("/error", "?code={0}");
                }
            });

        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
        }
        else if (!app.Environment.IsUnitTests())
        {
            app.UseForwardedHeaders();
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.UseStaticFiles();

        app.UseSession();

        app.UseMiddleware<AuthenticationStateMiddleware>();
        app.UseMiddleware<Infrastructure.Middleware.AppendSessionIdToAnalyticsEventsMiddleware>();

        app.UseRouting();

        if (builder.Environment.IsProduction())
        {
            app.UseSentryTracing();
        }

        app.UseHealthChecks("/status");

        app.UseSwagger(options =>
        {
            options.PreSerializeFilters.Add((_, request) =>
            {
                request.HttpContext.Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
            });
        });

        app.UseAuthentication();

        app.UseAuthorization();

        if (!builder.Environment.IsUnitTests() && !builder.Environment.IsEndToEndTests())
        {
            app.MapHangfireDashboardWithAuthorizationPolicy(authorizationPolicyName: AuthorizationPolicies.GetAnIdentityAdmin, "/_hangfire");
        }

        if (builder.Environment.IsDevelopment())
        {
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "V1");

                options.OAuthClientId("swagger-ui");
                options.OAuthClientSecret("super-secret");
                options.OAuthUseBasicAuthenticationWithAccessCodeGrant();
                options.OAuthUsePkce();
            });
        }

        if (builder.Environment.IsProduction())
        {
            app.UseWhen(
                ctx => ctx.Request.Path.StartsWithSegments("/api") && !ctx.Request.Path.StartsWithSegments("/api/find-trn"),
                a => a.UseMiddleware<Infrastructure.Middleware.RateLimitMiddleware>());
        }

        if (builder.Environment.IsProduction())
        {
            app.UseWhen(
                ctx => ctx.Request.Path != new PathString("/health") && ctx.Request.Headers.UserAgent != "AlwaysOn",
                a => a.UseDfeAnalytics());
        }

        // Add security headers middleware but exclude the endpoints managed by OpenIddict and the API
        app.UseWhen(
            ctx => !ctx.Request.Path.StartsWithSegments(new PathString("/connect")) && !ctx.Request.Path.StartsWithSegments(new PathString("/api")),
            a =>
            {
                a.UseMiddleware<Infrastructure.Middleware.AppendSecurityResponseHeadersMiddleware>();

                a.UseCsp(options =>
                {
                    var pageTemplateHelper = app.Services.GetRequiredService<PageTemplateHelper>();

                    options.ByDefaultAllow
                        .FromSelf();

                    options.AllowScripts
                        .FromSelf()
                        .From(pageTemplateHelper.GetCspScriptHashes())
                        .AddNonce();

                    // Ensure ASP.NET Core's auto refresh works
                    // See https://github.com/dotnet/aspnetcore/issues/33068
                    if (builder.Environment.IsDevelopment())
                    {
                        options.AllowConnections
                            .ToAnywhere();
                    }
                });
            });

        app.MapGet("/health", async context =>
        {
            await context.Response.WriteAsync("OK");
        });

        var gitSha = builder.Configuration["GitSha"];
        if (!string.IsNullOrEmpty(gitSha))
        {
            app.MapGet("/_sha", async context =>
            {
                await context.Response.WriteAsync(gitSha);
            });
        }

        if (builder.Environment.IsDevelopment())
        {
            app.MapPost("/webhook", async context =>
            {
                var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("WebHookDebug");

                using var sr = new StreamReader(context.Request.Body);
                var body = await sr.ReadToEndAsync();
                logger.LogInformation("Received web hook: {Payload}", body);
            });
        }

        app.MapControllers();
        app.MapRazorPages();
        app.MapApiEndpoints();

        app.Run();

        void ConfigureRateLimitServices()
        {
            builder.Services.Configure<ClientRateLimitOptions>(builder.Configuration.GetSection("ClientRateLimiting"));
            builder.Services.AddRedisRateLimiting();
            builder.Services.AddSingleton<IRateLimitConfiguration, ApiRateLimitConfiguration>();
        }

        SecurityKey LoadKey(string configurationValue)
        {
            var rsa = RSA.Create();
            rsa.FromXmlString(configurationValue);
            return new RsaSecurityKey(rsa);
        }

        async Task MigrateDatabase()
        {
            await using var scope = app.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TeacherIdentityServerDbContext>();
            await dbContext.Database.MigrateAsync();
        }
    }
}
