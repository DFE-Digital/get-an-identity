using System.IdentityModel.Tokens.Jwt;
using GovUk.Frontend.AspNetCore;

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddGovUkFrontend();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = "Cookies";
        options.DefaultChallengeScheme = "oidc";
    })
    .AddCookie("Cookies")
    .AddOpenIdConnect("oidc", options =>
    {
        options.Authority = "https://localhost:7236";

        options.ClientId = "client";
        options.ClientSecret = "super-secret";
        options.ResponseType = "code id_token";
        options.CallbackPath = new PathString("/oidc/callback");

        options.Scope.Clear();
        options.Scope.Add("email");
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("custom_scope");

        options.SaveTokens = true;

        // Log the access token to the console for debugging
        options.Events.OnTokenResponseReceived = ctx =>
        {
            Console.WriteLine(ctx.TokenEndpointResponse.AccessToken);
            return Task.CompletedTask;
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
