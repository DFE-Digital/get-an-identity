using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Jobs;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.DqtApi;
using User = TeacherIdentity.AuthServer.Models.User;

namespace TeacherIdentity.AuthServer.Tests.Jobs;

[Collection(nameof(DisableParallelization))]
public class SyncNamesWithDqtJobTests : IClassFixture<DbFixture>, IAsyncLifetime
{
    private readonly DbFixture _dbFixture;

    public SyncNamesWithDqtJobTests(DbFixture dbFixture)
    {
        _dbFixture = dbFixture;
    }

    public async Task InitializeAsync()
    {
        await ClearNonTestUsers();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Execute_WhenDqtNamesAreDifferentToIdentity_UpdatesUserAndInsertsEvent()
    {
        // Arrange
        var trn = "1234567";
        var created = _dbFixture.Clock.UtcNow.AddDays(-5);
        var user = new User
        {
            UserId = Guid.NewGuid(),
            EmailAddress = Faker.Internet.Email(),
            FirstName = Faker.Name.First(),
            MiddleName = Faker.Name.Middle(),
            LastName = Faker.Name.Last(),
            Created = created,
            Updated = created,
            DateOfBirth = new DateOnly(1969, 12, 1),
            Trn = trn,
            TrnAssociationSource = TrnAssociationSource.Api,
            TrnLookupStatus = TrnLookupStatus.Found,
            UserType = UserType.Teacher
        };

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
        });

        var dqtTeacher = new TeacherInfo()
        {
            Trn = trn,
            FirstName = Faker.Name.First(),
            MiddleName = Faker.Name.Middle(),
            LastName = Faker.Name.Last(),
            DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
            NationalInsuranceNumber = null,
            PendingNameChange = false,
            PendingDateOfBirthChange = false,
            Email = Faker.Internet.Email()
        };

        var dqtApiClient = Mock.Of<IDqtApiClient>();
        Mock.Get(dqtApiClient)
            .Setup(d => d.GetTeacherByTrn(trn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dqtTeacher);

        // Act
        var job = new SyncNamesWithDqtJob(
            _dbFixture.GetDbContextFactory(),
            dqtApiClient,
            _dbFixture.Clock);
        await job.Execute(CancellationToken.None);

        // Assert
        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            var updatedUser = await dbContext.Users.SingleAsync(r => r.Trn == trn);
            Assert.Equal(dqtTeacher.FirstName, updatedUser.FirstName);
            Assert.Equal(dqtTeacher.MiddleName, updatedUser.MiddleName);
            Assert.Equal(dqtTeacher.LastName, updatedUser.LastName);
            Assert.Equal(user.DateOfBirth, updatedUser.DateOfBirth); // i.e. doesn't get changed from DQT
            Assert.Equal(user.EmailAddress, updatedUser.EmailAddress); // i.e. doesn't get changed from DQT
            Assert.Equal(_dbFixture.Clock.UtcNow, updatedUser.Updated);

            var userUpdatedEvents = await dbContext.Events
                    .Where(e => e.EventName == "UserUpdatedEvent").ToListAsync();
            var userUpdatedEvent = userUpdatedEvents
                .Select(e => JsonSerializer.Deserialize<UserUpdatedEvent>(e.Payload))
                .Where(u => u!.User.Trn == trn)
                .SingleOrDefault();
            Assert.NotNull(userUpdatedEvent);
            Assert.Equal(dqtTeacher.FirstName, userUpdatedEvent.User.FirstName);
            Assert.Equal(dqtTeacher.MiddleName, userUpdatedEvent.User.MiddleName);
            Assert.Equal(dqtTeacher.LastName, userUpdatedEvent.User.LastName);
        });
    }

    [Fact]
    public async Task Execute_WhenDqtNamesAreTheSameAsIdentity_DoesNotUpdateUserOrInsertAnEvent()
    {
        // Arrange
        var trn = "1234567";
        var firstName = Faker.Name.First();
        var middleName = Faker.Name.Middle();
        var lastName = Faker.Name.Last();

        var created = _dbFixture.Clock.UtcNow.AddDays(-5);

        var user = new User
        {
            UserId = Guid.NewGuid(),
            EmailAddress = Faker.Internet.Email(),
            FirstName = firstName,
            MiddleName = middleName,
            LastName = lastName,
            Created = created,
            Updated = created,
            DateOfBirth = new DateOnly(1969, 12, 1),
            Trn = trn,
            TrnAssociationSource = TrnAssociationSource.Api,
            TrnLookupStatus = TrnLookupStatus.Found,
            UserType = UserType.Teacher
        };

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
        });

        var dqtTeacher = new TeacherInfo()
        {
            Trn = trn,
            FirstName = firstName,
            MiddleName = middleName,
            LastName = lastName,
            DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
            NationalInsuranceNumber = null,
            PendingNameChange = false,
            PendingDateOfBirthChange = false,
            Email = Faker.Internet.Email()
        };

        var dqtApiClient = Mock.Of<IDqtApiClient>();
        Mock.Get(dqtApiClient)
            .Setup(d => d.GetTeacherByTrn(trn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dqtTeacher);

        // Act
        var job = new SyncNamesWithDqtJob(
            _dbFixture.GetDbContextFactory(),
            dqtApiClient,
            _dbFixture.Clock);
        await job.Execute(CancellationToken.None);

        // Assert
        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            var updatedUser = await dbContext.Users.SingleAsync(r => r.Trn == trn);
            Assert.Equal(created, updatedUser.Updated);

            var userUpdatedEvents = await dbContext.Events
                    .Where(e => e.EventName == "UserUpdatedEvent").ToListAsync();
            var userUpdatedEvent = userUpdatedEvents
                .Select(e => JsonSerializer.Deserialize<UserUpdatedEvent>(e.Payload))
                .Where(u => u!.User.Trn == trn)
                .SingleOrDefault();
            Assert.Null(userUpdatedEvent);
        });
    }

    private async Task ClearNonTestUsers()
    {
        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            await TestUsers.DeleteNonTestUsers(dbContext);
        });
    }
}
