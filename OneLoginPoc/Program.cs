using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.IdentityModel.Tokens;
using OneLoginPoc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(defaultScheme: OneLoginAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddOneLogin(options =>
    {
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

        var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText("private_key.pem"));
        options.ClientAuthenticationCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);

        //options.VectorsOfTrust = "[\"Cl.Cm.P2\"]";
        options.VectorsOfTrust = "[\"Cl\"]";

        options.MetadataAddress = "https://oidc.integration.account.gov.uk/.well-known/openid-configuration";
        options.ClientAssertionJwtAudience = "https://oidc.integration.account.gov.uk/token";

        options.ClientId = "???";
    });

builder.Services.AddRazorPages().AddMvcOptions(options =>
{
    options.Filters.Add(new AuthorizeFilter());
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

//app.UseAuthentication();

app.UseAuthorization();

app.MapRazorPages();

app.Run();