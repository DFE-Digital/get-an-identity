using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Account.OfficialDateOfBirth;

public class OfficialDateOfBirthTests : TestBase
{
    public OfficialDateOfBirthTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Theory]
    [MemberData(nameof(InvalidDateOfBirthState))]
    public async Task Get_OfficialDateOfBirthChangeDisabled_ReturnsBadRequest(bool hasTrn, bool hasDobConflict, bool hasPendingDobChange)
    {
        // Arrange
        if (hasTrn)
        {
            HostFixture.SetUserId(TestUsers.DefaultUserWithTrn.UserId);
            MockDqtApiResponse(TestUsers.DefaultUserWithTrn, hasDobConflict: hasDobConflict, hasPendingDateOfBirthChange: hasPendingDobChange);
        }
        else
        {
            HostFixture.SetUserId(TestUsers.DefaultUser.UserId);
        }

        var request = new HttpRequestMessage(HttpMethod.Get, $"/account/official-date-of-birth");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidRequest_ReturnsSuccess()
    {
        HostFixture.SetUserId(TestUsers.DefaultUserWithTrn.UserId);
        MockDqtApiResponse(TestUsers.DefaultUserWithTrn, hasDobConflict: true, hasPendingDateOfBirthChange: false);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/account/official-date-of-birth");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    private void MockDqtApiResponse(User user, bool hasDobConflict, bool hasPendingDateOfBirthChange)
    {
        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(user.Trn!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthServer.Services.DqtApi.TeacherInfo()
            {
                DateOfBirth = hasDobConflict ? user.DateOfBirth!.Value.AddDays(1) : user.DateOfBirth!.Value,
                FirstName = user.FirstName,
                LastName = user.LastName,
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                Trn = user.Trn!,
                PendingDateOfBirthChange = hasPendingDateOfBirthChange
            });
    }

    public static TheoryData<bool, bool, bool> InvalidDateOfBirthState { get; } = new()
    {
        // hasTrn, hasDobConflicts, hasPendingDobChange
        { false, false, false },
        { true, false, false },
        { true, true, true },
    };
}
