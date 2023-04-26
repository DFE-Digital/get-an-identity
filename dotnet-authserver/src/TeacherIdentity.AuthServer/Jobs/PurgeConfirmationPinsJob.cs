using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Jobs;

public class PurgeConfirmationPinsJob
{
    private static readonly TimeSpan _expiredPinGracePeriod = TimeSpan.FromDays(7);

    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;

    public PurgeConfirmationPinsJob(
        TeacherIdentityServerDbContext dbContext,
        IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public async Task Execute(CancellationToken cancellationToken)
    {
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM email_confirmation_pins WHERE expires < {_clock.UtcNow - _expiredPinGracePeriod}", cancellationToken);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM sms_confirmation_pins WHERE expires < {_clock.UtcNow - _expiredPinGracePeriod}", cancellationToken);
    }
}
