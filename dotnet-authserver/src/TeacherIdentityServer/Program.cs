using GovUk.Frontend.AspNetCore;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using Prometheus;
using TeacherIdentityServer;
using TeacherIdentityServer.Configuration;
using TeacherIdentityServer.Models;
using TeacherIdentityServer.State;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
});

builder.Configuration
    .AddJsonEnvironmentVariable("VCAP_SERVICES", configurationKeyPrefix: "VCAP_SERVICES")
    .AddJsonEnvironmentVariable("VCAP_APPLICATION", configurationKeyPrefix: "VCAP_APPLICATION");

MetricLabels.ConfigureLabels(builder.Configuration);

builder.Services.AddGovUkFrontend(options => options.AddImportsToHtml = false);

builder.Services.AddAuthentication()
    .AddCookie(options =>
    {
        options.Cookie.Name = "gai-auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(5);

        options.Events.OnRedirectToLogin = ctx =>
        {
            var authStateId = Guid.NewGuid();
            var authState = new AuthenticationState(authStateId, ctx.Properties.RedirectUri!);
            ctx.HttpContext.Features.Set(new AuthenticationStateFeature(authState));

            // TODO Use IUrlHelper here

            ctx.Response.Redirect("/email" + QueryString.Create(AuthenticationStateMiddleware.IdQueryParameterName, authStateId.ToString()));

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
    options.Cookie.Name = "gai-session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddDbContext<TeacherIdentityServerDbContext>(options =>
{
    TeacherIdentityServerDbContext.ConfigureOptions(options, builder.Configuration.GetConnectionString("DefaultConnection"));
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

        options
            .AddDevelopmentEncryptionCertificate()
            .AddDevelopmentSigningCertificate();

        options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableLogoutEndpointPassthrough()
            .EnableTokenEndpointPassthrough()
            .EnableUserinfoEndpointPassthrough()
            .EnableStatusCodePagesIntegration();

        options.DisableAccessTokenEncryption();

        options.RegisterClaims(CustomClaims.QualifiedTeacherTrn);
        options.RegisterScopes(CustomScopes.QualifiedTeacher);
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseStatusCodePagesWithReExecute("/error", "?code={0}");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSession();

app.UseMiddleware<AuthenticationStateMiddleware>();

app.UseRouting();

app.UseHttpMetrics();

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
});

using (var scope = app.Services.CreateAsyncScope())
{
    var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

    var dbContext = scope.ServiceProvider.GetRequiredService<TeacherIdentityServerDbContext>();
    await dbContext.Database.EnsureCreatedAsync();

    // Web client
    {
        var client = await manager.FindByClientIdAsync("client");
        if (client is not null)
        {
            await manager.DeleteAsync(client);
        }

        await manager.CreateAsync(new OpenIddictApplicationDescriptor()
        {
            ClientId = "client",
            ClientSecret = "super-secret",
            //ConsentType = ConsentTypes.Explicit,
            ConsentType = ConsentTypes.Implicit,
            DisplayName = "Sample Client app",
            RedirectUris =
            {
                new Uri("https://localhost:7261/oidc/callback")
            },
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.Implicit,
                Permissions.ResponseTypes.Code,
                Permissions.ResponseTypes.IdToken,
                Permissions.ResponseTypes.CodeIdToken,
                Permissions.Scopes.Email,
                Permissions.Scopes.Profile,
                $"scp:{CustomScopes.QualifiedTeacher}"
            },
            Requirements =
            {
                //Requirements.Features.ProofKeyForCodeExchange
            }
        });
    }

    // Console app client
    {
        var client = await manager.FindByClientIdAsync("client2");
        if (client is not null)
        {
            await manager.DeleteAsync(client);
        }

        await manager.CreateAsync(new OpenIddictApplicationDescriptor()
        {
            ClientId = "client2",
            ClientSecret = "another-big-secret",
            //ConsentType = ConsentTypes.Explicit,
            ConsentType = ConsentTypes.Implicit,
            DisplayName = "Sample Client app 2",
            Permissions =
            {
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.ClientCredentials,
                Permissions.ResponseTypes.Token,
                //Permissions.Scopes.Email,
                //Permissions.Scopes.Profile,
            },
            Requirements =
            {
                //Requirements.Features.ProofKeyForCodeExchange
            }
        });
    }
}

app.Run();
