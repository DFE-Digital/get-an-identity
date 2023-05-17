using Moq;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.EndToEndTests;

public class TrnTokenSignInJourney : IClassFixture<HostFixture>
{
    private readonly HostFixture _hostFixture;

    public TrnTokenSignInJourney(HostFixture hostFixture)
    {
        _hostFixture = hostFixture;
        _hostFixture.OnTestStarting();
    }

    [Fact]
    public async Task NewTeacherUser_WithTrnToken_CreatesUserAndCompletesFlow()
    {
        var mobileNumber = _hostFixture.TestData.GenerateUniqueMobileNumber();
        var firstName = Faker.Name.First();
        var middleName = Faker.Name.Middle();
        var lastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var trn = _hostFixture.TestData.GenerateTrn();

        var trnToken = await _hostFixture.TestData.GenerateTrnToken(trn);

        _hostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(trn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherInfo()
            {
                DateOfBirth = dateOfBirth,
                FirstName = firstName,
                MiddleName = middleName,
                LastName = lastName,
                Trn = trn,
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                PendingDateOfBirthChange = false,
                PendingNameChange = false
            });

        await using var context = await _hostFixture.CreateBrowserContext();
        var page = await context.NewPageAsync();

        await page.StartOAuthJourney(CustomScopes.DqtRead, trnToken: trnToken.TrnToken);

        await page.RegisterFromTrnTokenLandingPage();

        await page.SubmitRegisterPhonePage(mobileNumber);

        await page.SubmitRegisterPhoneConfirmationPage();

        await page.SubmitTrnTokenCheckAnswersPage();

        await page.SubmitCompletePageForNewUser();

        await page.AssertSignedInOnTestClient(trnToken.Email, trn, firstName, lastName);
    }
}
