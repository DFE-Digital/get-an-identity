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

    public SetTeacherTrnHandler(
        TeacherIdentityServerDbContext dbContext,
        IBackgroundJobScheduler backgroundJobScheduler)
    {
        _dbContext = dbContext;
        _backgroundJobScheduler = backgroundJobScheduler;
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

        user.Trn = request.Body!.Trn!;

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
