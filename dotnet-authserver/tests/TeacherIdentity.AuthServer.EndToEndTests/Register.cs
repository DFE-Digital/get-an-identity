using TeacherIdentity.AuthServer.Events;

namespace TeacherIdentity.AuthServer.EndToEndTests;

public class Register : IClassFixture<HostFixture>
{
    private readonly HostFixture _hostFixture;

    public Register(HostFixture hostFixture)
    {
        _hostFixture = hostFixture;
        _hostFixture.OnTestStarting();
    }

    [Fact]
    public async Task NewUserCanRegister()
    {
        var email = Faker.Internet.Email();
        var mobileNumber = _hostFixture.TestData.GenerateUniqueMobileNumber();
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.StartOAuthJourney(additionalScope: null);

        await page.RegisterFromLandingPage();

        await page.SubmitRegisterEmailPage(email);

        await page.SubmitRegisterEmailConfirmationPage();

        await page.SubmitRegisterPhonePage(mobileNumber);

        await page.SubmitRegisterPhoneConfirmationPage();

        await page.SubmitRegisterNamePage(firstName, lastName);

        await page.SubmitDateOfBirthPage(dateOfBirth);

        await page.SubmitCompletePageForNewUser();

        await page.AssertSignedInOnTestClient(email, trn: null, firstName, lastName);

        Guid createdUserId = default;

        _hostFixture.EventObserver.AssertEventsSaved(
            e =>
            {
                var userRegisteredEvent = Assert.IsType<UserRegisteredEvent>(e);
                Assert.Equal(_hostFixture.TestClientId, userRegisteredEvent.ClientId);
                Assert.Equal(email, userRegisteredEvent.User.EmailAddress);
                Assert.Equal(mobileNumber, userRegisteredEvent.User.MobileNumber);
                Assert.Equal(firstName, userRegisteredEvent.User.FirstName);
                Assert.Equal(lastName, userRegisteredEvent.User.LastName);
                Assert.Equal(dateOfBirth, userRegisteredEvent.User.DateOfBirth);

                createdUserId = userRegisteredEvent.User.UserId;
            },
            e =>
            {
                var userSignedInEvent = Assert.IsType<UserSignedInEvent>(e);
                Assert.Equal(createdUserId, userSignedInEvent.User.UserId);
            });
    }

    [Fact]
    public async Task UserWithEmailAlreadyExists_SignsInExistingUser()
    {
        var existingUser = await _hostFixture.TestData.CreateUser();

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.StartOAuthJourney(additionalScope: null);

        await page.RegisterFromLandingPage();

        await page.SubmitRegisterEmailPage(existingUser.EmailAddress);

        await page.SubmitRegisterEmailConfirmationPage();

        await page.SignInFromRegisterEmailExistsPage();

        await page.SubmitCompletePageForExistingUser();

        await page.AssertSignedInOnTestClient(existingUser);

        _hostFixture.EventObserver.AssertEventsSaved(
            e => _hostFixture.AssertEventIsUserSignedIn(e, existingUser.UserId, expectOAuthProperties: true));
    }
}
