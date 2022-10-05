using MediatR;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Api.V1.ApiModels;
using TeacherIdentity.AuthServer.Api.V1.Requests;
using TeacherIdentity.AuthServer.Api.Validation;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Api.V1.Handlers;

public class UpdateUserHandler : IRequestHandler<UpdateUserRequest, UserInfo>
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;

    public UpdateUserHandler(TeacherIdentityServerDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
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

        var updateMade = false;

        if (request.Body.EmailSet)
        {
            user.EmailAddress = request.Body.Email!;
            updateMade = true;
        }

        if (request.Body.FirstNameSet)
        {
            user.FirstName = request.Body.FirstName!;
            updateMade = true;
        }

        if (request.Body.LastNameSet)
        {
            user.LastName = request.Body.LastName!;
            updateMade = true;
        }

        if (updateMade)
        {
            user.Updated = _clock.UtcNow;
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
