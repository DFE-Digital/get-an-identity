﻿using Microsoft.EntityFrameworkCore;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Api;

public class SetJourneyFindALostTrnUserTests : ApiTestBase
{
    public SetJourneyFindALostTrnUserTests(HostFixture hostFixture)
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
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Put_AlreadyGotStateForJourneyId_ReturnsBadRequest()
    {
        // Arrange
        var journeyId = Guid.NewGuid();
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();
        var dateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth());

        await TestData.WithDbContext(async dbContext =>
        {
            dbContext.JourneyTrnLookupStates.Add(new Models.JourneyTrnLookupState()
            {
                JourneyId = journeyId,
                Created = Clock.UtcNow,
                DateOfBirth = dateOfBirth,
                FirstName = firstName,
                LastName = lastName
            });

            await dbContext.SaveChangesAsync();
        });

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
        var response = await HttpClient.SendAsync(request);

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

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/find-trn/user/{journeyId}")
        {
            Content = JsonContent.Create(new
            {
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = dateOfBirth.ToString("yyyy-MM-dd"),
                Trn = trn
            })
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status204NoContent, (int)response.StatusCode);

        await TestData.WithDbContext(async dbContext =>
        {
            var state = await dbContext.JourneyTrnLookupStates.SingleOrDefaultAsync(s => s.JourneyId == journeyId);

            Assert.NotNull(state);
            Assert.Equal(firstName, state!.FirstName);
            Assert.Equal(lastName, state.LastName);
            Assert.Equal(dateOfBirth, state.DateOfBirth);
            Assert.Equal(trn, state.Trn);
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