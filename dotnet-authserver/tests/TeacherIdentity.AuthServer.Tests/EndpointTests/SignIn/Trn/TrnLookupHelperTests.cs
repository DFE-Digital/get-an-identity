using Microsoft.Extensions.Logging.Abstractions;
using TeacherIdentity.AuthServer.Pages.SignIn.Trn;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Trn;

public class TrnLookupHelperTests
{
    [Fact]
    public async Task LookupTrn_IsCancelled_ReturnsNullAndSetsNullResultOnAuthenticationState()
    {
        // Arrange
        var authenticationState = CreateAuthenticationState();

        // Set an existing result on AuthenticationState so we can be sure it's being cleared out
        authenticationState.OnTrnLookupCompleted(trn: "1234567", TrnLookupStatus.Found);

        var dqtApiClientMock = new Mock<IDqtApiClient>();
        dqtApiClientMock
            .Setup(mock => mock.FindTeachers(It.IsAny<FindTeachersRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var logger = new NullLogger<TrnLookupHelper>();

        var helper = new TrnLookupHelper(dqtApiClientMock.Object, logger);

        // Act
        var result = await helper.LookupTrn(authenticationState);

        // Assert
        Assert.Null(result);
        Assert.Null(authenticationState.Trn);
        Assert.NotNull(authenticationState.TrnLookupStatus);
        Assert.NotEqual(TrnLookupStatus.Found, authenticationState.TrnLookupStatus);
    }

    [Fact]
    public async Task LookupTrn_ReturnsNoResults_ReturnsNullAndSetsNullResultOnAuthenticationState()
    {
        // Arrange
        var authenticationState = CreateAuthenticationState();

        // Set an existing result on AuthenticationState so we can be sure it's being cleared out
        authenticationState.OnTrnLookupCompleted(trn: "1234567", TrnLookupStatus.Found);

        var dqtApiClientMock = new Mock<IDqtApiClient>();
        dqtApiClientMock
            .Setup(mock => mock.FindTeachers(It.IsAny<FindTeachersRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FindTeachersResponse()
            {
                Results = Array.Empty<FindTeachersResponseResult>()
            });

        var logger = new NullLogger<TrnLookupHelper>();

        var helper = new TrnLookupHelper(dqtApiClientMock.Object, logger);

        // Act
        var result = await helper.LookupTrn(authenticationState);

        // Assert
        Assert.Null(result);
        Assert.Null(authenticationState.Trn);
        Assert.NotNull(authenticationState.TrnLookupStatus);
        Assert.NotEqual(TrnLookupStatus.Found, authenticationState.TrnLookupStatus);
    }

    [Fact]
    public async Task LookupTrn_ReturnsMultipleResults_ReturnsNullAndSetsNullResultOnAuthenticationState()
    {
        // Arrange
        var authenticationState = CreateAuthenticationState();

        // Set an existing result on AuthenticationState so we can be sure it's being cleared out
        authenticationState.OnTrnLookupCompleted(trn: "1234567", TrnLookupStatus.Found);

        var dqtApiClientMock = new Mock<IDqtApiClient>();
        dqtApiClientMock
            .Setup(mock => mock.FindTeachers(It.IsAny<FindTeachersRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FindTeachersResponse()
            {
                Results = new[]
                {
                    new FindTeachersResponseResult()
                    {
                        DateOfBirth = authenticationState.DateOfBirth,
                        EmailAddresses = new[] { authenticationState.EmailAddress! },
                        FirstName = authenticationState.FirstName!,
                        LastName = authenticationState.LastName!,
                        HasActiveSanctions = false,
                        NationalInsuranceNumber = authenticationState.NationalInsuranceNumber,
                        Trn = "1234567",
                        Uid = Guid.NewGuid().ToString()
                    },
                    new FindTeachersResponseResult()
                    {
                        DateOfBirth = authenticationState.DateOfBirth,
                        EmailAddresses = new[] { authenticationState.EmailAddress! },
                        FirstName = authenticationState.FirstName!,
                        LastName = authenticationState.LastName!,
                        HasActiveSanctions = false,
                        NationalInsuranceNumber = authenticationState.NationalInsuranceNumber,
                        Trn = "2345678",
                        Uid = Guid.NewGuid().ToString()
                    }
                }
            });

        var logger = new NullLogger<TrnLookupHelper>();

        var helper = new TrnLookupHelper(dqtApiClientMock.Object, logger);

        // Act
        var result = await helper.LookupTrn(authenticationState);

        // Assert
        Assert.Null(result);
        Assert.Null(authenticationState.Trn);
        Assert.NotNull(authenticationState.TrnLookupStatus);
        Assert.NotEqual(TrnLookupStatus.Found, authenticationState.TrnLookupStatus);
    }

    [Fact]
    public async Task LookupTrn_ReturnsExactlyOneResult_ReturnsTrnAndSetsTrnOnAuthenticationState()
    {
        // Arrange
        var authenticationState = CreateAuthenticationState();

        var trn = "1234567";

        var dqtApiClientMock = new Mock<IDqtApiClient>();
        dqtApiClientMock
            .Setup(mock => mock.FindTeachers(It.IsAny<FindTeachersRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FindTeachersResponse()
            {
                Results = new[]
                {
                    new FindTeachersResponseResult()
                    {
                        DateOfBirth = authenticationState.DateOfBirth,
                        EmailAddresses = new[] { authenticationState.EmailAddress! },
                        FirstName = authenticationState.FirstName!,
                        LastName = authenticationState.LastName!,
                        HasActiveSanctions = false,
                        NationalInsuranceNumber = authenticationState.NationalInsuranceNumber,
                        Trn = trn,
                        Uid = Guid.NewGuid().ToString()
                    }
                }
            });

        var logger = new NullLogger<TrnLookupHelper>();

        var helper = new TrnLookupHelper(dqtApiClientMock.Object, logger);

        // Act
        var result = await helper.LookupTrn(authenticationState);

        // Assert
        Assert.Equal(trn, result);
        Assert.Equal(trn, authenticationState.Trn);
        Assert.Equal(TrnLookupStatus.Found, authenticationState.TrnLookupStatus);
    }

    [Fact]
    public async Task LookupTrn_NormalizesTrn_BeforeCallingDqtApi()
    {
        // Arrange
        var authenticationState = CreateAuthenticationState();
        authenticationState.OnTrnSet("RP99/12345");

        var dqtApiClientMock = new Mock<IDqtApiClient>();
        dqtApiClientMock
            .Setup(mock => mock.FindTeachers(It.IsAny<FindTeachersRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FindTeachersResponse()
            {
                Results = Array.Empty<FindTeachersResponseResult>()
            });

        var logger = new NullLogger<TrnLookupHelper>();

        var helper = new TrnLookupHelper(dqtApiClientMock.Object, logger);

        // Act
        await helper.LookupTrn(authenticationState);

        // Assert
        var expectedNormalizedTrn = "9912345";
        dqtApiClientMock.Verify(mock => mock.FindTeachers(It.Is<FindTeachersRequest>(r => r.Trn == expectedNormalizedTrn), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task LookupTrn_NormalizesNino_BeforeCallingDqtApi()
    {
        // Arrange
        var authenticationState = CreateAuthenticationState();
        authenticationState.OnNationalInsuranceNumberSet("qq 12 34 56 c");

        var dqtApiClientMock = new Mock<IDqtApiClient>();
        dqtApiClientMock
            .Setup(mock => mock.FindTeachers(It.IsAny<FindTeachersRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FindTeachersResponse()
            {
                Results = Array.Empty<FindTeachersResponseResult>()
            });

        var logger = new NullLogger<TrnLookupHelper>();

        var helper = new TrnLookupHelper(dqtApiClientMock.Object, logger);

        // Act
        await helper.LookupTrn(authenticationState);

        // Assert
        var expectedNormalizedNino = "QQ123456C";
        dqtApiClientMock.Verify(mock => mock.FindTeachers(It.Is<FindTeachersRequest>(r => r.NationalInsuranceNumber == expectedNormalizedNino), It.IsAny<CancellationToken>()));
    }

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

        var dqtApiClientMock = new Mock<IDqtApiClient>();
        var logger = new NullLogger<TrnLookupHelper>();

        var helper = new TrnLookupHelper(dqtApiClientMock.Object, logger);

        // Act
        var trnLookupStatus = helper.GetTrnLookupStatus(trnLookupResult, authenticationState);

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

        var dqtApiClientMock = new Mock<IDqtApiClient>();
        var logger = new NullLogger<TrnLookupHelper>();

        var helper = new TrnLookupHelper(dqtApiClientMock.Object, logger);

        // Act
        var trnLookupStatus = helper.GetTrnLookupStatus(trnLookupResult, authenticationState);

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

        var dqtApiClientMock = new Mock<IDqtApiClient>();
        var logger = new NullLogger<TrnLookupHelper>();

        var helper = new TrnLookupHelper(dqtApiClientMock.Object, logger);

        // Act
        var trnLookupStatus = helper.GetTrnLookupStatus(trnLookupResult, authenticationState);

        // Assert
        Assert.Equal(TrnLookupStatus.Pending, trnLookupStatus);
    }

    [Fact]
    public void GetTrnLookupStatus_TrnWasNotFoundAndUserStatedTheyDoNotHaveTrnOrQts_ReturnsNone()
    {
        // Arrange
        var authenticationState = CreateAuthenticationState(statedTrn: null, awardedQts: false);
        var trnLookupResult = (string?)null;

        var dqtApiClientMock = new Mock<IDqtApiClient>();
        var logger = new NullLogger<TrnLookupHelper>();

        var helper = new TrnLookupHelper(dqtApiClientMock.Object, logger);

        // Act
        var trnLookupStatus = helper.GetTrnLookupStatus(trnLookupResult, authenticationState);

        // Assert
        Assert.Equal(TrnLookupStatus.None, trnLookupStatus);
    }

    private static AuthenticationState CreateAuthenticationState(string? statedTrn = null, bool awardedQts = true)
    {
        var authState = new AuthenticationState(
            journeyId: Guid.NewGuid(),
            UserRequirements.TrnHolder,
            postSignInUrl: "/",
            startedAt: DateTime.Now);

        authState.OnEmailSet(Faker.Internet.Email());
        authState.OnEmailVerified();
        authState.OnTrnSet(statedTrn);
        authState.OnOfficialNameSet(
            Faker.Name.First(),
            Faker.Name.Last(),
            AuthenticationState.HasPreviousNameOption.No,
            previousOfficialFirstName: null,
            previousOfficialLastName: null);
        authState.OnDateOfBirthSet(DateOnly.FromDateTime(Faker.Identification.DateOfBirth()));
        authState.OnAwardedQtsSet(awardedQts);

        return authState;
    }
}
