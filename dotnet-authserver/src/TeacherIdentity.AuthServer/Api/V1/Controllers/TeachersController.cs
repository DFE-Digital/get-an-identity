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
[Route("teachers")]
[Authorize(AuthorizationPolicies.GetAnIdentitySupport)]
public class TeachersController : ControllerBase
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IBackgroundJobScheduler _backgroundJobScheduler;

    public TeachersController(
        TeacherIdentityServerDbContext dbContext,
        IBackgroundJobScheduler backgroundJobScheduler)
    {
        _dbContext = dbContext;
        _backgroundJobScheduler = backgroundJobScheduler;
    }

    [HttpGet("{teacherId}")]
    [SwaggerOperation(summary: "Get a teacher's details by their user ID")]
    [ProducesResponseType(typeof(GetTeacherDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTeacherDetail([FromRoute] Guid teacherId)
    {
        var teacher = await _dbContext.Users
            .Where(u => u.UserType == UserType.Default && u.UserId == teacherId)
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

        if (teacher is null)
        {
            return NotFound();
        }


        return Ok(teacher);
    }

    [HttpGet("")]
    [SwaggerOperation(summary: "Retrieves all teachers")]
    [ProducesResponseType(typeof(GetAllTeachersResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllTeachers()
    {
        var teachers = await _dbContext.Users
            .Where(u => u.UserType == UserType.Default)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Select(u => new TeacherInfo()
            {
                UserId = u.UserId,
                Email = u.EmailAddress,
                FirstName = u.FirstName,
                LastName = u.LastName,
                DateOfBirth = u.DateOfBirth,
                Trn = u.Trn
            })
            .ToArrayAsync();

        return Ok(new GetAllTeachersResponse() { Teachers = teachers });
    }

    [HttpPut("{teacherId}/trn")]
    [SwaggerOperation(summary: "Set the TRN for a teacher")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetTeacherTrn(
        [FromRoute] Guid teacherId,
        [FromBody] SetTeacherTrnRequest request)
    {
        // This will move into a FluentValidation validator shortly
        if (request.Trn is null || request.Trn.Length != 7 || !request.Trn.All(c => c >= '0' && c <= '9'))
        {
            return BadRequest();
        }

        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.UserId == teacherId);

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
                UserId = teacherId
            }));

        return NoContent();
    }
}
