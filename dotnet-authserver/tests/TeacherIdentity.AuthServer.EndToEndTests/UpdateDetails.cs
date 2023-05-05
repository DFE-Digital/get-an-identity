using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.EndToEndTests;

public class UpdateDetails : IClassFixture<HostFixture>
{
    private readonly HostFixture _hostFixture;

    public UpdateDetails(HostFixture hostFixture)
    {
        _hostFixture = hostFixture;
    }

    [Fact]
    public async Task UpdateNameWithinOAuthFlow()
    {
        var user = await _hostFixture.TestData.CreateUser(hasTrn: true);

        var newFirstName = Faker.Name.First();
        var newLastName = Faker.Name.Last();

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.StartOAuthJourney(additionalScope: CustomScopes.Trn, trnRequirement: TrnRequirementType.Legacy);

        await page.SubmitEmailPage(user.EmailAddress);

        await page.SubmitEmailConfirmationPage();

        // Confirm your details page
        await page.WaitForUrlPathAsync("/sign-in/complete");
        await page.ClickChangeLinkForSummaryListRowWithKey("Name");

        // Update your name page
        await page.WaitForSelectorAsync("h1:text-is('Update your name')");
        await page.FillAsync("text=First name", newFirstName);
        await page.FillAsync("text=Last name", newLastName);
        await page.ClickContinueButton();

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(user.EmailAddress, user.Trn, newFirstName, newLastName);
    }

    [Fact]
    public async Task UpdateEmailWithinOAuthFlow()
    {
        var user = await _hostFixture.TestData.CreateUser(hasTrn: true);

        var newEmail = Faker.Internet.Email();

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.StartOAuthJourney(additionalScope: CustomScopes.Trn, trnRequirement: TrnRequirementType.Legacy);

        await page.SubmitEmailPage(user.EmailAddress);

        await page.SubmitEmailConfirmationPage();

        // Confirm your details page
        await page.WaitForUrlPathAsync("/sign-in/complete");
        await page.ClickChangeLinkForSummaryListRowWithKey("Email address");

        // Update your email page
        await page.WaitForSelectorAsync("h1:text-is('Change your email address')");
        await page.FillAsync("text=Enter your new email address", newEmail);
        await page.ClickContinueButton();

        // Confirm your email address page

        await page.WaitForSelectorAsync("h1:text-is('Confirm your email address')");
        await page.FillAsync("text=Enter your code", HostFixture.UserVerificationPin);
        await page.ClickContinueButton();

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(newEmail, user.Trn, user.FirstName, user.LastName);
    }
}
