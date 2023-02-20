namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Register;

[Collection(nameof(DisableParallelization))]  // Depends on mocks
public class PhoneExistsTests : TestBase
{
    public PhoneExistsTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/register/phone-exists");
    }

    [Fact]
    public async Task Get_MissingAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/register/phone-exists");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        var user = await TestData.CreateUser();
        await JourneyHasExpired_RendersErrorPage(c => c.Completed(user), additionalScopes: null, HttpMethod.Get, "/sign-in/register/phone-exists");
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
        await ValidRequest_RendersContent(c => c.Completed(user), "/sign-in/register/phone-exists", additionalScopes: null);
    }
}
