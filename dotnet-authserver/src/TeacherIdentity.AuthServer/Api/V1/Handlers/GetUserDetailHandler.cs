using MediatR;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Api.V1.Requests;
using TeacherIdentity.AuthServer.Api.V1.Responses;
using TeacherIdentity.AuthServer.Api.Validation;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Api.V1.Handlers;

public class GetUserDetailHandler : IRequestHandler<GetUserDetailRequest, GetUserDetailResponse>
{
    private readonly TeacherIdentityServerDbContext _dbContext;

    public GetUserDetailHandler(TeacherIdentityServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetUserDetailResponse> Handle(GetUserDetailRequest request, CancellationToken cancellationToken)
    {
        // N.B. The UserType predicate is here to prevent GetAnIdentitySupport users being able to 'see' admins.
        // In future when use of this endpoint is expanded (for admins, say) then this predicate should be dynamic
        // based on the current scope.

        var user = await _dbContext.Users
            .Where(u => u.UserType == UserType.Default && u.UserId == request.UserId)
            .Select(u => new GetUserDetailResponse()
            {
                UserId = u.UserId,
                Email = u.EmailAddress,
                FirstName = u.FirstName,
                LastName = u.LastName,
                DateOfBirth = u.DateOfBirth,
                Trn = u.Trn,
                Created = u.Created,
                RegisteredWithClientId = u.RegisteredWithClientId,
                RegisteredWithClientDisplayName = u.RegisteredWithClient != null ? u.RegisteredWithClient.DisplayName : null
            })
            .SingleOrDefaultAsync();

        if (user is null)
        {
            throw new ErrorException(ErrorRegistry.UserNotFound());
        }

        return user;
    }
}
