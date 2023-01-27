using TeacherIdentity.AuthServer.Pages.SignIn.Trn;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Trn;

public class TrnLookupPageModelTests
{
    [Theory]
    [InlineData("2345678", false)]  // Stated TRN matches found TRN and user said they don't have QTS
    [InlineData("2345678", true)]  // Stated TRN matches found TRN and user said they do have QTS
    [InlineData("1234567", false)]  // Stated TRN does not match found TRN and user said they don't have QTS
    [InlineData("1234567", true)]  // Stated TRN does not match found TRN and user said they do have QTS
    [InlineData(null, false)]  // No stated TRN and user said they don't have QTS
    [InlineData(null, true)]  // No stated TRN and user said they do have QTS
    public void GetTrnLookupStatus_TrnWasFound_ReturnsFound(string? statedTrn, bool awardedQts)
    {
        // Arrange
        var authenticationState = CreateAuthenticationState(statedTrn, awardedQts);
        var trnLookupResult = "2345678";

        // Act
        var trnLookupStatus = TrnLookupPageModel.GetTrnLookupStatus(trnLookupResult, authenticationState);

        // Assert
        Assert.Equal(TrnLookupStatus.Found, trnLookupStatus);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GetTrnLookupStatus_TrnWasNotFoundButUserStatedTheyHaveTrn_ReturnsPending(bool awardedQts)
    {
        // Arrange
        var authenticationState = CreateAuthenticationState(statedTrn: "1234567", awardedQts);
        var trnLookupResult = (string?)null;

        // Act
        var trnLookupStatus = TrnLookupPageModel.GetTrnLookupStatus(trnLookupResult, authenticationState);

        // Assert
        Assert.Equal(TrnLookupStatus.Pending, trnLookupStatus);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("1234567")]
    public void GetTrnLookupStatus_TrnWasNotFoundButUserStatedTheyHaveQts_ReturnsPending(string statedTrn)
    {
        // Arrange
        var authenticationState = CreateAuthenticationState(statedTrn, awardedQts: true);
        var trnLookupResult = (string?)null;

        // Act
        var trnLookupStatus = TrnLookupPageModel.GetTrnLookupStatus(trnLookupResult, authenticationState);

        // Assert
        Assert.Equal(TrnLookupStatus.Pending, trnLookupStatus);
    }

    [Fact]
    public void GetTrnLookupStatus_TrnWasNotFoundAndUserStatedTheyDoNotHaveTrnOrQts_ReturnsNone()
    {
        // Arrange
        var authenticationState = CreateAuthenticationState(statedTrn: null, awardedQts: false);
        var trnLookupResult = (string?)null;

        // Act
        var trnLookupStatus = TrnLookupPageModel.GetTrnLookupStatus(trnLookupResult, authenticationState);

        // Assert
        Assert.Equal(TrnLookupStatus.None, trnLookupStatus);
    }

    private static AuthenticationState CreateAuthenticationState(string? statedTrn, bool awardedQts)
    {
        var authState = new AuthenticationState(
            journeyId: Guid.NewGuid(),
            UserRequirements.TrnHolder,
            postSignInUrl: "/",
            startedAt: DateTime.Now);

        authState.OnEmailSet(Faker.Internet.Email());
        authState.OnEmailVerified();
        authState.OnHasTrnSet(hasTrn: statedTrn is not null);
        authState.StatedTrn = statedTrn;
        authState.OnOfficialNameSet(Faker.Name.First(), Faker.Name.Last(), previousOfficialFirstName: null, previousOfficialLastName: null);
        authState.OnDateOfBirthSet(DateOnly.FromDateTime(Faker.Identification.DateOfBirth()));
        authState.OnAwardedQtsSet(awardedQts);

        return authState;
    }
}
