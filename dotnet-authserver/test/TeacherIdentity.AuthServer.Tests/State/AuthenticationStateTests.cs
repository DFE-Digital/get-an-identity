using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer.Tests.State;

public class AuthenticationStateTests
{
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
        var user = new User()
        {
            DateOfBirth = new DateOnly(2001, 4, 1),
            EmailAddress = Faker.Internet.Email(),
            FirstName = Faker.Name.First(),
            LastName = Faker.Name.Last(),
            UserId = Guid.NewGuid()
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
        Assert.True(authenticationState.HaveCompletedFindALostTrnJourney);
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
                        HaveCompletedFindALostTrnJourney = false
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
                        HaveCompletedFindALostTrnJourney = true,
                        UserId = Guid.NewGuid()
                    },
                    authorizationUrl
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
                    authorizationUrl
                },
            };
        }
    }
}
