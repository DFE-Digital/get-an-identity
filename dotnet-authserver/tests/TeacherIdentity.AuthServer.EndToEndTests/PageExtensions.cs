using Microsoft.Playwright;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.EndToEndTests;

public static class PageExtensions
{
    public static Task WaitForUrlPathAsync(this IPage page, string path) =>
        page.WaitForURLAsync(url =>
        {
            var asUri = new Uri(url);
            return asUri.LocalPath == path;
        });

    public static Task ClickChangeLinkForElementWithTestId(this IPage page, string testId) =>
        page.GetByTestId(testId).GetByText("Change").ClickAsync();

    public static Task ClickChangeLinkForSummaryListRowWithKey(this IPage page, string key) =>
        page.Locator($".govuk-summary-list__row:has(> dt:text('{key}'))").GetByText("Change").ClickAsync();

    private static Task ClickLink(this IPage page, string text) =>
        page.Locator("a").GetByText(text).ClickAsync();

    private static Task ClickButton(this IPage page, string text) =>
        page.ClickAsync($".govuk-button:text-is('{text}')");

    public static Task ClickContinueButton(this IPage page) => ClickButton(page, "Continue");

    public static async Task FillDateInput(this IPage page, DateOnly date)
    {
        await page.FillAsync("label:text-is('Day')", date.Day.ToString());
        await page.FillAsync("label:text-is('Month')", date.Month.ToString());
        await page.FillAsync("label:text-is('Year')", date.Year.ToString());
    }

    public static async Task StartOAuthJourney(this IPage page, string? additionalScope = null)
    {
        await page.GotoAsync($"/profile?scope=email+openid+profile{(additionalScope is not null ? "+" + Uri.EscapeDataString(additionalScope) : string.Empty)}");
    }

    public static async Task AssertOnTestClient(this IPage page)
    {
        await page.WaitForURLAsync(url => url.StartsWith(HostFixture.ClientBaseUrl));
    }

    public static Task AssertSignedInOnTestClient(this IPage page, User user) =>
        AssertSignedInOnTestClient(page, user.EmailAddress, user.Trn, user.FirstName, user.LastName);

    public static async Task AssertSignedInOnTestClient(this IPage page, string email, string? trn, string firstName, string lastName)
    {
        await page.AssertOnTestClient();

        var signedInEmail = await page.InnerTextAsync("data-testid=email");
        Assert.Equal(email, signedInEmail);
        Assert.Equal(trn ?? string.Empty, await page.InnerTextAsync("data-testid=trn"));
        Assert.Equal(firstName, await page.InnerTextAsync("data-testid=first-name"));
        Assert.Equal(lastName, await page.InnerTextAsync("data-testid=last-name"));
    }

    public static async Task AssertSignedOutOnTestClient(this IPage page)
    {
        await page.AssertOnTestClient();

        var signedInMarker = await page.GetByTestId("SignedIn").InnerTextAsync();
        Assert.Equal(bool.FalseString, signedInMarker);
    }

    public static async Task SignInFromLandingPage(this IPage page)
    {
        await page.WaitForUrlPathAsync("/sign-in/landing");
        await page.ClickAsync("a:text-is('Sign in')");
    }

    public static async Task SubmitEmailPage(this IPage page, string email)
    {
        await page.WaitForUrlPathAsync("/sign-in/email");
        await page.FillAsync("input[type='email']", email);
        await page.ClickContinueButton();
    }

    public static async Task SubmitEmailConfirmationPage(this IPage page)
    {
        await page.WaitForUrlPathAsync("/sign-in/email-confirmation");
        await page.FillAsync("text=Enter your code", HostFixture.UserVerificationPin);
        await page.ClickContinueButton();
    }

    public static async Task SubmitCompletePageForExistingUser(this IPage page)
    {
        await page.WaitForUrlPathAsync("/sign-in/complete");
        await page.ClickContinueButton();
    }

    public static async Task SubmitCompletePageForNewUserInLegacyTrnJourney(this IPage page)
    {
        await page.WaitForUrlPathAsync("/sign-in/complete");
        Assert.Equal(1, await page.Locator("data-testid=legacy-first-time-user-content").CountAsync());
        await page.ClickContinueButton();
    }

    public static async Task SignOutFromTestClient(this IPage page)
    {
        await page.ClickAsync("a:text-is('Sign out')");

        await page.WaitForURLAsync($"{HostFixture.ClientBaseUrl}/");
    }

    public static async Task GoToAccountPage(this IPage page)
    {
        await page.GotoAsync($"{HostFixture.AuthServerBaseUrl}/account");
    }

    public static async Task AssertOnAccountPage(this IPage page)
    {
        await page.WaitForUrlPathAsync("/account");
    }

    public static async Task SignOutFromAccountPageWithoutClientContext(this IPage page)
    {
        await page.AssertOnAccountPage();

        await page.RunAndWaitForResponseAsync(
            () => page.ClickAsync("a:text-is('Sign out')"),
            resp => resp.Url == $"{HostFixture.AuthServerBaseUrl}/" && resp.Status == 404);
    }

    public static async Task SignOutFromAccountPageWithClientContext(this IPage page)
    {
        await page.AssertOnAccountPage();

        await page.ClickAsync("a:text-is('Sign out')");

        await page.WaitForURLAsync($"{HostFixture.ClientBaseUrl}/");

        await page.AssertOnTestClient();
    }

    public static async Task GoToAccountPageFromTestClient(this IPage page)
    {
        await page.ClickAsync("a:text-is('DfE Identity account')");

        await page.AssertOnAccountPage();
    }

    public static async Task ReturnToTestClientFromAccountPage(this IPage page)
    {
        await page.AssertOnAccountPage();

        await page.ClickAsync("a:text-is('Back to Development test client')");

        await page.AssertOnTestClient();
    }

    public static async Task SubmitTrnBookendPage(this IPage page)
    {
        await page.WaitForUrlPathAsync("/sign-in/trn");
        await page.ClickContinueButton();
    }

    public static async Task SubmitTrnHasTrnPageWithoutTrn(this IPage page)
    {
        await page.WaitForUrlPathAsync("/sign-in/trn/has-trn");
        await page.ClickAsync("label:text-is('No, I need to continue without my TRN')");
        await page.ClickContinueButton();
    }

    public static async Task SubmitTrnHasTrnPageWithTrn(this IPage page, string trn)
    {
        await page.WaitForUrlPathAsync("/sign-in/trn/has-trn");
        await page.ClickAsync("label:text-is('Yes, I know my TRN')");
        await page.FillAsync("text=What is your TRN?", trn);
        await page.ClickContinueButton();
    }

    public static async Task SubmitTrnOfficialNamePage(this IPage page, string officialFirstName, string officialLastName)
    {
        await page.WaitForUrlPathAsync("/sign-in/trn/official-name");
        await page.FillAsync("text=First name", officialFirstName);
        await page.FillAsync("text=Last name", officialLastName);
        await page.ClickAsync("label:text-is('No')");  // Have you ever changed your name?
        await page.ClickContinueButton();
    }

    public static async Task SubmitTrnPreferredNamePageWithoutPreferredName(this IPage page)
    {
        await page.WaitForUrlPathAsync("/sign-in/trn/preferred-name");
        await page.ClickAsync("label:text-is('Yes')");  // Is x y your preferred name?
        await page.ClickContinueButton();
    }

    public static async Task SubmitTrnDateOfBirthPage(this IPage page, DateOnly dateOfBirth)
    {
        await page.WaitForUrlPathAsync("/sign-in/trn/date-of-birth");
        await page.FillDateInput(dateOfBirth);
        await page.ClickContinueButton();
    }

    public static async Task SubmitTrnHasNinoPage(this IPage page, bool hasNino)
    {
        await page.WaitForUrlPathAsync("/sign-in/trn/has-nino");
        await page.ClickAsync($"label:text-is('{(hasNino ? "Yes" : "No")}')");  // Do you have a National Insurance number?
        await page.ClickContinueButton();
    }

    public static async Task SubmitTrnNiNumberPage(this IPage page, string nationalInsuranceNumber)
    {
        await page.WaitForUrlPathAsync("/sign-in/trn/ni-number");
        await page.FillAsync("text='What is your National Insurance number?'", nationalInsuranceNumber);
        await page.ClickContinueButton();
    }

    public static async Task SubmitTrnAwardedQtsPage(this IPage page, bool awardedQts)
    {
        await page.WaitForUrlPathAsync("/sign-in/trn/awarded-qts");
        await page.ClickAsync($"label:text-is('{(awardedQts ? "Yes" : "No")}')");  // Have you been awarded qualified teacher status (QTS)?
        await page.ClickContinueButton();
    }

    public static async Task SubmitTrnIttProviderPageWithIttProvider(this IPage page, string ittProviderName)
    {
        await page.WaitForUrlPathAsync("/sign-in/trn/itt-provider");
        await page.ClickAsync("label:text-is('Yes')");  // Did a university, SCITT or school award your QTS?
        await page.FillAsync("label:text-is('Where did you get your QTS?')", ittProviderName);
        await page.FocusAsync("button:text-is('Continue')");  // Un-focus accessible autocomplete
        await page.ClickContinueButton();
    }

    public static async Task SubmitTrnCheckAnswersPage(this IPage page)
    {
        await page.WaitForUrlPathAsync("/sign-in/trn/check-answers");
        await page.WaitForSelectorAsync("h1:text-is('Check your answers')");
        await page.ClickContinueButton();
    }

    public static async Task SubmitTrnNoMatchPage(this IPage page)
    {
        await page.WaitForUrlPathAsync("/sign-in/trn/no-match");
        await page.ClickAsync("label:text-is('No, use these details, they are correct')");
        await page.ClickContinueButton();
    }

    public static async Task SubmitTrnTrnInUsePage(this IPage page)
    {
        await page.WaitForUrlPathAsync("/sign-in/trn/different-email");
        await page.FillAsync("text=Enter your code", HostFixture.UserVerificationPin);
        await page.ClickContinueButton();
    }

    public static async Task SubmitTrnChooseEmailPage(this IPage page, string email)
    {
        await page.WaitForUrlPathAsync("/sign-in/trn/choose-email");
        await page.ClickAsync($"text={email}");
        await page.ClickContinueButton();
    }

    public static async Task RegisterFromLandingPage(this IPage page)
    {
        await page.WaitForUrlPathAsync("/sign-in/landing");
        await page.ClickAsync("a:text-is('Create an account')");
    }

    public static async Task SubmitRegisterEmailPage(this IPage page, string email)
    {
        await page.WaitForUrlPathAsync("/sign-in/register/email");
        await page.FillAsync("text=Your email address", email);
        await page.ClickContinueButton();
    }

    public static async Task SubmitRegisterEmailConfirmationPage(this IPage page)
    {
        await page.WaitForUrlPathAsync("/sign-in/register/email-confirmation");
        await page.FillAsync("label:text-is('Confirmation code')", HostFixture.UserVerificationPin);
        await page.ClickContinueButton();
    }

    public static async Task SubmitRegisterPhonePage(this IPage page, string mobileNumber)
    {
        await page.WaitForUrlPathAsync("/sign-in/register/phone");
        await page.FillAsync("label:text-is('Mobile number')", mobileNumber);
        await page.ClickContinueButton();
    }

    public static async Task SubmitRegisterPhoneConfirmationPage(this IPage page)
    {
        await page.WaitForUrlPathAsync("/sign-in/register/phone-confirmation");
        await page.FillAsync("label:text-is('Security code')", HostFixture.UserVerificationPin);
        await page.ClickContinueButton();
    }

    public static async Task SubmitRegisterNamePage(this IPage page, string firstName, string lastName)
    {
        await page.WaitForUrlPathAsync("/sign-in/register/name");
        await page.FillAsync("label:text-is('First name')", firstName);
        await page.FillAsync("label:text-is('Last name')", lastName);
        await page.ClickContinueButton();
    }

    public static async Task SubmitDateOfBirthPage(this IPage page, DateOnly dateOfBirth)
    {
        await page.WaitForUrlPathAsync("/sign-in/register/date-of-birth");
        await page.FillDateInput(dateOfBirth);
        await page.ClickContinueButton();
    }

    public static async Task SubmitCheckAnswersPage(this IPage page)
    {
        await page.WaitForUrlPathAsync("/sign-in/register/check-answers");
        await page.ClickContinueButton();
    }

    public static async Task SubmitCompletePageForNewUser(this IPage page)
    {
        await page.WaitForUrlPathAsync("/sign-in/complete");
        Assert.Equal(1, await page.Locator("data-testid=first-time-user-content").CountAsync());
        await page.ClickContinueButton();
    }

    public static async Task SubmitRegisterHasNinoPage(this IPage page, bool hasNino)
    {
        await page.WaitForUrlPathAsync("/sign-in/register/has-nino");
        await page.ClickAsync($"label:text-is('{(hasNino ? "Yes" : "No")}')");  // Do you have a National Insurance number?
        await page.ClickContinueButton();
    }

    public static async Task SubmitRegisterNiNumberPage(this IPage page, string nationalInsuranceNumber)
    {
        await page.WaitForUrlPathAsync("/sign-in/register/ni-number");
        await page.FillAsync("text='What is your National Insurance number?'", nationalInsuranceNumber);
        await page.ClickContinueButton();
    }

    public static async Task SubmitRegisterHasTrnPage(this IPage page, bool hasTrn)
    {
        await page.WaitForUrlPathAsync("/sign-in/register/has-trn");
        await page.ClickAsync($"label:text-is('{(hasTrn ? "Yes" : "No")}')");  // Do you have a National Insurance number?
        await page.ClickContinueButton();
    }

    public static async Task SubmitRegisterTrnPage(this IPage page, string trn)
    {
        await page.WaitForUrlPathAsync("/sign-in/register/trn");
        await page.FillAsync("text=Enter your TRN", trn);
        await page.ClickContinueButton();
    }

    public static async Task SubmitRegisterHasQtsPage(this IPage page, bool hasQts)
    {
        await page.WaitForUrlPathAsync("/sign-in/register/has-qts");
        await page.ClickAsync($"label:text-is('{(hasQts ? "Yes" : "No")}')");  // Have you been awarded qualified teacher status (QTS)?
        await page.ClickContinueButton();
    }

    public static async Task SubmitRegisterIttProviderPageWithIttProvider(this IPage page, string ittProviderName)
    {
        await page.WaitForUrlPathAsync("/sign-in/register/itt-provider");
        await page.ClickAsync("label:text-is('Yes')");  // Did a university, SCITT or school award your QTS?
        await page.FillAsync("label:text-is('Where did you get your QTS?')", ittProviderName);
        await page.FocusAsync("button:text-is('Continue')");  // Un-focus accessible autocomplete
        await page.ClickContinueButton();
    }

    public static async Task SubmitAccountExistsPage(this IPage page, bool isUsersAccount)
    {
        await page.WaitForUrlPathAsync("/sign-in/register/account-exists");
        await page.ClickAsync($"label:text-is('{(isUsersAccount ? "Yes, sign into this account" : "No, this is not my account")}')");  // Do you have a National Insurance number?
        await page.ClickContinueButton();
    }

    public static async Task SubmitExistingAccountEmailConfirmationPage(this IPage page, bool cantAccessEmail = false)
    {
        await page.WaitForUrlPathAsync("/sign-in/register/existing-account-email-confirmation");
        await page.FillAsync("label:text-is('Confirmation code')", HostFixture.UserVerificationPin);
        if (cantAccessEmail)
        {
            await page.ClickAsync("a:text-is('I cannot access this email address')");
        }
        else
        {
            await page.ClickContinueButton();
        }
    }

    public static async Task SubmitExistingAccountPhonePage(this IPage page)
    {
        await page.WaitForUrlPathAsync("/sign-in/register/existing-account-phone");
        await page.ClickButton("Request security code");
    }

    public static async Task SubmitExistingAccountPhoneConfirmationPage(this IPage page)
    {
        await page.WaitForUrlPathAsync("/sign-in/register/existing-account-phone-confirmation");
        await page.FillAsync("label:text-is('Security code')", HostFixture.UserVerificationPin);
        await page.ClickContinueButton();
    }

    public static async Task SignInFromRegisterEmailExistsPage(this IPage page)
    {
        await page.WaitForUrlPathAsync("/sign-in/register/email-exists");
        await page.ClickLink("Sign in");
    }

    public static async Task SignInFromRegisterPhoneExistsPage(this IPage page)
    {
        await page.WaitForUrlPathAsync("/sign-in/register/phone-exists");
        await page.ClickLink("Sign in");
    }
}
