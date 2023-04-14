using Microsoft.Playwright;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.EndToEndTests;

public partial class SignIn
{
    [Fact]
    public async Task StaffUser_CanSignInSuccessfullyWithEmailAndPin()
    {
        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await SignInExistingStaffUserWithTestClient(page);
    }

    [Fact]
    public async Task StaffUser_CanSignInToAdminPageSuccessfullyWithEmailAndPin()
    {
        var user = await _hostFixture.TestData.CreateUser(userType: UserType.Staff, staffRoles: StaffRoles.All);

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{HostFixture.AuthServerBaseUrl}/admin/staff");

        await page.SubmitEmailPage(user.EmailAddress);

        await page.SubmitEmailConfirmationPage();

        await page.WaitForUrlPathAsync("/admin/staff");

        _hostFixture.EventObserver.AssertEventsSaved(
            e => _hostFixture.AssertEventIsUserSignedIn(e, user.UserId, expectOAuthProperties: false));
    }

    [Fact]
    public async Task StaffUser_MissingPermission_GetsForbiddenError()
    {
        var user = await _hostFixture.TestData.CreateUser(userType: UserType.Staff, staffRoles: Array.Empty<string>());

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.StartOAuthJourney(CustomScopes.UserRead);

        await page.SubmitEmailPage(user.EmailAddress);

        await page.SubmitEmailConfirmationPage();

        await page.WaitForSelectorAsync("h1:text-is('Forbidden')");
    }

    private async Task<Guid> SignInExistingStaffUserWithTestClient(IPage page)
    {
        var user = await _hostFixture.TestData.CreateUser(userType: UserType.Staff, staffRoles: StaffRoles.All);

        await page.StartOAuthJourney(CustomScopes.UserRead);

        await page.SubmitEmailPage(user.EmailAddress);

        await page.SubmitEmailConfirmationPage();

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(user);

        _hostFixture.EventObserver.AssertEventsSaved(
            e => _hostFixture.AssertEventIsUserSignedIn(e, user.UserId));

        return user.UserId;
    }
}
