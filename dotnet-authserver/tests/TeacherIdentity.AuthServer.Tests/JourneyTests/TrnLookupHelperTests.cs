using Microsoft.Extensions.Logging.Abstractions;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Tests.JourneyTests;

public class TrnLookupHelperTests
{
    [Fact]
    public async Task LookupTrn_IsCancelled_ReturnsNullAndSetsNullResultOnAuthenticationState()
    {
        // Arrange
        var authenticationState = CreateAuthenticationState();

        // Set an existing result on AuthenticationState so we can be sure it's being cleared out
        authenticationState.OnTrnLookupCompleted(findTeachersResult: GetTeachersResponseResult("1234567"), TrnLookupStatus.Found);

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
        authenticationState.OnTrnLookupCompleted(findTeachersResult: GetTeachersResponseResult("1234567"), TrnLookupStatus.Found);

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
        authenticationState.OnTrnLookupCompleted(findTeachersResult: GetTeachersResponseResult("1234567"), TrnLookupStatus.Found);

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
                        MiddleName = authenticationState.MiddleName!,
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
                        MiddleName = authenticationState.MiddleName!,
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
                        MiddleName = authenticationState.MiddleName!,
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
    public void ResolveTrn_TrnWasFound_ReturnsFoundAndTrn(string? statedTrn, bool awardedQts)
    {
        // Arrange
        var authenticationState = CreateAuthenticationState(statedTrn, awardedQts);
        var trnLookupResults = new[] { GetTeachersResponseResult("2345678") };

        var dqtApiClientMock = new Mock<IDqtApiClient>();
        var logger = new NullLogger<TrnLookupHelper>();

        var helper = new TrnLookupHelper(dqtApiClientMock.Object, logger);

        // Act
        var (trn, trnLookupStatus) = helper.ResolveTrn(trnLookupResults, authenticationState);

        // Assert
        Assert.Equal(TrnLookupStatus.Found, trnLookupStatus);
        Assert.NotNull(trn);
    }

    [Theory]
    [InlineData(true, "1234567")]
    [InlineData(true, null)]
    [InlineData(false, "1234567")]
    [InlineData(false, null)]
    public void ResolveTrn_MultipleTrnsWereMatchedReturnsPending(bool awardedQts, string? statedTrn)
    {
        // Arrange
        var authenticationState = CreateAuthenticationState(statedTrn, awardedQts);
        var trnLookupResults = new[] { GetTeachersResponseResult("2345678"), GetTeachersResponseResult("3456789") };

        var dqtApiClientMock = new Mock<IDqtApiClient>();
        var logger = new NullLogger<TrnLookupHelper>();

        var helper = new TrnLookupHelper(dqtApiClientMock.Object, logger);

        // Act
        var (trn, trnLookupStatus) = helper.ResolveTrn(trnLookupResults, authenticationState);

        // Assert
        Assert.Equal(TrnLookupStatus.Pending, trnLookupStatus);
        Assert.Null(trn);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ResolveTrn_TrnWasNotFoundButUserStatedTheyHaveTrn_ReturnsPendingAndNullTrn(bool awardedQts)
    {
        // Arrange
        var authenticationState = CreateAuthenticationState(statedTrn: "1234567", awardedQts);
        var trnLookupResults = Array.Empty<FindTeachersResponseResult>();

        var dqtApiClientMock = new Mock<IDqtApiClient>();
        var logger = new NullLogger<TrnLookupHelper>();

        var helper = new TrnLookupHelper(dqtApiClientMock.Object, logger);

        // Act
        var (trn, trnLookupStatus) = helper.ResolveTrn(trnLookupResults, authenticationState);

        // Assert
        Assert.Equal(TrnLookupStatus.Pending, trnLookupStatus);
        Assert.Null(trn);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("1234567")]
    public void ResolveTrn_TrnWasNotFoundButUserStatedTheyHaveQts_ReturnsPendingAndNullTrn(string? statedTrn)
    {
        // Arrange
        var authenticationState = CreateAuthenticationState(statedTrn, awardedQts: true);
        var trnLookupResults = Array.Empty<FindTeachersResponseResult>();

        var dqtApiClientMock = new Mock<IDqtApiClient>();
        var logger = new NullLogger<TrnLookupHelper>();

        var helper = new TrnLookupHelper(dqtApiClientMock.Object, logger);

        // Act
        var (trn, trnLookupStatus) = helper.ResolveTrn(trnLookupResults, authenticationState);

        // Assert
        Assert.Equal(TrnLookupStatus.Pending, trnLookupStatus);
        Assert.Null(trn);
    }

    [Fact]
    public void ResolveTrn_TrnWasNotFoundAndUserStatedTheyDoNotHaveTrnOrQts_ReturnsNoneAndNullTrn()
    {
        // Arrange
        var authenticationState = CreateAuthenticationState(statedTrn: null, awardedQts: false);
        var trnLookupResults = Array.Empty<FindTeachersResponseResult>();

        var dqtApiClientMock = new Mock<IDqtApiClient>();
        var logger = new NullLogger<TrnLookupHelper>();

        var helper = new TrnLookupHelper(dqtApiClientMock.Object, logger);

        // Act
        var (trn, trnLookupStatus) = helper.ResolveTrn(trnLookupResults, authenticationState);

        // Assert
        Assert.Equal(TrnLookupStatus.None, trnLookupStatus);
        Assert.Null(trn);
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

    private FindTeachersResponseResult GetTeachersResponseResult(
        string trn,
        string? firstName = null,
        string? middleName = null,
        string? lastName = null)
    {
        return new FindTeachersResponseResult()
        {
            Trn = trn,
            EmailAddresses = new[] { Faker.Internet.Email() },
            FirstName = firstName ?? Faker.Name.First(),
            MiddleName = middleName ?? Faker.Name.Middle(),
            LastName = lastName ?? Faker.Name.Last(),
            DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
            NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
            Uid = "",
            HasActiveSanctions = false,
        };
    }
}
