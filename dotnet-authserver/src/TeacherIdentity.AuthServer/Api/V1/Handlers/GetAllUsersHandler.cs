using MediatR;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Api.V1.ApiModels;
using TeacherIdentity.AuthServer.Api.V1.Requests;
using TeacherIdentity.AuthServer.Api.V1.Responses;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Api.V1.Handlers;

public class GetAllUsersHandler : IRequestHandler<GetAllUsersRequest, GetAllUsersResponse>
{
    private readonly TeacherIdentityServerDbContext _dbContext;

    public GetAllUsersHandler(TeacherIdentityServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetAllUsersResponse> Handle(GetAllUsersRequest request, CancellationToken cancellationToken)
    {
        var pageSize = (request?.PageSize) ?? 25;
        var skip = ((request?.PageNumber ?? 1) - 1) * pageSize;

        // N.B. The UserType predicate is here to prevent GetAnIdentitySupport users being able to 'see' admins.
        // In future when use of this endpoint is expanded (for admins, say) then this predicate should be dynamic
        // based on the current scope.
        var allUsers = _dbContext.Users.Where(u => u.UserType == UserType.Default);

        if (request?.TrnLookupStatus is not null)
        {
            allUsers = allUsers.Where(u => request.TrnLookupStatus.Contains(u.TrnLookupStatus!.Value));
        }

        var total = await allUsers.CountAsync();

        var users = await allUsers
            .Where(u => u.UserType == UserType.Default)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Skip(skip)
            .Take(pageSize)
            .Select(u => new UserInfo()
            {
                UserId = u.UserId,
                Email = u.EmailAddress,
                FirstName = u.FirstName,
                LastName = u.LastName,
                DateOfBirth = u.DateOfBirth,
                Trn = u.Trn
            })
            .ToArrayAsync();

        return new GetAllUsersResponse() { Users = users, Total = total };
    }
}
