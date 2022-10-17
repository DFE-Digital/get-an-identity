using MediatR;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Api.V1.Requests;
using TeacherIdentity.AuthServer.Api.Validation;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.BackgroundJobs;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Api.V1.Handlers;

public class SetTeacherTrnHandler : IRequestHandler<SetTeacherTrnRequest>
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IBackgroundJobScheduler _backgroundJobScheduler;
    private readonly IClock _clock;

    public SetTeacherTrnHandler(
        TeacherIdentityServerDbContext dbContext,
        IBackgroundJobScheduler backgroundJobScheduler,
        IClock clock)
    {
        _dbContext = dbContext;
        _backgroundJobScheduler = backgroundJobScheduler;
        _clock = clock;
    }

    public async Task<Unit> Handle(SetTeacherTrnRequest request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.UserId == request.UserId);

        if (user is null)
        {
            throw new ErrorException(ErrorRegistry.UserNotFound());
        }

        if (user.UserType != UserType.Default)
        {
            throw new ErrorException(ErrorRegistry.UserMustBeTeacher());
        }

        if (user.Trn == request.Body.Trn)
        {
            return Unit.Value;
        }

        user.Trn = request.Body.Trn;
        user.Updated = _clock.UtcNow;

        _dbContext.AddEvent(new Events.UserUpdatedEvent()
        {
            Source = Events.UserUpdatedEventSource.Api,
            CreatedUtc = _clock.UtcNow,
            Changes = Events.UserUpdatedEventChanges.Trn,
            User = Events.User.FromModel(user)
        });

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException dex) when (dex.IsUniqueIndexViolation("ix_users_trn"))
        {
            throw new ErrorException(ErrorRegistry.TrnIsAssignedToAnotherUser());
        }

        await _backgroundJobScheduler.Enqueue<IDqtApiClient>(
            dqtApiClient => dqtApiClient.SetTeacherIdentityInfo(new DqtTeacherIdentityInfo()
            {
                Trn = user.Trn!,
                UserId = request.UserId
            }));

        return Unit.Value;
    }
}
