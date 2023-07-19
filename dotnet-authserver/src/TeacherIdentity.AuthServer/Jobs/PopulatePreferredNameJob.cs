using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Jobs;

public class PopulatePreferredNameJob : IDisposable
{
    private readonly TeacherIdentityServerDbContext _readDbContext;
    private readonly TeacherIdentityServerDbContext _writeDbContext;
    private readonly IClock _clock;

    public PopulatePreferredNameJob(
        IDbContextFactory<TeacherIdentityServerDbContext> dbContextFactory,
        IClock clock)
    {
        _readDbContext = dbContextFactory.CreateDbContext();
        _writeDbContext = dbContextFactory.CreateDbContext();
        _clock = clock;
    }

    public async Task Execute(CancellationToken cancellationToken)
    {
        await foreach (var userWithoutPreferredName in _readDbContext.Users.Where(u => u.UserType != UserType.Staff && string.IsNullOrEmpty(u.PreferredName)).AsNoTracking().AsAsyncEnumerable())
        {
            var userToUpdate = await _writeDbContext.Users.Where(u => u.UserId == userWithoutPreferredName.UserId).SingleAsync();
            userToUpdate.PreferredName = $"{userWithoutPreferredName.FirstName} {userWithoutPreferredName.LastName}";
            userToUpdate.Updated = _clock.UtcNow;

            _writeDbContext.AddEvent(new UserUpdatedEvent()
            {
                Source = UserUpdatedEventSource.System,
                UpdatedByClientId = null,
                UpdatedByUserId = null,
                CreatedUtc = userToUpdate.Updated,
                User = Events.User.FromModel(userToUpdate),
                Changes = UserUpdatedEventChanges.PreferredName
            });

            await _writeDbContext.SaveChangesAsync(cancellationToken);
        }
    }


    public void Dispose()
    {
        ((IDisposable)_readDbContext).Dispose();
        ((IDisposable)_writeDbContext).Dispose();
    }
}
