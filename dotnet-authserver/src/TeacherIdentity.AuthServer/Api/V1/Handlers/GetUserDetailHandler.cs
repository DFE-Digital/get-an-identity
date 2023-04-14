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
            .IgnoreQueryFilters()
            .Include(u => u.MergedUsers)
            .Include(u => u.RegisteredWithClient)
            .Where(u => u.UserType == UserType.Default)
            .SingleOrDefaultAsync(u => u.UserId == request.UserId);

        if (user?.IsDeleted == true)
        {
            user = await _dbContext.Users
                .IgnoreQueryFilters()
                .Include(u => u.MergedUsers)
                .Include(u => u.RegisteredWithClient)
                .Where(u => u.UserType == UserType.Default && u.IsDeleted == false)
                .SingleOrDefaultAsync(u => u.UserId == user.MergedWithUserId);
        }

        if (user is null)
        {
            throw new ErrorException(ErrorRegistry.UserNotFound());
        }

        return new GetUserDetailResponse
        {
            Created = user.Created,
            RegisteredWithClientId = user.RegisteredWithClientId,
            RegisteredWithClientDisplayName = user.RegisteredWithClient?.DisplayName,
            UserId = user.UserId,
            Email = user.EmailAddress,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DateOfBirth = user.DateOfBirth,
            Trn = user.Trn,
            TrnLookupStatus = user.TrnLookupStatus,
            MobileNumber = user.MobileNumber,
            MergedUserIds = user.MergedUsers?.Select(mu => mu.UserId) ?? Enumerable.Empty<Guid>()
        };
    }
}
