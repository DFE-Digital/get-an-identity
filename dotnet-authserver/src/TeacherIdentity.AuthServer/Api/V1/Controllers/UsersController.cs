using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using TeacherIdentity.AuthServer.Api.V1.ApiModels;
using TeacherIdentity.AuthServer.Api.V1.Requests;
using TeacherIdentity.AuthServer.Api.V1.Responses;
using TeacherIdentity.AuthServer.Api.Validation;
using TeacherIdentity.AuthServer.Infrastructure.Security;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.BackgroundJobs;
using TeacherIdentity.AuthServer.Services.DqtApi;

namespace TeacherIdentity.AuthServer.Api.V1.Controllers;

[ApiController]
[Route("users")]
[Route("teachers")]
[Authorize(AuthorizationPolicies.GetAnIdentitySupport)]
public class UsersController : ControllerBase
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IBackgroundJobScheduler _backgroundJobScheduler;

    public UsersController(
        TeacherIdentityServerDbContext dbContext,
        IBackgroundJobScheduler backgroundJobScheduler)
    {
        _dbContext = dbContext;
        _backgroundJobScheduler = backgroundJobScheduler;
    }

    [HttpGet("{userId}")]
    [SwaggerOperation(summary: "Get a user's details by their user ID")]
    [ProducesResponseType(typeof(GetTeacherDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserDetail([FromRoute] Guid userId)
    {
        // N.B. The UserType predicate is here to prevent GetAnIdentitySupport users being able to 'see' admins.
        // In future when use of this endpoint is expanded (for admins, say) then this predicate should be dynamic
        // based on the current scope.

        var user = await _dbContext.Users
            .Where(u => u.UserType == UserType.Default && u.UserId == userId)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Select(u => new GetTeacherDetailResponse()
            {
                UserId = u.UserId,
                Email = u.EmailAddress,
                FirstName = u.FirstName,
                LastName = u.LastName,
                DateOfBirth = u.DateOfBirth,
                Trn = u.Trn
            })
            .SingleOrDefaultAsync();

        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpGet("")]
    [SwaggerOperation(summary: "Retrieves all users")]
    [ProducesResponseType(typeof(GetAllUsersResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllUsers()
    {
        // N.B. The UserType predicate is here to prevent GetAnIdentitySupport users being able to 'see' admins.
        // In future when use of this endpoint is expanded (for admins, say) then this predicate should be dynamic
        // based on the current scope.

        var users = await _dbContext.Users
            .Where(u => u.UserType == UserType.Default)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
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

        return Ok(new GetAllUsersResponse() { Users = users });
    }

    [HttpPut("{userId}/trn")]
    [SwaggerOperation(summary: "Set the TRN for a teacher")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetTeacherTrn(
        [FromRoute] Guid userId,
        [FromBody] SetTeacherTrnRequest request)
    {
        // This will move into a FluentValidation validator shortly
        if (request.Trn is null || request.Trn.Length != 7 || !request.Trn.All(c => c >= '0' && c <= '9'))
        {
            return BadRequest();
        }

        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.UserId == userId);

        if (user is null)
        {
            return NotFound();
        }

        if (user.UserType != UserType.Default)
        {
            throw new ErrorException(ErrorRegistry.UserMustBeTeacher());
        }

        user.Trn = request.Trn;

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
                Trn = request.Trn!,
                UserId = userId
            }));

        return NoContent();
    }
}
