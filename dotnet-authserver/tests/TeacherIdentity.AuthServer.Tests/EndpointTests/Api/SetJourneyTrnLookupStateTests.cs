using Microsoft.EntityFrameworkCore;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Api;

public class SetJourneyTrnLookupStateTests : TestBase
{
    public SetJourneyTrnLookupStateTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Theory]
    [MemberData(nameof(InvalidStateData))]
    public async Task Put_InvalidState_ReturnsBadRequest(string firstName, string lastName, string dateOfBirth)
    {
        // Arrange
        var journeyId = Guid.NewGuid();

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/find-trn/user/{journeyId}")
        {
            Content = JsonContent.Create(new
            {
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = dateOfBirth
            })
        };

        // Act
        var response = await ApiKeyHttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Put_AlreadyGotLockedStateForJourneyId_ReturnsBadRequest()
    {
        // Arrange
        var journeyId = Guid.NewGuid();
        var user = await TestData.CreateUser();

        await TestData.WithDbContext(async dbContext =>
        {
            dbContext.JourneyTrnLookupStates.Add(new Models.JourneyTrnLookupState()
            {
                JourneyId = journeyId,
                Created = Clock.UtcNow,
                DateOfBirth = user.DateOfBirth!.Value,
                OfficialFirstName = user.FirstName,
                OfficialLastName = user.LastName,
                Locked = Clock.UtcNow,
                UserId = user.UserId,
                Trn = null,
                NationalInsuranceNumber = null
            });

            await dbContext.SaveChangesAsync();
        });

        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/find-trn/user/{journeyId}")
        {
            Content = JsonContent.Create(new
            {
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = dateOfBirth.ToString("yyyy-MM-dd")
            })
        };

        // Act
        var response = await ApiKeyHttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Put_AlreadyGotStateButUnlockedForJourneyId_WritesUpdatedStateToDbAndReturnsNoContent()
    {
        // Arrange
        var journeyId = Guid.NewGuid();

        await TestData.WithDbContext(async dbContext =>
        {
            dbContext.JourneyTrnLookupStates.Add(new Models.JourneyTrnLookupState()
            {
                JourneyId = journeyId,
                Created = Clock.UtcNow.AddMinutes(-2),
                DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
                OfficialFirstName = Faker.Name.First(),
                OfficialLastName = Faker.Name.Last(),
                Locked = null,
                UserId = null,
                Trn = null,
                NationalInsuranceNumber = null
            });

            await dbContext.SaveChangesAsync();
        });

        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var trn = TestData.GenerateTrn();

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/find-trn/user/{journeyId}")
        {
            Content = JsonContent.Create(new
            {
                OfficialFirstName = firstName,
                OfficialLastName = lastName,
                DateOfBirth = dateOfBirth.ToString("yyyy-MM-dd"),
                Trn = trn
            })
        };

        // Act
        var response = await ApiKeyHttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status204NoContent, (int)response.StatusCode);

        await TestData.WithDbContext(async dbContext =>
        {
            var state = await dbContext.JourneyTrnLookupStates.SingleOrDefaultAsync(s => s.JourneyId == journeyId);

            Assert.NotNull(state);
            Assert.Equal(firstName, state!.OfficialFirstName);
            Assert.Equal(lastName, state.OfficialLastName);
            Assert.Equal(dateOfBirth, state.DateOfBirth);
            Assert.Equal(trn, state.Trn);
        });
    }


    [Theory]
    [InlineData(null, "fakelastname")]
    [InlineData("testing", null)]
    public async Task Put_InvalidPreferredNameCombination_ReturnsBadRequest(string preferredFirstName, string preferredLastName)
    {
        // Arrange
        var journeyId = Guid.NewGuid();
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var trn = TestData.GenerateTrn();
        var nino = Faker.Identification.UkNationalInsuranceNumber(formatted: true);

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/find-trn/user/{journeyId}")
        {
            Content = JsonContent.Create(new
            {
                OfficialFirstName = firstName,
                OfficialLastName = lastName,
                DateOfBirth = dateOfBirth.ToString("yyyy-MM-dd"),
                Trn = trn,
                NationalInsuranceNumber = nino,
                PreferredFirstName = preferredFirstName,
                PreferredLastName = preferredLastName
            })
        };

        // Act
        var response = await ApiKeyHttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Put_ValidRequest_WritesStateToDbReturnsNoContent(bool hasTrn)
    {
        // Arrange
        var journeyId = Guid.NewGuid();
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());
        var trn = hasTrn ? TestData.GenerateTrn() : null;
        var nino = Faker.Identification.UkNationalInsuranceNumber(formatted: true);

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/find-trn/user/{journeyId}")
        {
            Content = JsonContent.Create(new
            {
                OfficialFirstName = firstName,
                OfficialLastName = lastName,
                DateOfBirth = dateOfBirth.ToString("yyyy-MM-dd"),
                Trn = trn,
                NationalInsuranceNumber = nino
            })
        };

        // Act
        var response = await ApiKeyHttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status204NoContent, (int)response.StatusCode);

        await TestData.WithDbContext(async dbContext =>
        {
            var state = await dbContext.JourneyTrnLookupStates.SingleOrDefaultAsync(s => s.JourneyId == journeyId);

            Assert.NotNull(state);
            Assert.Equal(firstName, state!.OfficialFirstName);
            Assert.Equal(lastName, state.OfficialLastName);
            Assert.Equal(dateOfBirth, state.DateOfBirth);
            Assert.Equal(trn, state.Trn);
            Assert.Equal(nino.ToUpper().Replace(" ", ""), state.NationalInsuranceNumber);
        });
    }

    public static TheoryData<string, string, string> InvalidStateData { get; } = new TheoryData<string, string, string>()
    {
        { "First", "Last", "" },
        { "First", "Last", "xxx" },
        { "", "Last", "01/01/2000" },
        { "First", "", "01/01/2000" },
        { "", "", "01/01/2000" },
        { "First", "", "" },
        { "", "Last", "" },
        { "", "", "" },
    };
}
