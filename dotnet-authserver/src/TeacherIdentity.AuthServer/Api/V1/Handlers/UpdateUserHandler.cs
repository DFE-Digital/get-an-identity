using MediatR;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Api.V1.ApiModels;
using TeacherIdentity.AuthServer.Api.V1.Requests;
using TeacherIdentity.AuthServer.Api.Validation;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Api.V1.Handlers;

public class UpdateUserHandler : IRequestHandler<UpdateUserRequest, UserInfo>
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;
    private readonly ICurrentUserProvider _currentUserProvider;

    public UpdateUserHandler(
        TeacherIdentityServerDbContext dbContext,
        IClock clock,
        ICurrentUserProvider currentUserProvider)
    {
        _dbContext = dbContext;
        _clock = clock;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<UserInfo> Handle(UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.UserId == request.UserId);

        if (user is null)
        {
            throw new ErrorException(ErrorRegistry.UserNotFound());
        }

        if (user.UserType != UserType.Default)
        {
            throw new ErrorException(ErrorRegistry.YouAreNotAuthorizedToPerformThisAction());
        }

        UserUpdatedEventChanges changes = UserUpdatedEventChanges.None;

        if (request.Body.EmailSet && request.Body.Email != user.EmailAddress)
        {
            user.EmailAddress = request.Body.Email!;
            changes |= UserUpdatedEventChanges.EmailAddress;
        }

        if (request.Body.FirstNameSet && request.Body.FirstName != user.FirstName)
        {
            user.FirstName = request.Body.FirstName!;
            changes |= UserUpdatedEventChanges.FirstName;
        }

        if (request.Body.LastNameSet && request.Body.LastName != user.LastName)
        {
            user.LastName = request.Body.LastName!;
            changes |= UserUpdatedEventChanges.LastName;
        }

        if (changes != UserUpdatedEventChanges.None)
        {
            user.Updated = _clock.UtcNow;

            _dbContext.AddEvent(new UserUpdatedEvent()
            {
                Source = UserUpdatedEventSource.Api,
                CreatedUtc = _clock.UtcNow,
                Changes = changes,
                User = Events.User.FromModel(user),
                UpdatedByClientId = _currentUserProvider.CurrentClientId,
                UpdatedByUserId = _currentUserProvider.CurrentUserId
            });

            await _dbContext.SaveChangesAsync();
        }

        return new UserInfo()
        {
            DateOfBirth = user.DateOfBirth,
            Email = user.EmailAddress,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Trn = user.Trn,
            UserId = user.UserId
        };
    }
}
