using System.Security.Claims;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services.DqtApi;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer.Tests;

public class UserClaimHelperTests : IClassFixture<DbFixture>
{
    private readonly DbFixture _dbFixture;

    public UserClaimHelperTests(DbFixture dbFixture)
    {
        _dbFixture = dbFixture;
    }

    [Theory]
    [MemberData(nameof(GetOptionalClaimsData))]
    public async Task GetPublicClaims_FromUser_ReturnsExpectedClaims(bool haveTrnScope, bool hasMobileNumber, bool hasPreferredName)
    {
        // Arrange
        var user = await _dbFixture.TestData.CreateUser(hasTrn: haveTrnScope, hasMobileNumber: hasMobileNumber, hasPreferredName: hasPreferredName);

        var dqtApiClient = Mock.Of<IDqtApiClient>();

        using var dbContext = _dbFixture.GetDbContext();
        var userClaimHelper = new UserClaimHelper(dbContext, dqtApiClient);

        // Act
        var result = await userClaimHelper.GetPublicClaims(
            user.UserId,
            trnMatchPolicy: haveTrnScope ? TrnMatchPolicy.Default : null);

        // Assert
        var expectedClaims = new List<Claim>()
        {
            new Claim(Claims.Subject, user.UserId.ToString()!),
            new Claim(Claims.Email, user.EmailAddress),
            new Claim(Claims.EmailVerified, bool.TrueString),
            new Claim(Claims.Name, user.FirstName + " " + user.MiddleName + " " + user.LastName),
            new Claim(Claims.GivenName, user.FirstName),
            new Claim(Claims.FamilyName, user.LastName),
            new Claim(Claims.Birthdate, user.DateOfBirth!.Value.ToString("yyyy-MM-dd")),
        };

        if (hasPreferredName)
        {
            expectedClaims.Add(new Claim(CustomClaims.PreferredName, user.FirstName + " " + user.LastName));
        }

        if (!string.IsNullOrEmpty(user.MiddleName))
        {
            expectedClaims.Add(new Claim(Claims.MiddleName, user.MiddleName));
        }

        if (haveTrnScope)
        {
            expectedClaims.Add(new Claim(CustomClaims.Trn, user.Trn!));
            expectedClaims.Add(new Claim(CustomClaims.TrnLookupStatus, user.TrnLookupStatus!.Value.ToString()));
        }

        if (hasMobileNumber)
        {
            expectedClaims.Add(new Claim(Claims.PhoneNumber, user.MobileNumber!));
            expectedClaims.Add(new Claim(Claims.PhoneNumberVerified, bool.TrueString));
        }

        Assert.Equal(expectedClaims.OrderBy(c => c.Type), result.OrderBy(c => c.Type), new ClaimTypeAndValueEqualityComparer());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetPublicClaims_FromUserWithMergedUsers_ReturnsPreviousUserIdClaim(bool hasMergedUsers)
    {
        // Arrange
        User? mergedUser = null;
        User? anotherMergedUser = null;

        var user = await _dbFixture.TestData.CreateUser();
        if (hasMergedUsers)
        {
            mergedUser = await _dbFixture.TestData.CreateUser(mergedWithUserId: user.UserId);
            anotherMergedUser = await _dbFixture.TestData.CreateUser(mergedWithUserId: user.UserId);
        }

        var dqtApiClient = Mock.Of<IDqtApiClient>();

        using var dbContext = _dbFixture.GetDbContext();
        var userClaimHelper = new UserClaimHelper(dbContext, dqtApiClient);

        // Act
        var result = await userClaimHelper.GetPublicClaims(
            user.UserId,
            trnMatchPolicy: null);

        // Assert
        if (hasMergedUsers)
        {
            var expectedPreviousUserIdClaims = new List<Claim>()
            {
                new Claim(CustomClaims.PreviousUserId, mergedUser!.UserId.ToString()),
                new Claim(CustomClaims.PreviousUserId, anotherMergedUser!.UserId.ToString()),
            };
            Assert.Equal(
                expectedPreviousUserIdClaims.OrderBy(c => c.Value),
                result.Where(c => c.Type == CustomClaims.PreviousUserId).OrderBy(c => c.Value),
                new ClaimTypeAndValueEqualityComparer());
        }
        else
        {
            Assert.DoesNotContain(result, c => c.Type == CustomClaims.PreviousUserId);
        }
    }

    [Fact]
    public async Task GetPublicClaims_TrnMatchPolicyDefaultAndUserHasTrnWithLowTrnVerificationLevel_ReturnsTrnClaimButNoNinoClaims()
    {
        // Arrange
        var trnMatchPolicy = TrnMatchPolicy.Default;
        var nino = Faker.Identification.UkNationalInsuranceNumber();

        var user = await _dbFixture.TestData.CreateUser(
            hasTrn: false,
            trnAssociationSource: TrnAssociationSource.Lookup,
            trnVerificationLevel: TrnVerificationLevel.Low);

        var dqtApiClientMock = new Mock<IDqtApiClient>();

        using var dbContext = _dbFixture.GetDbContext();
        var userClaimHelper = new UserClaimHelper(dbContext, dqtApiClientMock.Object);

        // Act
        var result = await userClaimHelper.GetPublicClaims(
            user.UserId,
            trnMatchPolicy);

        // Assert
        Assert.DoesNotContain(result, c => c.Type == CustomClaims.Trn);
        Assert.DoesNotContain(result, c => c.Type == CustomClaims.NiNumber);
        Assert.DoesNotContain(result, c => c.Type == CustomClaims.TrnMatchNiNumber);
    }

    [Fact]
    public async Task GetPublicClaims_TrnMatchPolicyDefaultAndUserHasNoTrn_DoesNotReturnTrnClaimOrNinoClaims()
    {
        // Arrange
        var trnMatchPolicy = TrnMatchPolicy.Default;
        var nino = Faker.Identification.UkNationalInsuranceNumber();

        var user = await _dbFixture.TestData.CreateUser(hasTrn: false);

        var dqtApiClientMock = new Mock<IDqtApiClient>();

        using var dbContext = _dbFixture.GetDbContext();
        var userClaimHelper = new UserClaimHelper(dbContext, dqtApiClientMock.Object);

        // Act
        var result = await userClaimHelper.GetPublicClaims(
            user.UserId,
            trnMatchPolicy);

        // Assert
        Assert.DoesNotContain(result, c => c.Type == CustomClaims.Trn);
        Assert.DoesNotContain(result, c => c.Type == CustomClaims.NiNumber);
        Assert.DoesNotContain(result, c => c.Type == CustomClaims.TrnMatchNiNumber);
    }

    [Fact]
    public async Task GetPublicClaims_TrnMatchPolicyStrictAndUserTrnAssociationSourceIsLookupWithLowTrnVerificationLevel_DoesNotReturnTrnClaimOrNinoClaims()
    {
        // Arrange
        var trnMatchPolicy = TrnMatchPolicy.Strict;
        var nino = Faker.Identification.UkNationalInsuranceNumber();

        var user = await _dbFixture.TestData.CreateUser(
            hasTrn: true,
            trnAssociationSource: TrnAssociationSource.Lookup,
            trnVerificationLevel: TrnVerificationLevel.Low,
            nationalInsuranceNumber: nino);

        var dqtApiClientMock = new Mock<IDqtApiClient>();
        dqtApiClientMock.Setup(mock => mock.GetTeacherByTrn(user.Trn!, It.IsAny<CancellationToken>())).ReturnsAsync(new TeacherInfo()
        {
            DateOfBirth = user.DateOfBirth,
            Email = user.EmailAddress,
            FirstName = user.FirstName,
            MiddleName = user.MiddleName ?? string.Empty,
            LastName = user.LastName,
            NationalInsuranceNumber = nino,
            PendingDateOfBirthChange = false,
            PendingNameChange = false,
            Trn = user.Trn!,
            Alerts = Array.Empty<AlertInfo>()
        });

        using var dbContext = _dbFixture.GetDbContext();
        var userClaimHelper = new UserClaimHelper(dbContext, dqtApiClientMock.Object);

        // Act
        var result = await userClaimHelper.GetPublicClaims(
            user.UserId,
            trnMatchPolicy);

        // Assert
        Assert.DoesNotContain(result, c => c.Type == CustomClaims.Trn);
        Assert.DoesNotContain(result, c => c.Type == CustomClaims.NiNumber);
        Assert.DoesNotContain(result, c => c.Type == CustomClaims.TrnMatchNiNumber);
    }

    [Fact]
    public async Task GetPublicClaims_TrnMatchPolicyStrictAndUserTrnAssociationSourceIsApiWithLowTrnVerificationLevel_DoesNotReturnTrnClaimOrNinoClaims()
    {
        // Arrange
        var trnMatchPolicy = TrnMatchPolicy.Strict;
        var nino = Faker.Identification.UkNationalInsuranceNumber();

        var user = await _dbFixture.TestData.CreateUser(
            hasTrn: true,
            trnAssociationSource: TrnAssociationSource.Api,
            trnVerificationLevel: TrnVerificationLevel.Low,
            nationalInsuranceNumber: nino);

        var dqtApiClientMock = new Mock<IDqtApiClient>();
        dqtApiClientMock.Setup(mock => mock.GetTeacherByTrn(user.Trn!, It.IsAny<CancellationToken>())).ReturnsAsync(new TeacherInfo()
        {
            DateOfBirth = user.DateOfBirth,
            Email = user.EmailAddress,
            FirstName = user.FirstName,
            MiddleName = user.MiddleName ?? string.Empty,
            LastName = user.LastName,
            NationalInsuranceNumber = nino,
            PendingDateOfBirthChange = false,
            PendingNameChange = false,
            Trn = user.Trn!,
            Alerts = Array.Empty<AlertInfo>()
        });

        using var dbContext = _dbFixture.GetDbContext();
        var userClaimHelper = new UserClaimHelper(dbContext, dqtApiClientMock.Object);

        // Act
        var result = await userClaimHelper.GetPublicClaims(
            user.UserId,
            trnMatchPolicy);

        // Assert
        Assert.DoesNotContain(result, c => c.Type == CustomClaims.Trn);
        Assert.DoesNotContain(result, c => c.Type == CustomClaims.NiNumber);
        Assert.DoesNotContain(result, c => c.Type == CustomClaims.TrnMatchNiNumber);
    }

    [Fact]
    public async Task GetPublicClaims_TrnMatchPolicyStrictAndUserTrnAssociationSourceIsUserImportWithLowTrnVerificationLevel_DoesNotReturnTrnClaimOrNinoClaims()
    {
        // Arrange
        var trnMatchPolicy = TrnMatchPolicy.Strict;
        var nino = Faker.Identification.UkNationalInsuranceNumber();

        var user = await _dbFixture.TestData.CreateUser(
            hasTrn: true,
            trnAssociationSource: TrnAssociationSource.UserImport,
            trnVerificationLevel: TrnVerificationLevel.Low,
            nationalInsuranceNumber: nino);

        var dqtApiClientMock = new Mock<IDqtApiClient>();
        dqtApiClientMock.Setup(mock => mock.GetTeacherByTrn(user.Trn!, It.IsAny<CancellationToken>())).ReturnsAsync(new TeacherInfo()
        {
            DateOfBirth = user.DateOfBirth,
            Email = user.EmailAddress,
            FirstName = user.FirstName,
            MiddleName = user.MiddleName ?? string.Empty,
            LastName = user.LastName,
            NationalInsuranceNumber = nino,
            PendingDateOfBirthChange = false,
            PendingNameChange = false,
            Trn = user.Trn!,
            Alerts = Array.Empty<AlertInfo>()
        });

        using var dbContext = _dbFixture.GetDbContext();
        var userClaimHelper = new UserClaimHelper(dbContext, dqtApiClientMock.Object);

        // Act
        var result = await userClaimHelper.GetPublicClaims(
            user.UserId,
            trnMatchPolicy);

        // Assert
        Assert.DoesNotContain(result, c => c.Type == CustomClaims.Trn);
        Assert.DoesNotContain(result, c => c.Type == CustomClaims.NiNumber);
        Assert.DoesNotContain(result, c => c.Type == CustomClaims.TrnMatchNiNumber);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetPublicClaims_TrnMatchPolicyStrictAndUserTrnAssociationSourceIsTrnToken_ReturnsTrnAndNinoClaims(bool userHasNino)
    {
        // Arrange
        var trnMatchPolicy = TrnMatchPolicy.Strict;
        var nino = Faker.Identification.UkNationalInsuranceNumber();

        var user = await _dbFixture.TestData.CreateUser(
            hasTrn: true,
            trnAssociationSource: TrnAssociationSource.SupportUi,
            trnVerificationLevel: TrnVerificationLevel.Low,
            nationalInsuranceNumber: userHasNino ? nino : null);

        var dqtApiClientMock = new Mock<IDqtApiClient>();
        dqtApiClientMock.Setup(mock => mock.GetTeacherByTrn(user.Trn!, It.IsAny<CancellationToken>())).ReturnsAsync(new TeacherInfo()
        {
            DateOfBirth = user.DateOfBirth,
            Email = user.EmailAddress,
            FirstName = user.FirstName,
            MiddleName = user.MiddleName ?? string.Empty,
            LastName = user.LastName,
            NationalInsuranceNumber = nino,
            PendingDateOfBirthChange = false,
            PendingNameChange = false,
            Trn = user.Trn!,
            Alerts = Array.Empty<AlertInfo>()
        });

        using var dbContext = _dbFixture.GetDbContext();
        var userClaimHelper = new UserClaimHelper(dbContext, dqtApiClientMock.Object);

        // Act
        var result = await userClaimHelper.GetPublicClaims(
            user.UserId,
            trnMatchPolicy);

        // Assert
        Assert.Contains(result, c => c.Type == CustomClaims.Trn && c.Value == user.Trn);
        Assert.Contains(result, c => c.Type == CustomClaims.NiNumber && c.Value == nino);
        Assert.Contains(result, c => c.Type == CustomClaims.TrnMatchNiNumber && c.Value.Equals("true", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetPublicClaims_TrnMatchPolicyStrictAndUserTrnAssociationSourceIsSupportUi_ReturnsTrnAndNinoClaims(bool userHasNino)
    {
        // Arrange
        var trnMatchPolicy = TrnMatchPolicy.Strict;
        var nino = Faker.Identification.UkNationalInsuranceNumber();

        var user = await _dbFixture.TestData.CreateUser(
            hasTrn: true,
            trnAssociationSource: TrnAssociationSource.SupportUi,
            trnVerificationLevel: TrnVerificationLevel.Low,
            nationalInsuranceNumber: userHasNino ? nino : null);

        var dqtApiClientMock = new Mock<IDqtApiClient>();
        dqtApiClientMock.Setup(mock => mock.GetTeacherByTrn(user.Trn!, It.IsAny<CancellationToken>())).ReturnsAsync(new TeacherInfo()
        {
            DateOfBirth = user.DateOfBirth,
            Email = user.EmailAddress,
            FirstName = user.FirstName,
            MiddleName = user.MiddleName ?? string.Empty,
            LastName = user.LastName,
            NationalInsuranceNumber = nino,
            PendingDateOfBirthChange = false,
            PendingNameChange = false,
            Trn = user.Trn!,
            Alerts = Array.Empty<AlertInfo>()
        });

        using var dbContext = _dbFixture.GetDbContext();
        var userClaimHelper = new UserClaimHelper(dbContext, dqtApiClientMock.Object);

        // Act
        var result = await userClaimHelper.GetPublicClaims(
            user.UserId,
            trnMatchPolicy);

        // Assert
        Assert.Contains(result, c => c.Type == CustomClaims.Trn && c.Value == user.Trn);
        Assert.Contains(result, c => c.Type == CustomClaims.NiNumber && c.Value == nino);
        Assert.Contains(result, c => c.Type == CustomClaims.TrnMatchNiNumber && c.Value.Equals("true", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetPublicClaims_TrnMatchPolicyStrictAndUserTrnAssociationSourceIsLookupWithMediumTrnVerificationLevel_ReturnsTrnAndNinoClaims(bool userHasNino)
    {
        // Arrange
        var trnMatchPolicy = TrnMatchPolicy.Strict;
        var nino = Faker.Identification.UkNationalInsuranceNumber();

        var user = await _dbFixture.TestData.CreateUser(
            hasTrn: true,
            trnAssociationSource: TrnAssociationSource.Lookup,
            trnVerificationLevel: TrnVerificationLevel.Medium,
            nationalInsuranceNumber: userHasNino ? nino : null);

        var dqtApiClientMock = new Mock<IDqtApiClient>();
        dqtApiClientMock.Setup(mock => mock.GetTeacherByTrn(user.Trn!, It.IsAny<CancellationToken>())).ReturnsAsync(new TeacherInfo()
        {
            DateOfBirth = user.DateOfBirth,
            Email = user.EmailAddress,
            FirstName = user.FirstName,
            MiddleName = user.MiddleName ?? string.Empty,
            LastName = user.LastName,
            NationalInsuranceNumber = nino,
            PendingDateOfBirthChange = false,
            PendingNameChange = false,
            Trn = user.Trn!,
            Alerts = Array.Empty<AlertInfo>()
        });

        using var dbContext = _dbFixture.GetDbContext();
        var userClaimHelper = new UserClaimHelper(dbContext, dqtApiClientMock.Object);

        // Act
        var result = await userClaimHelper.GetPublicClaims(
            user.UserId,
            trnMatchPolicy);

        // Assert
        Assert.Contains(result, c => c.Type == CustomClaims.Trn && c.Value == user.Trn);
        Assert.Contains(result, c => c.Type == CustomClaims.NiNumber && c.Value == nino);
        Assert.Contains(result, c => c.Type == CustomClaims.TrnMatchNiNumber && c.Value.Equals("true", StringComparison.OrdinalIgnoreCase));
    }

    public static IEnumerable<object[]> GetOptionalClaimsData()
    {
        var boolValues = new[] { true, false };

        foreach (var haveTrnScope in boolValues)
        {
            foreach (var hasMobileNumber in boolValues)
            {
                foreach (var hasPreferredName in boolValues)
                {
                    yield return new object[] { haveTrnScope, hasMobileNumber, hasPreferredName };
                }
            }
        }
    }
}
