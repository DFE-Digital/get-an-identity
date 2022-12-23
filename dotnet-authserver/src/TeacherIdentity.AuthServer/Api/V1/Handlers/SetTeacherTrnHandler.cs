using MediatR;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Api.V1.Requests;
using TeacherIdentity.AuthServer.Api.Validation;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Api.V1.Handlers;

public class SetTeacherTrnHandler : IRequestHandler<SetTeacherTrnRequest>
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;

    public SetTeacherTrnHandler(
        TeacherIdentityServerDbContext dbContext,
        IClock clock)
    {
        _dbContext = dbContext;
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

        var trn = request.Body.Trn;

        if ((trn is not null && user.Trn == trn) ||
            (trn is null && user.TrnLookupStatus == TrnLookupStatus.Failed))
        {
            // Nothing is changing
            return Unit.Value;
        }

        if (user.Trn is not null)
        {
            throw new ErrorException(ErrorRegistry.UserAlreadyHasTrnAssigned());
        }

        var changes = Events.UserUpdatedEventChanges.None;

        if (trn is null)
        {
            user.TrnLookupStatus = TrnLookupStatus.Failed;
            changes = Events.UserUpdatedEventChanges.TrnLookupStatus;
        }
        else
        {
            user.Trn = trn;
            user.TrnLookupStatus = TrnLookupStatus.Found;
            user.TrnAssociationSource = TrnAssociationSource.Api;
            changes = Events.UserUpdatedEventChanges.Trn | Events.UserUpdatedEventChanges.TrnLookupStatus;
        }

        user.Updated = _clock.UtcNow;

        _dbContext.AddEvent(new Events.UserUpdatedEvent()
        {
            Source = Events.UserUpdatedEventSource.Api,
            CreatedUtc = _clock.UtcNow,
            Changes = changes,
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

        return Unit.Value;
    }
}
