using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Jobs;
using TeacherIdentity.AuthServer.Models;
using User = TeacherIdentity.AuthServer.Models.User;

namespace TeacherIdentity.AuthServer.Tests.Jobs;

[Collection(nameof(DisableParallelization))]
public class PopulatePreferredNameJobTests : IClassFixture<DbFixture>, IAsyncLifetime
{
    private readonly DbFixture _dbFixture;

    public PopulatePreferredNameJobTests(DbFixture dbFixture)
    {
        _dbFixture = dbFixture;
    }

    public async Task InitializeAsync()
    {
        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            await TestUsers.DeleteNonTestUsers(dbContext);
            await dbContext.Database.ExecuteSqlAsync($"truncate table events");
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Execute_WhenPreferredNameIsNullAndUserTypeIsTeacher_UpdatesUserAndInsertsEvent()
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

        // Act
        var job = new PopulatePreferredNameJob(
            _dbFixture.GetDbContextFactory(),
            _dbFixture.Clock);
        await job.Execute(CancellationToken.None);

        // Assert
        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            var updatedUser = await dbContext.Users.SingleAsync(r => r.UserId == user.UserId);
            Assert.Equal($"{user.FirstName} {user.LastName}", updatedUser.PreferredName);
            Assert.Equal(_dbFixture.Clock.UtcNow, updatedUser.Updated);

            var userUpdatedEvents = await dbContext.Events
                    .Where(e => e.EventName == "UserUpdatedEvent").ToListAsync();
            var userUpdatedEvent = userUpdatedEvents
                .Select(e => JsonSerializer.Deserialize<UserUpdatedEvent>(e.Payload))
                .Where(u => u!.User.Trn == trn)
                .SingleOrDefault();
            Assert.NotNull(userUpdatedEvent);
            Assert.Equal($"{user.FirstName} {user.LastName}", userUpdatedEvent.User.PreferredName);
        });
    }

    [Fact]
    public async Task Execute_WhenPreferredNameIsAlreadySetAndUserTypeIsTeacher_DoesNotUpdateUserOrInsertAnEvent()
    {
        // Arrange
        var created = _dbFixture.Clock.UtcNow.AddDays(-5);
        var user = new User
        {
            UserId = Guid.NewGuid(),
            EmailAddress = Faker.Internet.Email(),
            FirstName = Faker.Name.First(),
            MiddleName = Faker.Name.Middle(),
            LastName = Faker.Name.Last(),
            PreferredName = Faker.Name.FullName(),
            Created = created,
            Updated = created,
            DateOfBirth = new DateOnly(1969, 12, 1),
            UserType = UserType.Teacher
        };

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
        });

        // Act
        var job = new PopulatePreferredNameJob(
            _dbFixture.GetDbContextFactory(),
            _dbFixture.Clock);
        await job.Execute(CancellationToken.None);

        // Assert
        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            var nonUpdatedUser = await dbContext.Users.SingleAsync(r => r.UserId == user.UserId);
            Assert.Equal(user.PreferredName, nonUpdatedUser.PreferredName);
            Assert.Equal(created, nonUpdatedUser.Updated);

            var userUpdatedEvents = await dbContext.Events
                    .Where(e => e.EventName == "UserUpdatedEvent").ToListAsync();
            var userUpdatedEvent = userUpdatedEvents
                .Select(e => JsonSerializer.Deserialize<UserUpdatedEvent>(e.Payload))
                .Where(u => u!.User.UserId == user.UserId)
                .SingleOrDefault();
            Assert.Null(userUpdatedEvent);
        });
    }

    [Fact]
    public async Task Execute_WhenPreferredNameIsNullAndUserTypeIsStaff_DoesNotUpdateUserOrInsertAnEvent()
    {
        // Arrange
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
            DateOfBirth = null,
            UserType = UserType.Staff
        };

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
        });

        // Act
        var job = new PopulatePreferredNameJob(
            _dbFixture.GetDbContextFactory(),
            _dbFixture.Clock);
        await job.Execute(CancellationToken.None);

        // Assert
        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            var nonUpdatedUser = await dbContext.Users.SingleAsync(r => r.UserId == user.UserId);
            Assert.Null(nonUpdatedUser.PreferredName);
            Assert.Equal(created, nonUpdatedUser.Updated);

            var userUpdatedEvents = await dbContext.Events
                    .Where(e => e.EventName == "UserUpdatedEvent").ToListAsync();
            var userUpdatedEvent = userUpdatedEvents
                .Select(e => JsonSerializer.Deserialize<UserUpdatedEvent>(e.Payload))
                .Where(u => u!.User.UserId == user.UserId)
                .SingleOrDefault();
            Assert.Null(userUpdatedEvent);
        });
    }
}
