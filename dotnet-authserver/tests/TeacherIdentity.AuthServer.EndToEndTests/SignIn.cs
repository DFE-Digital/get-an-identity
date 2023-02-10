using Microsoft.Playwright;
using Moq;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.EndToEndTests;

public class SignIn : IClassFixture<HostFixture>
{
    private readonly HostFixture _hostFixture;

    public SignIn(HostFixture hostFixture)
    {
        _hostFixture = hostFixture;
        _hostFixture.OnTestStarting();
    }

    [Theory]
    [InlineData(CustomScopes.DqtRead)]
#pragma warning disable CS0618 // Type or member is obsolete
    [InlineData(CustomScopes.Trn)]
#pragma warning restore CS0618 // Type or member is obsolete
    public async Task ExistingTeacherUser_CanSignInSuccessfullyWithEmailAndPin(string additionalScope)
    {
        var user = await _hostFixture.TestData.CreateUser(userType: UserType.Default);

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        // Start on the client app and try to access a protected area

        await page.GotoAsync($"/profile?scope=email+openid+profile+{Uri.EscapeDataString(additionalScope)}");

        // Fill in the sign in form (email + PIN)

        await page.FillAsync("text=Enter your email address", user.EmailAddress);
        await page.ClickAsync("button:text-is('Continue')");

        var pin = _hostFixture.CapturedEmailConfirmationPins.Last().Pin;
        await page.FillAsync("text=Enter your code", pin);
        await page.ClickAsync("button:text-is('Continue')");

        // Should now be on the confirmation page

        Assert.Equal(1, await page.Locator("data-testid=known-user-content").CountAsync());
        await page.ClickAsync("button:text-is('Continue')");

        // Should now be back at the client, signed in

        var clientAppHost = new Uri(HostFixture.ClientBaseUrl).Host;
        var pageUrlHost = new Uri(page.Url).Host;
        Assert.Equal(clientAppHost, pageUrlHost);

        var signedInEmail = await page.InnerTextAsync("data-testid=email");
        Assert.Equal(user.Trn ?? string.Empty, await page.InnerTextAsync("data-testid=trn"));
        Assert.Equal(user.EmailAddress, signedInEmail);

        // Check events have been emitted

        _hostFixture.EventObserver.AssertEventsSaved(e => AssertEventIsUserSignedIn(e, user.UserId));
    }

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
        var staffUser = await _hostFixture.TestData.CreateUser(userType: UserType.Staff, staffRoles: StaffRoles.All);

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        // Try to access protected admin area on auth server

        await page.GotoAsync($"{HostFixture.AuthServerBaseUrl}/admin/staff");

        // Fill in the sign in form (email + PIN)

        await page.FillAsync("text=Enter your email address", staffUser.EmailAddress);
        await page.ClickAsync("button:text-is('Continue')");

        var pin = _hostFixture.CapturedEmailConfirmationPins.Last().Pin;
        await page.FillAsync("text=Enter your code", pin);
        await page.ClickAsync("button:text-is('Continue')");

        // Should now be on the originally request URL, /admin/staff

        var clientAppHost = new Uri(HostFixture.ClientBaseUrl).Host;
        Assert.EndsWith("/admin/staff", page.Url);

        // Check events have been emitted

        _hostFixture.EventObserver.AssertEventsSaved(e => AssertEventIsUserSignedIn(e, staffUser.UserId, expectOAuthProperties: false));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task NewTeacherUser_IsRedirectedToFindAndRegistersAnAccountOnCallback(bool hasTrn)
    {
        var email = Faker.Internet.Email();
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var trn = hasTrn ? _hostFixture.TestData.GenerateTrn() : null;
        var dateOfBirth = new DateOnly(1990, 1, 2);

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await SignInAsNewTeacherUserWithTrnScope(page, email, firstName, lastName, trn, dateOfBirth, null, null);
    }

    [Fact]
    public async Task NewTeacherUser_WithFoundTrn_CreatesUserAndCompletesFlow()
    {
        var email = Faker.Internet.Email();
        var officialFirstName = Faker.Name.First();
        var officialLastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var trn = _hostFixture.TestData.GenerateTrn();

        _hostFixture.DqtApiClient
            .Setup(mock => mock.FindTeachers(It.IsAny<FindTeachersRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FindTeachersResponse()
            {
                Results = Array.Empty<FindTeachersResponseResult>()
            });

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        // Start on the client app and try to access a protected area

        await page.GotoAsync($"/profile?scope=email+openid+profile+{Uri.EscapeDataString(CustomScopes.DqtRead)}");

        // Fill in the sign in form (email + PIN)

        await page.FillAsync("text=Enter your email address", email);
        await page.ClickAsync("button:text-is('Continue')");

        var pin = _hostFixture.CapturedEmailConfirmationPins.Last().Pin;
        await page.FillAsync("text=Enter your code", pin);
        await page.ClickAsync("button:text-is('Continue')");

        // Should now be at the first bookend page

        var urlPath = new Uri(page.Url).LocalPath;
        Assert.EndsWith("/trn", urlPath);

        await page.ClickAsync("button:text-is('Continue')");

        // Has TRN page

        await page.ClickAsync("label:text-is('Yes, I know my TRN')");
        await page.FillAsync("text=What is your TRN?", trn);
        await page.ClickAsync("button:text-is('Continue')");

        // Official name page

        await page.FillAsync("text=First name", officialFirstName);
        await page.FillAsync("text=Last name", officialLastName);
        await page.ClickAsync("label:text-is('No')");  // Have you ever changed your name?
        await page.ClickAsync("button:text-is('Continue')");

        // Preferred name page

        await page.ClickAsync("label:text-is('Yes')");  // Is x y your preferred name?
        await page.ClickAsync("button:text-is('Continue')");

        // Simulate DQT API returning result when next page submitted

        _hostFixture.DqtApiClient
            .Setup(mock => mock.FindTeachers(It.IsAny<FindTeachersRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FindTeachersResponse()
            {
                Results = new FindTeachersResponseResult[]
                {
                    new()
                    {
                        DateOfBirth = dateOfBirth,
                        FirstName = officialFirstName,
                        LastName = officialLastName,
                        EmailAddresses = new[] { email },
                        HasActiveSanctions = false,
                        NationalInsuranceNumber = null,
                        Trn = trn,
                        Uid = Guid.NewGuid().ToString()
                    }
                }
            });

        // Date of birth page

        await page.FillAsync("label:text-is('Day')", dateOfBirth.Day.ToString());
        await page.FillAsync("label:text-is('Month')", dateOfBirth.Month.ToString());
        await page.FillAsync("label:text-is('Year')", dateOfBirth.Year.ToString());
        await page.ClickAsync("button:text-is('Continue')");

        // Check answers page

        await page.WaitForSelectorAsync("h1:text-is('Check your answers')");
        await page.ClickAsync("button:text-is('Continue')");

        // Should now be on the confirmation page

        Assert.Equal(1, await page.Locator("data-testid=first-time-user-content").CountAsync());
        await page.ClickAsync("button:text-is('Continue')");

        // Should now be back on the client, signed in

        var clientAppHost = new Uri(HostFixture.ClientBaseUrl).Host;
        var pageUrlHost = new Uri(page.Url).Host;
        Assert.Equal(clientAppHost, pageUrlHost);

        Assert.Equal(officialFirstName, await page.InnerTextAsync("data-testid=first-name"));
        Assert.Equal(officialLastName, await page.InnerTextAsync("data-testid=last-name"));
        Assert.Equal(email, await page.InnerTextAsync("data-testid=email"));
        Assert.Equal(trn ?? string.Empty, await page.InnerTextAsync("data-testid=trn"));
    }

    [Fact]
    public async Task NewTeacherUser_WithoutFoundTrn_CreatesUserAndCompletesFlow()
    {
        var email = Faker.Internet.Email();
        var officialFirstName = Faker.Name.First();
        var officialLastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var nino = Faker.Identification.UkNationalInsuranceNumber();
        var ittProvider = Faker.Company.Name();

        _hostFixture.DqtApiClient
            .Setup(mock => mock.FindTeachers(It.IsAny<FindTeachersRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FindTeachersResponse()
            {
                Results = Array.Empty<FindTeachersResponseResult>()
            });

        _hostFixture.DqtApiClient
            .Setup(mock => mock.GetIttProviders(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetIttProvidersResponse()
            {
                IttProviders = new[]
                {
                    new IttProvider()
                    {
                        ProviderName = "Provider 1",
                        Ukprn = "123"
                    },
                    new IttProvider()
                    {
                        ProviderName = ittProvider,
                        Ukprn = "234"
                    }
                }
            });

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        // Start on the client app and try to access a protected area

        await page.GotoAsync($"/profile?scope=email+openid+profile+{Uri.EscapeDataString(CustomScopes.DqtRead)}");

        // Fill in the sign in form (email + PIN)

        await page.FillAsync("text=Enter your email address", email);
        await page.ClickAsync("button:text-is('Continue')");

        var pin = _hostFixture.CapturedEmailConfirmationPins.Last().Pin;
        await page.FillAsync("text=Enter your code", pin);
        await page.ClickAsync("button:text-is('Continue')");

        // Should now be at the first bookend page

        var urlPath = new Uri(page.Url).LocalPath;
        Assert.EndsWith("/trn", urlPath);

        await page.ClickAsync("button:text-is('Continue')");

        // Has TRN page

        await page.ClickAsync("label:text-is('No, I need to continue without my TRN')");
        await page.ClickAsync("button:text-is('Continue')");

        // Official name page

        await page.FillAsync("text=First name", officialFirstName);
        await page.FillAsync("text=Last name", officialLastName);
        await page.ClickAsync("label:text-is('No')");  // Have you ever changed your name?
        await page.ClickAsync("button:text-is('Continue')");

        // Preferred name page

        await page.ClickAsync("label:text-is('Yes')");  // Is x y your preferred name?
        await page.ClickAsync("button:text-is('Continue')");

        // Date of birth page

        await page.FillAsync("label:text-is('Day')", dateOfBirth.Day.ToString());
        await page.FillAsync("label:text-is('Month')", dateOfBirth.Month.ToString());
        await page.FillAsync("label:text-is('Year')", dateOfBirth.Year.ToString());
        await page.ClickAsync("button:text-is('Continue')");

        // Has NI number page

        await page.ClickAsync("label:text-is('Yes')");  // Do you have a National Insurance number?
        await page.ClickAsync("button:text-is('Continue')");

        // NI number page

        await page.FillAsync("text='What is your National Insurance number?'", nino);
        await page.ClickAsync("button:text-is('Continue')");

        // Awarded QTS page

        await page.ClickAsync("label:text-is('Yes')");  // Have you been awarded qualified teacher status (QTS)?
        await page.ClickAsync("button:text-is('Continue')");

        // ITT Provider page

        await page.ClickAsync("label:text-is('Yes')");  // Did a university, SCITT or school award your QTS?
        await page.FillAsync("label:text-is('Where did you get your QTS?')", ittProvider);
        await page.FocusAsync("button:text-is('Continue')");  // Un-focus accessible autocomplete
        await page.ClickAsync("button:text-is('Continue')");

        // Check answers page

        await page.WaitForSelectorAsync("h1:text-is('Check your answers')");
        await page.ClickAsync("button:text-is('Continue')");

        // No match page

        await page.ClickAsync("label:text-is('No, use these details, they are correct')");
        await page.ClickAsync("button:text-is('Continue')");

        // Should now be on the confirmation page

        Assert.Equal(1, await page.Locator("data-testid=first-time-user-content").CountAsync());
        await page.ClickAsync("button:text-is('Continue')");

        // Should now be back on the client, signed in

        var clientAppHost = new Uri(HostFixture.ClientBaseUrl).Host;
        var pageUrlHost = new Uri(page.Url).Host;
        Assert.Equal(clientAppHost, pageUrlHost);

        Assert.Equal(officialFirstName, await page.InnerTextAsync("data-testid=first-name"));
        Assert.Equal(officialLastName, await page.InnerTextAsync("data-testid=last-name"));
        Assert.Equal(email, await page.InnerTextAsync("data-testid=email"));
        Assert.Equal(string.Empty, await page.InnerTextAsync("data-testid=trn"));
    }

    [Fact]
    public async Task ExistingTeacherUser_SignsInWithinSameSessionTheyRegisteredWith_SkipsEmailAndPinAndShowsCorrectConfirmationPage()
    {
        var email = Faker.Internet.Email();
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var trn = (string?)null;
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await SignInAsNewTeacherUserWithTrnScope(page, email, firstName, lastName, trn, dateOfBirth, null, null);

        await ClearCookiesForTestClient();

        // Start on the client app and try to access a protected area

        await page.GotoAsync("/profile?scope=email+openid+profile+trn");

        // Should have jumped straight to confirmation page as the auth server knows who we are

        Assert.Equal(1, await page.Locator("data-testid=known-user-content").CountAsync());
        await page.ClickAsync("button:text-is('Continue')");

        // Should now be back at the client, signed in

        var clientAppHost = new Uri(HostFixture.ClientBaseUrl).Host;
        var pageUrlHost = new Uri(page.Url).Host;
        Assert.Equal(clientAppHost, pageUrlHost);

        var signedInEmail = await page.InnerTextAsync("data-testid=email");
        Assert.Equal(trn ?? string.Empty, await page.InnerTextAsync("data-testid=trn"));
        Assert.Equal(email, signedInEmail);

        async Task ClearCookiesForTestClient()
        {
            var cookies = await context.CookiesAsync();

            await context.ClearCookiesAsync();

            // All the Auth server cookies start with 'tis-'
            await context.AddCookiesAsync(
                cookies
                    .Where(c => c.Name.StartsWith("tis-"))
                    .Select(c => new Cookie()
                    {
                        Domain = c.Domain,
                        Expires = c.Expires,
                        HttpOnly = c.HttpOnly,
                        Name = c.Name,
                        Path = c.Path,
                        SameSite = c.SameSite,
                        Secure = c.Secure,
                        Value = c.Value
                    }));
        }
    }

    [Fact]
    public async Task NewTeacherUser_WithTrnMatchingExistingAccount_VerifiesExistingAccountEmailAndCanSignInSuccessfully()
    {
        var existingTrnOwner = await _hostFixture.TestData.CreateUser(hasTrn: true);

        var trn = existingTrnOwner.Trn!;
        var trnOwnerEmailAddress = existingTrnOwner.EmailAddress;
        var email = Faker.Internet.Email();
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        // Start on the client app and try to access a protected area

        await page.GotoAsync("/profile?scope=email+openid+profile+trn");

        // Fill in the sign in form (email + PIN)

        await page.FillAsync("text=Enter your email address", email);
        await page.ClickAsync("button:text-is('Continue')");

        var pin = _hostFixture.CapturedEmailConfirmationPins.Last().Pin;
        await page.FillAsync("text=Enter your code", pin);
        await page.ClickAsync("button:text-is('Continue')");

        // Should now be at the first bookend page

        var urlPath = new Uri(page.Url).LocalPath;
        Assert.EndsWith("/trn", urlPath);

        await page.ClickAsync("button:text-is('Continue')");

        // Should now be on our stub Find page

        urlPath = new Uri(page.Url).LocalPath;
        Assert.EndsWith("/FindALostTrn", urlPath);

        await page.FillAsync("#OfficialFirstName", firstName);
        await page.FillAsync("#OfficialLastName", lastName);
        await page.FillAsync("id=DateOfBirth.Day", dateOfBirth.Day.ToString());
        await page.FillAsync("id=DateOfBirth.Month", dateOfBirth.Month.ToString());
        await page.FillAsync("id=DateOfBirth.Year", dateOfBirth.Year.ToString());
        await page.FillAsync("#Trn", trn);

        await page.ClickAsync("button:text-is('Continue')");

        // Should now be on 'TRN in use' page

        pin = _hostFixture.CapturedEmailConfirmationPins.Last().Pin;
        await page.FillAsync("text=Enter your code", pin);

        await page.ClickAsync("button:text-is('Continue')");

        // Should now be on 'Choose email' page

        await page.ClickAsync($"text={trnOwnerEmailAddress}");
        await page.ClickAsync("button:text-is('Continue')");

        // Should now be on the confirmation page

        Assert.Equal(1, await page.Locator("data-testid=first-time-user-content").CountAsync());
        await page.ClickAsync("button:text-is('Continue')");

        // Should now be back on the client, signed in

        var clientAppHost = new Uri(HostFixture.ClientBaseUrl).Host;
        var pageUrlHost = new Uri(page.Url).Host;
        Assert.Equal(clientAppHost, pageUrlHost);

        Assert.Equal(existingTrnOwner.FirstName, await page.InnerTextAsync("data-testid=first-name"));
        Assert.Equal(existingTrnOwner.LastName, await page.InnerTextAsync("data-testid=last-name"));
        Assert.Equal(existingTrnOwner.EmailAddress, await page.InnerTextAsync("data-testid=email"));
        Assert.Equal(trn, await page.InnerTextAsync("data-testid=trn"));

        // Check events have been emitted

        _hostFixture.EventObserver.AssertEventsSaved(e => AssertEventIsUserSignedIn(e, existingTrnOwner.UserId));
    }

    [Fact]
    public async Task StaffUser_MissingPermission_GetsForbiddenError()
    {
        var staffUser = await _hostFixture.TestData.CreateUser(userType: UserType.Staff, staffRoles: Array.Empty<string>());

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        // Start on the client app and try to access a protected area with admin scope

        await page.GotoAsync($"/profile?scope={CustomScopes.UserRead}");

        // Fill in the sign in form (email + PIN)

        await page.FillAsync("text=Enter your email address", staffUser.EmailAddress);
        await page.ClickAsync("button:text-is('Continue')");

        var pin = _hostFixture.CapturedEmailConfirmationPins.Last().Pin;
        await page.FillAsync("text=Enter your code", pin);
        await page.ClickAsync("button:text-is('Continue')");

        // Should get a Forbidden error

        await page.WaitForSelectorAsync("h1:text-is('Forbidden')");
    }

    [Fact]
    public async Task FirstRequestToProtectedAreaOfSiteForUserAlreadySignedInViaOAuth_IssuesUserSignedInEvent()
    {
        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        var userId = await SignInExistingStaffUserWithTestClient(page);
        _hostFixture.EventObserver.Clear();

        // Try to access protected admin area on auth server, should be authenticated already

        await page.GotoAsync($"{HostFixture.AuthServerBaseUrl}/admin/staff");
        await page.WaitForSelectorAsync("caption:text-is('Staff users')");

        // Should have a second signed in event emitted

        _hostFixture.EventObserver.AssertEventsSaved(e => AssertEventIsUserSignedIn(e, userId, expectOAuthProperties: false));
    }

    [Fact]
    public async Task TeacherUser_WithTrnAssignedViaApi_CanSignInSuccessfully()
    {
        var user = await _hostFixture.TestData.CreateUser(hasTrn: true, haveCompletedTrnLookup: false);

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        // Start on the client app and try to access a protected area

        await page.GotoAsync($"/profile?scope=email+openid+profile+{Uri.EscapeDataString(CustomScopes.DqtRead)}");

        // Fill in the sign in form (email + PIN)

        await page.FillAsync("text=Enter your email address", user.EmailAddress);
        await page.ClickAsync("button:text-is('Continue')");

        var pin = _hostFixture.CapturedEmailConfirmationPins.Last().Pin;
        await page.FillAsync("text=Enter your code", pin);
        await page.ClickAsync("button:text-is('Continue')");

        // Should now be on the confirmation page

        Assert.Equal(1, await page.Locator("data-testid=known-user-content").CountAsync());
        await page.ClickAsync("button:text-is('Continue')");

        // Should now be back at the client, signed in

        var clientAppHost = new Uri(HostFixture.ClientBaseUrl).Host;
        var pageUrlHost = new Uri(page.Url).Host;
        Assert.Equal(clientAppHost, pageUrlHost);

        var signedInEmail = await page.InnerTextAsync("data-testid=email");
        Assert.Equal(user.Trn, await page.InnerTextAsync("data-testid=trn"));
        Assert.Equal(user.EmailAddress, signedInEmail);
    }

    private void AssertEventIsUserSignedIn(
        Events.EventBase @event,
        Guid userId,
        bool expectOAuthProperties = true)
    {
        var userSignedIn = Assert.IsType<Events.UserSignedInEvent>(@event);
        Assert.Equal(DateTime.UtcNow, userSignedIn.CreatedUtc, TimeSpan.FromSeconds(10));
        Assert.Equal(userId, userSignedIn.User.UserId);

        if (expectOAuthProperties)
        {
            Assert.Equal(_hostFixture.TestClientId, userSignedIn.ClientId);
            Assert.NotNull(userSignedIn.Scope);
        }
    }

    private async Task SignInAsNewTeacherUserWithTrnScope(
        IPage page,
        string email,
        string firstName,
        string lastName,
        string? trn,
        DateOnly dateOfBirth,
        string? preferredFirstName,
        string? preferredLastName)
    {
        // Start on the client app and try to access a protected area

        await page.GotoAsync("/profile?scope=email+openid+profile+trn");

        // Fill in the sign in form (email + PIN)

        await page.FillAsync("text=Enter your email address", email);
        await page.ClickAsync("button:text-is('Continue')");

        var pin = _hostFixture.CapturedEmailConfirmationPins.Last().Pin;
        await page.FillAsync("text=Enter your code", pin);
        await page.ClickAsync("button:text-is('Continue')");

        // Should now be at the first bookend page

        var urlPath = new Uri(page.Url).LocalPath;
        Assert.EndsWith("/trn", urlPath);

        await page.ClickAsync("button:text-is('Continue')");

        // Should now be on our stub Find page

        urlPath = new Uri(page.Url).LocalPath;
        Assert.EndsWith("/FindALostTrn", urlPath);

        await page.FillAsync("#OfficialFirstName", firstName);
        await page.FillAsync("#OfficialLastName", lastName);
        await page.FillAsync("#PreferredFirstName", preferredFirstName ?? string.Empty);
        await page.FillAsync("#PreferredLastName", preferredLastName ?? string.Empty);
        await page.FillAsync("id=DateOfBirth.Day", dateOfBirth.Day.ToString());
        await page.FillAsync("id=DateOfBirth.Month", dateOfBirth.Month.ToString());
        await page.FillAsync("id=DateOfBirth.Year", dateOfBirth.Year.ToString());
        await page.FillAsync("#Trn", trn ?? string.Empty);

        await page.ClickAsync("button:text-is('Continue')");

        // Should now be on the confirmation page

        Assert.Equal(1, await page.Locator("data-testid=first-time-user-content").CountAsync());
        await page.ClickAsync("button:text-is('Continue')");

        // Should now be back on the client, signed in

        var clientAppHost = new Uri(HostFixture.ClientBaseUrl).Host;
        var pageUrlHost = new Uri(page.Url).Host;
        Assert.Equal(clientAppHost, pageUrlHost);

        Assert.Equal(firstName, await page.InnerTextAsync("data-testid=first-name"));
        Assert.Equal(lastName, await page.InnerTextAsync("data-testid=last-name"));
        Assert.Equal(email, await page.InnerTextAsync("data-testid=email"));
        Assert.Equal(trn ?? string.Empty, await page.InnerTextAsync("data-testid=trn"));
    }

    private async Task<Guid> SignInExistingStaffUserWithTestClient(IPage page)
    {
        var user = await _hostFixture.TestData.CreateUser(userType: UserType.Staff, staffRoles: StaffRoles.All);

        // Start on the client app and try to access a protected area with admin scope

        await page.GotoAsync($"/profile?scope=email+openid+profile+{CustomScopes.UserRead}");

        // Fill in the sign in form (email + PIN)

        await page.FillAsync("text=Enter your email address", user.EmailAddress);
        await page.ClickAsync("button:text-is('Continue')");

        var pin = _hostFixture.CapturedEmailConfirmationPins.Last().Pin;
        await page.FillAsync("text=Enter your code", pin);
        await page.ClickAsync("button:text-is('Continue')");

        // Should now be on the confirmation page

        Assert.Equal(1, await page.Locator("data-testid=known-user-content").CountAsync());
        await page.ClickAsync("button:text-is('Continue')");

        // Should now be back at the client, signed in

        var clientAppHost = new Uri(HostFixture.ClientBaseUrl).Host;
        var pageUrlHost = new Uri(page.Url).Host;
        Assert.Equal(clientAppHost, pageUrlHost);

        var signedInEmail = await page.InnerTextAsync("data-testid=email");
        Assert.Equal(string.Empty, await page.InnerTextAsync("data-testid=trn"));
        Assert.Equal(user.EmailAddress, signedInEmail);

        // Check events have been emitted

        _hostFixture.EventObserver.AssertEventsSaved(e => AssertEventIsUserSignedIn(e, user.UserId));

        return user.UserId;
    }
}
