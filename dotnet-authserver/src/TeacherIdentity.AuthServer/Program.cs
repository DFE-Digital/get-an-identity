using System.Net.Http.Headers;
using System.Security.Cryptography;
using AspNetCoreRateLimit;
using AspNetCoreRateLimit.Redis;
using FluentValidation;
using GovUk.Frontend.AspNetCore;
using Hangfire;
using Hangfire.PostgreSql;
using Joonasw.AspNetCore.SecurityHeaders;
using MediatR;
using Microsoft.AspNetCore.Authentication.Cookies;
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
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Notify.Client;
using OpenIddict.Validation.AspNetCore;
using Prometheus;
using Sentry.AspNetCore;
using Serilog;
using StackExchange.Redis;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;
using TeacherIdentity.AuthServer.Api.Validation;
using TeacherIdentity.AuthServer.Configuration;
using TeacherIdentity.AuthServer.EventProcessing;
using TeacherIdentity.AuthServer.Infrastructure;
using TeacherIdentity.AuthServer.Infrastructure.ApplicationModel;
using TeacherIdentity.AuthServer.Infrastructure.Filters;
using TeacherIdentity.AuthServer.Infrastructure.Json;
using TeacherIdentity.AuthServer.Infrastructure.Middleware;
using TeacherIdentity.AuthServer.Infrastructure.Security;
using TeacherIdentity.AuthServer.Infrastructure.Swagger;
using TeacherIdentity.AuthServer.Jobs;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Notifications;
using TeacherIdentity.AuthServer.Notifications.WebHooks;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services;
using TeacherIdentity.AuthServer.Services.BackgroundJobs;
using TeacherIdentity.AuthServer.Services.DqtApi;
using TeacherIdentity.AuthServer.Services.Email;
using TeacherIdentity.AuthServer.Services.EmailVerification;
using TeacherIdentity.AuthServer.Services.TrnLookup;
using TeacherIdentity.AuthServer.State;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace TeacherIdentity.AuthServer;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog((ctx, config) => config.ReadFrom.Configuration(ctx.Configuration));

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.AddServerHeader = false;
        });

        if (builder.Environment.IsProduction())
        {
            builder.WebHost.UseSentry();

            builder.Services.AddDataProtection()
                .PersistKeysToAzureBlobStorage(
                    connectionString: builder.Configuration.GetConnectionString("DataProtectionBlobStorage"),
                    containerName: builder.Configuration["DataProtectionKeysContainerName"],
                    blobName: "keys");

            var redisConfiguration = ConfigurationOptions.Parse(builder.Configuration.GetConnectionString("Redis"));

            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.ConfigurationOptions = redisConfiguration;
            });

            builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConfiguration));

            ConfigureRateLimitServices();
        }

        MetricLabels.ConfigureLabels(builder.Configuration);

        builder.Services.AddAntiforgery(options =>
        {
            options.Cookie.Name = "tis-antiforgery";
            options.SuppressXFrameOptionsHeader = true;
        });

        builder.Services.AddGovUkFrontend(options => options.AddImportsToHtml = false);

        if (builder.Environment.IsProduction())
        {
            builder.Services.AddOptions<DqtApiOptions>()
                .Bind(builder.Configuration.GetSection("DqtApi"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            builder.Services
                .AddSingleton<IDqtApiClient, DqtApiClient>()
                .AddHttpClient<IDqtApiClient, DqtApiClient>((sp, httpClient) =>
                {
                    var options = sp.GetRequiredService<IOptions<DqtApiOptions>>();
                    httpClient.BaseAddress = new Uri(options.Value.BaseAddress);
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.ApiKey);
                });
        }
        else
        {
            builder.Services.AddSingleton<IDqtApiClient, FakeDqtApiClient>();
        }

        builder.Services.AddAuthentication()
            .AddCookie(options =>
            {
                options.Cookie.Name = "tis-auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;

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
                        string returnUrl = QueryHelpers.ParseQuery(new Uri(ctx.RedirectUri).Query)["returnUrl"];

                        authenticationState = new AuthenticationState(
                            journeyId: Guid.NewGuid(),
                            userRequirements,
                            postSignInUrl: returnUrl);

                        ctx.HttpContext.Features.Set(new AuthenticationStateFeature(authenticationState));
                    }

                    var linkGenerator = ctx.HttpContext.RequestServices.GetRequiredService<IIdentityLinkGenerator>();
                    ctx.Response.Redirect(authenticationState.GetNextHopUrl(linkGenerator));

                    return Task.CompletedTask;
                };
            })
            .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.AuthenticationScheme, _ => { });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AuthorizationPolicies.GetAnIdentityAdmin,
                policy => policy
                    .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .RequireRole(StaffRoles.GetAnIdentityAdmin));

            options.AddPolicy(
                AuthorizationPolicies.TrnLookupApi,
                policy => policy
                    .AddAuthenticationSchemes(ApiKeyAuthenticationHandler.AuthenticationScheme)
                    .RequireAuthenticatedUser());

            options.AddPolicy(
                AuthorizationPolicies.GetAnIdentitySupportApi,
                policy => policy
                    .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .AddRequirements(new ScopeAuthorizationRequirement(CustomScopes.GetAnIdentitySupport)));
        });

        builder.Services.AddSingleton<IAuthorizationHandler, RequireScopeAuthorizationHandler>();

        builder.Services.AddControllersWithViews()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new DateOnlyConverter());
            });

        builder.Services.AddSingleton<RequireAuthenticationStateFilter>();

        builder.Services.AddRazorPages(options =>
        {
            // Every page within the SignIn folder must have AuthenticationState passed to it.
            options.Conventions.AddFolderApplicationModelConvention(
                "/SignIn",
                model =>
                {
                    model.Filters.Add(new RequireAuthenticationStateFilterFactory());
                    model.Filters.Add(new NoCachePageFilter());
                    model.Filters.Add(new RedirectToCompletePageFilter());
                });

            options.Conventions.AddFolderApplicationModelConvention(
                "/Admin",
                model =>
                {
                    model.Filters.Add(new AuthorizeFilter(AuthorizationPolicies.GetAnIdentityAdmin));
                });
        });

        builder.Services.AddSession(options =>
        {
            options.Cookie.Name = "tis-session";
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

        var pgConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

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
            })
            .AddServer(options =>
            {
                options.AddEventHandler<ApplyAuthorizationResponseContext>(
                    builder => builder.UseSingletonHandler<ProcessAuthorizationResponseHandler>());

                options
                    .SetAuthorizationEndpointUris("/connect/authorize")
                    .SetLogoutEndpointUris("/connect/signout")
                    .SetTokenEndpointUris("/connect/token")
                    .SetUserinfoEndpointUris("/connect/userinfo");

                options
                    .AllowAuthorizationCodeFlow()
                    .AllowClientCredentialsFlow();

                if (builder.Environment.IsUnitTests())
                {
                    options.AllowPasswordFlow();
                }

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
                    var encryptionKeysConfig = builder.Configuration.GetSection("EncryptionKeys").Get<string[]>();
                    var signingKeysConfig = builder.Configuration.GetSection("SigningKeys").Get<string[]>();

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
                options.RegisterScopes(Scopes.Email, Scopes.Profile, CustomScopes.Trn, CustomScopes.GetAnIdentityAdmin, CustomScopes.GetAnIdentitySupport);
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.All;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        builder.Services.AddOptions<FindALostTrnIntegrationOptions>()
            .Bind(builder.Configuration.GetSection("FindALostTrnIntegration"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddTransient<FindALostTrnIntegrationHelper>();

        builder.Services.AddSingleton<IAuthenticationStateProvider, SessionAuthenticationStateProvider>();

        if (builder.Environment.IsProduction())
        {
            builder.Services.Configure<SentryAspNetCoreOptions>(options =>
            {
                var hostingEnvironmentName = builder.Configuration["EnvironmentName"];
                if (!string.IsNullOrEmpty(hostingEnvironmentName))
                {
                    options.Environment = hostingEnvironmentName;
                }

                var gitSha = builder.Configuration["GitSha"];
                if (!string.IsNullOrEmpty(gitSha))
                {
                    options.Release = gitSha;
                }
            });
        }

        builder.Services.AddCsp(nonceByteAmount: 32);

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddSingleton<IDistributedCache, DevelopmentFileDistributedCache>();
        }

        if (!builder.Environment.IsUnitTests())
        {
            builder.Services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(pgConnectionString));

            builder.Services.AddHangfireServer();

            builder.Services.AddSingleton<IHostedService, RegisterRecurringJobsHostedService>();
        }

        if (builder.Environment.IsProduction())
        {
            builder.Services.AddSingleton(new NotificationClient(builder.Configuration["NotifyApiKey"]));
            builder.Services.AddSingleton<IEmailSender, NotifyEmailSender>();
            builder.Services.AddSingleton<IRateLimitStore, RateLimitStore>();

            // Use Hangfire for scheduling emails in the background (so we get retries etc.).
            // As the implementation needs to be able to resolve itself we need two service registrations here;
            // one for the interface (that decorates the 'base' notify implementation) and another for the concrete type.
            builder.Services.Decorate<IEmailSender, BackgroundEmailSender>();
            builder.Services.AddSingleton<BackgroundEmailSender>(sp => (BackgroundEmailSender)sp.GetRequiredService<IEmailSender>());
        }
        else
        {
            builder.Services.AddSingleton<IEmailSender, NoopEmailSender>();
            builder.Services.AddSingleton<IRateLimitStore, NoopRateLimitStore>();
        }

        builder.Services.AddTransient<IRequestClientIpProvider, RequestClientIpProvider>();

        builder.Services.AddSingleton<IClock, SystemClock>();

        builder.Services.AddTransient<IEmailVerificationService, EmailVerificationService>();

        builder.Services.AddSingleton<IApiClientRepository, ConfigurationApiClientRepository>();

        builder.Services.AddSingleton<IActionDescriptorProvider, RemoveStubFindEndpointsActionDescriptorProvider>();

        builder.Services.AddOptions<EmailVerificationOptions>()
            .Bind(builder.Configuration.GetSection("EmailVerification"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddOptions<RateLimitStoreOptions>()
            .Bind(builder.Configuration.GetSection("EmailVerificationRateLimit"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddApplicationInsightsTelemetry();

        builder.Services.AddTransient<ICurrentClientProvider, AuthenticationStateCurrentClientProvider>();

        builder.Services.AddSingleton<IIdentityLinkGenerator, IdentityLinkGenerator>();

        if (builder.Environment.IsProduction())
        {
            builder.Services.AddSingleton<IBackgroundJobScheduler, HangfireBackgroundJobScheduler>();
        }
        else
        {
            builder.Services.AddSingleton<IBackgroundJobScheduler, ExecuteImmediatelyJobScheduler>();
        }

        builder.Services.AddMvc(options =>
        {
            options.Conventions.Add(new ApiControllerConvention());
        });

        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo() { Title = "Get an identity to access Teacher Services API", Version = "v1" });

            c.DocInclusionPredicate((docName, api) => docName.Equals(api.GroupName, StringComparison.OrdinalIgnoreCase));
            c.EnableAnnotations();
            c.ExampleFilters();
            c.OperationFilter<ResponseContentTypeOperationFilter>();
            c.OperationFilter<RateLimitOperationFilter>();
        });

        builder.Services.AddSwaggerExamplesFromAssemblyOf<Program>();
        builder.Services.AddTransient<ISerializerDataContractResolver>(sp =>
        {
            var serializerOptions = sp.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
            return new Infrastructure.Swagger.JsonSerializerDataContractResolver(serializerOptions);
        });

        builder.Services.AddSingleton<UserClaimHelper>();

        builder.Services.AddSingleton<Redactor>();

        builder.Services.AddSingleton<PinValidator>();

        builder.Services.AddHsts(options =>
        {
            options.Preload = true;
            options.IncludeSubDomains = true;
            options.MaxAge = TimeSpan.FromDays(365);
        });

        builder.Services.AddSingleton<IEventObserver, PublishNotificationsEventObserver>();

        if (!builder.Environment.IsUnitTests())
        {
            builder.Services.AddSingleton<INotificationPublisher, WebHookNotificationPublisher>();
            builder.Services.AddOptions<WebHookNotificationOptions>();
            builder.Services.AddSingleton<IWebHookNotificationSender, WebHookNotificationSender>();
        }

        builder.Services.AddMediatR(typeof(Program));

        builder.Services.AddValidatorsFromAssemblyContaining(typeof(Program));

        builder.Services.Decorate<Microsoft.AspNetCore.Mvc.Infrastructure.ProblemDetailsFactory, CamelCaseErrorKeysProblemDetailsFactory>();

        var app = builder.Build();

        if (builder.Environment.IsProduction() &&
            Environment.GetEnvironmentVariable("WEBSITE_ROLE_INSTANCE_ID") == "0")
        {
            await MigrateDatabase();
            await ConfigureClients();
        }

        app.UseSerilogRequestLogging();

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

        // Add security headers middleware but exclude the endpoints managed by OpenIddict
        app.UseWhen(
            ctx => !ctx.Request.Path.StartsWithSegments(new PathString("/connect")),
            a =>
            {
                a.UseMiddleware<AppendSecurityResponseHeadersMiddleware>();

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

        app.UseRouting();

        if (builder.Environment.IsProduction())
        {
            app.UseSentryTracing();
        }

        app.UseHttpMetrics();

        app.UseHealthChecks("/status");

        app.UseAuthentication();

        app.UseAuthorization();

        if (builder.Environment.IsProduction())
        {
            app.UseWhen(
                ctx => ctx.Request.Path.StartsWithSegments("/api") && !ctx.Request.Path.StartsWithSegments("/api/find-trn"),
                a => a.UseMiddleware<RateLimitMiddleware>());
        }

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapRazorPages();
            endpoints.MapMetrics();

            endpoints.MapGet("/health", async context =>
            {
                await context.Response.WriteAsync("OK");
            });

            var gitSha = builder.Configuration["GitSha"];
            if (!string.IsNullOrEmpty(gitSha))
            {
                endpoints.MapGet("/_sha", async context =>
                {
                    await context.Response.WriteAsync(gitSha);
                });
            }

            if (!builder.Environment.IsUnitTests())
            {
                endpoints.MapHangfireDashboardWithAuthorizationPolicy(authorizationPolicyName: AuthorizationPolicies.GetAnIdentityAdmin, "/_hangfire");
            }
        });

        app.UseSwagger(options =>
        {
            options.PreSerializeFilters.Add((_, request) =>
            {
                request.HttpContext.Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
            });
        });

        app.Run();

        async Task ConfigureClients()
        {
            var clients = builder.Configuration.GetSection("Clients").Get<ClientConfiguration[]>() ?? Array.Empty<ClientConfiguration>();
            var helper = new ClientConfigurationHelper(app.Services);
            await helper.UpsertClients(clients);
        }

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
