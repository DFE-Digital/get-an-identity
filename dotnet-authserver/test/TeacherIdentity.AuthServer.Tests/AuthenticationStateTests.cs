using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Flurl;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.State;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer.Tests;

public class AuthenticationStateTests
{
    [Fact]
    public void FromClaims()
    {
        // Arrange
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var email = Faker.Internet.Email();
        var emailVerified = true;
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var firstTimeUser = true;
        var haveCompletedTrnLookup = true;
        var trn = "2345678";
        var userId = Guid.NewGuid();

        var claims = new[]
        {
            new Claim(Claims.Subject, userId.ToString()!),
            new Claim(Claims.Email, email),
            new Claim(Claims.EmailVerified, emailVerified.ToString()),
            new Claim(Claims.Name, firstName + " " + lastName),
            new Claim(Claims.GivenName, firstName),
            new Claim(Claims.FamilyName, lastName),
            new Claim(Claims.Birthdate, dateOfBirth.ToString("yyyy-MM-dd")),
            new Claim(CustomClaims.HaveCompletedTrnLookup, haveCompletedTrnLookup.ToString()),
            new Claim(CustomClaims.Trn, trn)
        };

        // Act
        var authenticationState = AuthenticationState.FromClaims(CreateAuthorizationUrl(), claims, firstTimeUser);

        // Assert
        Assert.Equal(dateOfBirth, authenticationState.DateOfBirth);
        Assert.Equal(email, authenticationState.EmailAddress);
        Assert.Equal(emailVerified, authenticationState.EmailAddressVerified);
        Assert.Equal(firstName, authenticationState.FirstName);
        Assert.Equal(lastName, authenticationState.LastName);
        Assert.Equal(firstTimeUser, authenticationState.FirstTimeUser);
        Assert.Equal(haveCompletedTrnLookup, authenticationState.HaveCompletedTrnLookup);
        Assert.Equal(trn, authenticationState.Trn);
        Assert.Equal(userId, authenticationState.UserId);
    }

    [Fact]
    public void GetClaims()
    {
        // Arrange
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var email = Faker.Internet.Email();
        var emailVerified = true;
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var firstTimeUser = true;
        var haveCompletedTrnLookup = true;
        var trn = "2345678";
        var userId = Guid.NewGuid();

        var authenticationState = new AuthenticationState(journeyId: Guid.NewGuid(), CreateAuthorizationUrl())
        {
            DateOfBirth = dateOfBirth,
            EmailAddress = email,
            EmailAddressVerified = emailVerified,
            FirstName = firstName,
            LastName = lastName,
            FirstTimeUser = firstTimeUser,
            HaveCompletedTrnLookup = haveCompletedTrnLookup,
            Trn = trn,
            UserId = userId
        };

        // Act
        var claims = authenticationState.GetClaims();

        // Assert
        var expectedClaims = new[]
        {
            new Claim(Claims.Subject, userId.ToString()!),
            new Claim(Claims.Email, email),
            new Claim(Claims.EmailVerified, emailVerified.ToString()),
            new Claim(Claims.Name, firstName + " " + lastName),
            new Claim(Claims.GivenName, firstName),
            new Claim(Claims.FamilyName, lastName),
            new Claim(Claims.Birthdate, dateOfBirth.ToString("yyyy-MM-dd")),
            new Claim(CustomClaims.HaveCompletedTrnLookup, haveCompletedTrnLookup.ToString()),
            new Claim(CustomClaims.Trn, trn)
        };
        Assert.Equal(expectedClaims, claims, new ClaimTypeAndValueEqualityComparer());
    }

    [Theory]
    [MemberData(nameof(GetNextHopUrlData))]
    public void GetNextHopUrl(AuthenticationState authenticationState, string expectedResult)
    {
        // Arrange
        var urlHelper = A.Fake<IUrlHelper>();

        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set(new AuthenticationStateFeature(authenticationState));

        var actionContext = new ActionContext()
        {
            ActionDescriptor = new ActionDescriptor(),
            HttpContext = httpContext,
            RouteData = new RouteData()
        };

        A.CallTo(() => urlHelper.ActionContext).Returns(actionContext);

        void ConfigureMockForPage(string pageName, string returns)
        {
            A.CallTo(() => urlHelper.RouteUrl(A<UrlRouteContext>.That.Matches(ctx => (string)((RouteValueDictionary)ctx.Values!)["page"]! == pageName)))
                .Returns(returns);
        }

        ConfigureMockForPage("/SignIn/Email", "/sign-in/email");
        ConfigureMockForPage("/SignIn/EmailConfirmation", "/sign-in/email-confirmation");
        ConfigureMockForPage("/SignIn/Trn", "/sign-in/trn");
        ConfigureMockForPage("/SignIn/TrnCallback", "/sign-in/trn-callback");

        // Act
        var result = authenticationState.GetNextHopUrl(urlHelper);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void Populate()
    {
        // Arrange
        var created = DateTime.UtcNow;
        var user = new User()
        {
            DateOfBirth = new DateOnly(2001, 4, 1),
            EmailAddress = Faker.Internet.Email(),
            FirstName = Faker.Name.First(),
            LastName = Faker.Name.Last(),
            UserId = Guid.NewGuid(),
            Created = created,
            CompletedTrnLookup = created
        };
        var trn = "1234567";
        var firstTimeUser = true;

        var authenticationState = new AuthenticationState(Guid.NewGuid(), "/");

        // Act
        authenticationState.Populate(user, firstTimeUser, trn);

        // Assert
        Assert.Equal(user.DateOfBirth, authenticationState.DateOfBirth);
        Assert.Equal(user.EmailAddress, authenticationState.EmailAddress);
        Assert.Equal(user.FirstName, authenticationState.FirstName);
        Assert.Equal(user.LastName, authenticationState.LastName);
        Assert.Equal(user.UserId, authenticationState.UserId);
        Assert.True(authenticationState.EmailAddressVerified);
        Assert.Equal(firstTimeUser, authenticationState.FirstTimeUser);
        Assert.True(authenticationState.HaveCompletedTrnLookup);
        Assert.Equal(trn, authenticationState.Trn);
    }

    public static TheoryData<AuthenticationState, string> GetNextHopUrlData
    {
        get
        {
            var journeyId = Guid.NewGuid();
            var authorizationUrl = "/connect/authorize?client_id=client&grant_type=code&scope=email%20profile%20trn&redirect_uri=%2F";

            return new TheoryData<AuthenticationState, string>()
            {
                // No email address
                {
                    new AuthenticationState(journeyId, authorizationUrl)
                    {
                        EmailAddress = null
                    },
                    $"/sign-in/email?asid={journeyId}"
                },

                // Not confirmed email address
                {
                    new AuthenticationState(journeyId, authorizationUrl)
                    {
                        EmailAddress = "john.doe@example.com"
                    },
                    $"/sign-in/email-confirmation?asid={journeyId}"
                },

                // Unknown user, not redirected to Find yet
                {
                    new AuthenticationState(journeyId, authorizationUrl)
                    {
                        EmailAddress = "john.doe@example.com",
                        EmailAddressVerified = true,
                        FirstTimeUser = true,
                        HaveCompletedTrnLookup = false
                    },
                    $"/sign-in/trn?asid={journeyId}"
                },

                // Unknown user, has completed Find journey
                {
                    new AuthenticationState(journeyId, authorizationUrl)
                    {
                        EmailAddress = "john.doe@example.com",
                        EmailAddressVerified = true,
                        FirstTimeUser = true,
                        HaveCompletedTrnLookup = true,
                        UserId = Guid.NewGuid()
                    },
                    authorizationUrl.SetQueryParam("asid", journeyId)
                },

                // Known user, confirmed
                {
                    new AuthenticationState(journeyId, authorizationUrl)
                    {
                        EmailAddress = "john.doe@example.com",
                        EmailAddressVerified = true,
                        UserId = Guid.NewGuid(),
                        Trn = "1234567",
                        FirstTimeUser = false
                    },
                    authorizationUrl.SetQueryParam("asid", journeyId)
                },
            };
        }
    }

    private static string CreateAuthorizationUrl()
    {
        var codeChallenge = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes("12345")));

        var client = TestClients.Client1;
        var authorizationUrl = $"/connect/authorize" +
            $"?client_id={client.ClientId}" +
            $"&response_type=code" +
            $"&scope=email%20profile%20trn" +
            $"&redirect_uri={Uri.EscapeDataString(client.RedirectUris.First().ToString())}" +
            $"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
            $"&code_challenge_method=S256" +
            $"&response_mode=form_post";

        return authorizationUrl;
    }

    private class ClaimTypeAndValueEqualityComparer : IEqualityComparer<Claim>
    {
        public bool Equals(Claim? x, Claim? y)
        {
            return x is null && y is null ||
                (x is not null && y is not null && x.Type == y.Type && x.Value == y.Value);
        }

        public int GetHashCode([DisallowNull] Claim obj) => HashCode.Combine(obj.Type, obj.Value);
    }
}
