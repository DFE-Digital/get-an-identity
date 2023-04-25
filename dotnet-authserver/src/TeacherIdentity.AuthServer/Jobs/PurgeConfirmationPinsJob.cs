using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Jobs;

public class PurgeConfirmationPinsJob
{
    private static readonly TimeSpan _expiredPinGracePeriod = TimeSpan.FromDays(7);

    private readonly TeacherIdentityServerDbContext _dbContext;

    public PurgeConfirmationPinsJob(TeacherIdentityServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Execute(CancellationToken cancellationToken)
    {
        await DeleteEmailConfirmationPins(cancellationToken);
        await DeleteSmsConfirmationPins(cancellationToken);
    }

    private async Task DeleteEmailConfirmationPins(CancellationToken cancellationToken)
    {
        var expiredEmailConfirmationPins = _dbContext.EmailConfirmationPins
            .Where(p => p.Expires < DateTime.UtcNow - _expiredPinGracePeriod)
            .ToList();

        if (expiredEmailConfirmationPins.Any())
        {
            _dbContext.EmailConfirmationPins.RemoveRange(expiredEmailConfirmationPins);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task DeleteSmsConfirmationPins(CancellationToken cancellationToken)
    {
        var expiredSmsConfirmationPins = _dbContext.SmsConfirmationPins
            .Where(p => p.Expires < DateTime.UtcNow - _expiredPinGracePeriod)
            .ToList();

        if (expiredSmsConfirmationPins.Any())
        {
            _dbContext.SmsConfirmationPins.RemoveRange(expiredSmsConfirmationPins);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
