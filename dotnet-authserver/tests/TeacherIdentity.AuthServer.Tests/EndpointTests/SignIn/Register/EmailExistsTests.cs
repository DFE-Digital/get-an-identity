namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Register;

public class EmailExistsTests : TestBase
{
    public EmailExistsTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/register/email-exists");
    }

    [Fact]
    public async Task Get_MissingAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/register/email-exists");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        var user = await TestData.CreateUser();
        await JourneyHasExpired_RendersErrorPage(c => c.Completed(user), additionalScopes: null, HttpMethod.Get, "/sign-in/register/email-exists");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_DoesNotRedirectToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_DoesNotRedirectToPostSignInUrl(additionalScopes: null, HttpMethod.Get, "/sign-in/complete");
    }

    [Fact]
    public async Task Get_ValidRequest_RendersContent()
    {
        var user = await TestData.CreateUser();
        await ValidRequest_RendersContent(c => c.Completed(user), "/sign-in/register/email-exists", additionalScopes: null);
    }
}
