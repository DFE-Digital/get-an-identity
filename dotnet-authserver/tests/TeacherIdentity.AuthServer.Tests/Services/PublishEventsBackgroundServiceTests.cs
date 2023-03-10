using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TeacherIdentity.AuthServer.EventProcessing;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.EventPublishing;

namespace TeacherIdentity.AuthServer.Tests.Services;

[Collection(nameof(DisableParallelization))]
public class PublishEventsBackgroundServiceTests : IClassFixture<DbFixture>, IAsyncLifetime
{
    private readonly DbFixture _dbFixture;

    public PublishEventsBackgroundServiceTests(DbFixture dbFixture)
    {
        _dbFixture = dbFixture;
    }

    public async Task InitializeAsync()
    {
        using var dbContext = _dbFixture.GetDbContext();
        await dbContext.Database.ExecuteSqlAsync($"delete from events");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task PublishEvents_PublishesUnpublishEventsAndSetsPublishedFlag()
    {
        // Arrange
        using var dbContext = _dbFixture.GetDbContext();
        var @event = CreateDummyEvent();
        dbContext.AddEvent(@event);
        await dbContext.SaveChangesAsync();
        var dbEvent = dbContext.ChangeTracker.Entries<Event>().Last().Entity;

        var eventObserver = new TestableEventObserver();

        var logger = new NullLogger<PublishEventsBackgroundService>();

        var service = new PublishEventsBackgroundService(eventObserver, _dbFixture.GetDbContextFactory(), logger);

        // Act
        await service.PublishEvents(CancellationToken.None);

        // Assert
        Assert.Collection(eventObserver.Events, e => e.Equals(@event));

        await dbContext.Entry(dbEvent).ReloadAsync();
        Assert.True(dbEvent.Published);
    }

    [Fact]
    public async Task PublishEvents_DoesNotPublishAlreadyPublishedEvent()
    {
        // Arrange
        using var dbContext = _dbFixture.GetDbContext();
        var @event = CreateDummyEvent();
        dbContext.AddEvent(@event);
        dbContext.ChangeTracker.Entries<Event>().Last().Entity.Published = true;
        await dbContext.SaveChangesAsync();

        var eventObserver = new TestableEventObserver();

        var logger = new NullLogger<PublishEventsBackgroundService>();

        var service = new PublishEventsBackgroundService(eventObserver, _dbFixture.GetDbContextFactory(), logger);

        // Act
        await service.PublishEvents(CancellationToken.None);

        // Assert
        Assert.Empty(eventObserver.Events);
    }

    [Fact]
    public async Task PublishEvents_EventObserverThrows_DoesNotThrow()
    {
        // Arrange
        using var dbContext = _dbFixture.GetDbContext();
        var @event = CreateDummyEvent();
        dbContext.AddEvent(@event);
        await dbContext.SaveChangesAsync();
        var dbEvent = dbContext.ChangeTracker.Entries<Event>().Last().Entity;

        var eventObserver = new Mock<IEventObserver>();
        var publishException = new Exception("Bang!");
        eventObserver.Setup(mock => mock.OnEventSaved(It.IsAny<EventBase>())).ThrowsAsync(publishException);

        var logger = new Mock<ILogger<PublishEventsBackgroundService>>();

        var service = new PublishEventsBackgroundService(eventObserver.Object, _dbFixture.GetDbContextFactory(), logger.Object);

        // Act
        await service.PublishEvents(CancellationToken.None);

        // Assert
        // Can't easily assert that logging has happened here due to extension methods
    }

    private EventBase CreateDummyEvent() => new UserUpdatedEvent()
    {
        Changes = UserUpdatedEventChanges.LastName,
        CreatedUtc = _dbFixture.Clock.UtcNow,
        Source = UserUpdatedEventSource.ChangedByUser,
        UpdatedByClientId = null,
        UpdatedByUserId = Guid.NewGuid(),
        User = new()
        {
            DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
            EmailAddress = Faker.Internet.Email(),
            FirstName = Faker.Name.First(),
            LastName = Faker.Name.Last(),
            StaffRoles = Array.Empty<string>(),
            Trn = null,
            TrnAssociationSource = null,
            TrnLookupStatus = null,
            UserId = Guid.NewGuid(),
            UserType = UserType.Default
        }
    };

    private class TestableEventObserver : IEventObserver
    {
        private readonly List<EventBase> _events = new();

        public IReadOnlyCollection<EventBase> Events => _events.AsReadOnly();

        public Task OnEventSaved(EventBase @event)
        {
            _events.Add(@event);
            return Task.CompletedTask;
        }
    }
}
