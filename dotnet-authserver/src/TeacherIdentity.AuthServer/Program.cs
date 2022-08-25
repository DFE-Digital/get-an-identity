using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using GovUk.Frontend.AspNetCore;
using Hangfire;
using Hangfire.PostgreSql;
using Joonasw.AspNetCore.SecurityHeaders;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Notify.Client;
using Prometheus;
using Sentry.AspNetCore;
using Serilog;
using TeacherIdentity.AuthServer.Configuration;
using TeacherIdentity.AuthServer.Infrastructure;
using TeacherIdentity.AuthServer.Infrastructure.ApplicationModel;
using TeacherIdentity.AuthServer.Infrastructure.Json;
using TeacherIdentity.AuthServer.Infrastructure.Middleware;
using TeacherIdentity.AuthServer.Infrastructure.Security;
using TeacherIdentity.AuthServer.Jobs;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
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

            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = builder.Configuration.GetConnectionString("Redis");
            });
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
            })
            .AddBasic(options =>
            {
                options.Realm = "TeacherIdentity.AuthServer";

                options.Events = new idunno.Authentication.Basic.BasicAuthenticationEvents()
                {
                    OnValidateCredentials = context =>
                    {
                        var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                        var username = config["AdminCredentials:Username"];
                        var password = config["AdminCredentials:Password"];

                        if (context.Username == username && context.Password == password)
                        {
                            var claims = new[]
                            {
                                new Claim(
                                    ClaimTypes.NameIdentifier,
                                    context.Username,
                                    ClaimValueTypes.String,
                                    context.Options.ClaimsIssuer),
                                new Claim(
                                    ClaimTypes.Name,
                                    context.Username,
                                    ClaimValueTypes.String,
                                    context.Options.ClaimsIssuer)
                            };

                            context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, context.Scheme.Name));

                            context.Success();
                        }

                        return Task.CompletedTask;
                    }
                };
            })
            .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.AuthenticationScheme, _ => { });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(
                "Hangfire",
                policy => policy
                    .AddAuthenticationSchemes("Basic")
                    .RequireAuthenticatedUser());

            options.AddPolicy(
                "TrnLookup",
                policy => policy
                    .AddAuthenticationSchemes(ApiKeyAuthenticationHandler.AuthenticationScheme)
                    .RequireAuthenticatedUser());
        });

        builder.Services.AddControllersWithViews()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new DateOnlyConverter());
            });

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

        var pgConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

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

                options.AddApplicationStore<TeacherIdentityApplicationStore>();
                options.ReplaceApplicationManager<TeacherIdentityApplicationManager>();
            })
            .AddServer(options =>
            {
                options.AddEventHandler<ApplyAuthorizationResponseContext>(
                    builder => builder.UseSingletonHandler<ProcessAuthorizationResponseHandler>());

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

            // Use Hangfire for scheduling emails in the background (so we get retries etc.).
            // As the implementation needs to be able to resolve itself we need two service registrations here;
            // one for the interface (that decorates the 'base' notify implementation) and another for the concrete type.
            builder.Services.Decorate<IEmailSender, BackgroundEmailSender>();
            builder.Services.AddSingleton<BackgroundEmailSender>(sp => (BackgroundEmailSender)sp.GetRequiredService<IEmailSender>());
        }
        else
        {
            builder.Services.AddSingleton<IEmailSender, NoopEmailSender>();
        }

        builder.Services.AddSingleton<IClock, SystemClock>();

        builder.Services.AddTransient<IEmailVerificationService, EmailVerificationService>();

        builder.Services.AddSingleton<IApiClientRepository, ConfigurationApiClientRepository>();

        builder.Services.AddSingleton<IActionDescriptorProvider, RemoveStubFindEndpointsActionDescriptorProvider>();

        builder.Services.AddOptions<EmailVerificationOptions>()
            .Bind(builder.Configuration.GetSection("EmailVerification"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddApplicationInsightsTelemetry();

        builder.Services.AddTransient<ICurrentClientProvider, AuthenticationStateCurrentClientProvider>();

        var app = builder.Build();

        if (builder.Environment.IsProduction())
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
                    options.ByDefaultAllow
                        .FromSelf();

                    options.AllowScripts
                        .FromSelf()
                        .From("'sha256-j7OoGArf6XW6YY4cAyS3riSSvrJRqpSi1fOF9vQ5SrI='")  // Hash of 'document.form.submit();' from the authorization POST back page in OpenIddict
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
                endpoints.MapHangfireDashboardWithAuthorizationPolicy(authorizationPolicyName: "Hangfire", "/_hangfire");
            }
        });

        app.Run();

        async Task ConfigureClients()
        {
            var clients = builder.Configuration.GetSection("Clients").Get<ClientConfiguration[]>();
            var helper = new ClientConfigurationHelper(app.Services);
            await helper.UpsertClients(clients);
        }

        SecurityKey LoadKey(string configurationKey)
        {
            var rsa = RSA.Create();
            rsa.FromXmlString(builder.Configuration[configurationKey]);

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
