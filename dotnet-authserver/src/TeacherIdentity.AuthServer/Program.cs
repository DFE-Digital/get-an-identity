using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
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
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Sentry.AspNetCore;
using Serilog;
using TeacherIdentity.AuthServer.Api;
using TeacherIdentity.AuthServer.EventProcessing;
using TeacherIdentity.AuthServer.Helpers;
using TeacherIdentity.AuthServer.Infrastructure;
using TeacherIdentity.AuthServer.Infrastructure.Filters;
using TeacherIdentity.AuthServer.Infrastructure.ModelBinding;
using TeacherIdentity.AuthServer.Infrastructure.RateLimiting;
using TeacherIdentity.AuthServer.Infrastructure.Redis;
using TeacherIdentity.AuthServer.Infrastructure.Security;
using TeacherIdentity.AuthServer.Infrastructure.Swagger;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Notifications;
using TeacherIdentity.AuthServer.Oidc;
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
            builder.WebHost.UseSentry(options =>
            {
                options.SetBeforeSend((Sentry.SentryEvent e) =>
                {
                    if (e.Exception is not null && !SentryErrors.ShouldReport(e.Exception))
                    {
                        return null;
                    }

                    return e;
                });
            });

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

                    if (!ctx.HttpContext.TryGetAuthenticationState(out var authenticationState))
                    {
                        var returnUrl = QueryHelpers.ParseQuery(new Uri(ctx.RedirectUri).Query)["returnUrl"].ToString();

                        // If we're signing in to an admin page, only allow Staff users to sign in.
                        // This requirement has an effect on the journey the user sees (i.e. there's no 'Register' prompt).
                        // Ideally we would infer this requirement from the policy but getting at the policy that
                        // triggered this sign in is non-trivial.
                        var userRequirements = new PathString(returnUrl).StartsWithSegments("/admin") ?
                            UserRequirements.StaffUserType :
                            UserRequirements.None;

                        authenticationState = new AuthenticationState(
                            journeyId: Guid.NewGuid(),
                            userRequirements,
                            postSignInUrl: returnUrl,
                            sessionId: null,
                            startedAt: DateTime.UtcNow);

                        ctx.HttpContext.Features.Set(new AuthenticationStateFeature(authenticationState));
                    }

                    var signInJourneyProvider = ctx.HttpContext.RequestServices.GetRequiredService<SignInJourneyProvider>();
                    var signInJourney = signInJourneyProvider.GetSignInJourney(authenticationState, ctx.HttpContext);
                    ctx.Response.Redirect(signInJourney.GetStartStepUrl());

                    return Task.CompletedTask;
                };

                options.Events.OnSigningOut = async ctx =>
                {
                    var httpContext = ctx.HttpContext;

                    ClaimsPrincipal? user = null;
                    string? clientId = null;

                    try
                    {
                        var oidcAuthenticateResult = await httpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                        if (oidcAuthenticateResult.Succeeded)
                        {
                            user = oidcAuthenticateResult.Principal;
                            clientId = user.FindFirstValue(Claims.Audience);
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // OIDC handler will throw when AuthenticateAsync is called if it's not an endpoint it manages
                    }

                    if (user is null)
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
                    .AddAuthenticationSchemes("Delegated")
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
                AuthorizationPolicies.ApiTrnTokenWrite,
                policy => policy
                    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .AddRequirements(new ScopeAuthorizationRequirement(CustomScopes.TrnTokenWrite)));

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
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        builder.Services.AddRazorPages(options =>
        {
            // Every page within the SignIn folder must have AuthenticationState passed to it.
            options.Conventions.AddFolderApplicationModelConvention(
                "/SignIn",
                model =>
                {
                    model.Filters.Add(new ServiceFilterAttribute(typeof(RequireAuthenticationStateFilter)));
                    model.Filters.Add(new NoCachePageFilter());
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

            options.Conventions.AddFolderApplicationModelConvention(
                "/Account",
                model =>
                {
                    model.Filters.Add(new AuthorizeFilter(AuthorizationPolicies.Authenticated));
                });
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

                options.RegisterClaims(
                    CustomClaims.Trn,
                    CustomClaims.TrnLookupStatus,
                    CustomClaims.PreviousUserId,
                    CustomClaims.PreferredName);

                options.RegisterScopes(
                    CustomScopes.All
                        .Append(Scopes.Email)
                        .Append(Scopes.Phone)
                        .Append(Scopes.Profile)
                        .ToArray());
            });

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.All;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        builder.Services
            .AddMvc(options =>
            {
                options.Conventions.Add(new Infrastructure.ApplicationModel.ApiControllerConvention());

                options.ModelBinderProviders.Insert(2, new DateOnlyModelBinderProvider());

                {
                    var simpleTypeModelBinderProvider = options.ModelBinderProviders.OfType<SimpleTypeModelBinderProvider>().Single();
                    options.ReplaceModelBinderProvider<SimpleTypeModelBinderProvider>(new SimpleTypeModelBinderProviderWrapper(simpleTypeModelBinderProvider));
                }
            })
            .AddCookieTempDataProvider();

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
            .AddTransient<RequireAuthenticationStateFilter>()
            .Decorate<ProblemDetailsFactory, Api.Validation.CamelCaseErrorKeysProblemDetailsFactory>();

        builder.Services
            .AddSingleton<IClock, SystemClock>()
            .AddScoped<IAuthenticationStateProvider, DbAuthenticationStateProvider>()
            .AddTransient<IRequestClientIpProvider, RequestClientIpProvider>()
            .AddSingleton<IdentityLinkGenerator, MvcIdentityLinkGenerator>()
            .AddSingleton<IApiClientRepository, ConfigurationApiClientRepository>()
            .AddTransient<ICurrentClientProvider, AuthenticationStateCurrentClientProvider>()
            .AddSingleton<IEventObserver, PublishNotificationsEventObserver>()
            .AddTransient<ClientScopedViewHelper>()
            .AddTransient<IActionContextAccessor, ActionContextAccessor>()
            .AddTransient<UserClaimHelper>()
            .AddSingleton(
                new QueryStringSignatureHelper(
                    builder.Configuration["QueryStringSignatureKey"] ?? throw new Exception("QueryStringSignatureKey missing from configuration.")))
            .AddSignInJourneyStateProvider();

        if (builder.Environment.IsProduction())
        {
            builder.Services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });

            builder.Services.AddRedis(builder.Environment, builder.Configuration, healthCheckBuilder);
            builder.Services.AddRateLimiting(builder.Environment, builder.Configuration);
        }

        builder.Services.AddNotifications(builder.Environment, builder.Configuration);

        builder.Services.AddAuthServerServices(builder.Environment, builder.Configuration, pgConnectionString);

        builder.Services.AddApiServices();

        var app = builder.Build();

        if (builder.Environment.IsProduction() &&
            Environment.GetEnvironmentVariable("WEBSITE_ROLE_INSTANCE_ID") == "0")
        {
            await MigrateDatabase();
        }

        app.UseWhen(
            context => context.Request.Path == new PathString("/.well-known/openid-configuration") && context.Request.Method == HttpMethods.Get,
            a => a.UseCors(options =>
            {
                options.AllowAnyHeader();
                options.AllowAnyMethod();
                options.AllowAnyOrigin();
            }));

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
        }

        app.UseStaticFiles();

        app.UseMiddleware<AuthenticationStateMiddleware>();
        app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/account"), x => x.UseMiddleware<Infrastructure.Middleware.ClientRedirectInfoMiddleware>());

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
                ctx => ctx.Request.Path.StartsWithSegments("/api"),
                a => a.UseRateLimiter());
        }

        if (builder.Environment.IsProduction())
        {
            app.UseWhen(
                ctx => ctx.Request.Path != new PathString("/health") && ctx.Request.Headers.UserAgent != "AlwaysOn",
                a =>
                {
                    a.UseDfeAnalytics();
                    a.UseMiddleware<Infrastructure.Middleware.AppendAuthorizationInfoToAnalyticsEventsMiddleware>();
                });
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
