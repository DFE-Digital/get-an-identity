using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Account.OfficialName;

public class OfficialNameTests : TestBase
{
    public OfficialNameTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Get_OfficialNameChangeDisabled_ReturnsBadRequest(bool hasTrn)
    {
        // Arrange
        if (hasTrn)
        {
            HostFixture.SetUserId(TestUsers.DefaultUserWithTrn.UserId);
            MockDqtApiResponse(TestUsers.DefaultUserWithTrn, hasPendingNameChange: true);
        }
        else
        {
            HostFixture.SetUserId(TestUsers.DefaultUser.UserId);
        }

        var request = new HttpRequestMessage(HttpMethod.Get, $"/account/official-name");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidRequest_ReturnsSuccess()
    {
        HostFixture.SetUserId(TestUsers.DefaultUserWithTrn.UserId);
        MockDqtApiResponse(TestUsers.DefaultUserWithTrn, hasPendingNameChange: false);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/account/official-name");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    private void MockDqtApiResponse(User user, bool hasPendingNameChange)
    {
        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(user.Trn!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthServer.Services.DqtApi.TeacherInfo()
            {
                DateOfBirth = user.DateOfBirth!.Value,
                FirstName = user.FirstName,
                MiddleName = "",
                LastName = user.LastName,
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                Trn = user.Trn!,
                PendingDateOfBirthChange = false,
                PendingNameChange = hasPendingNameChange,
                Email = null,
                Alerts = Array.Empty<AlertInfo>()
            });
    }
}
