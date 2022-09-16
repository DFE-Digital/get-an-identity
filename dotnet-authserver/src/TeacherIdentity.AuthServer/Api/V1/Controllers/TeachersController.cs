using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Api.V1.ApiModels;
using TeacherIdentity.AuthServer.Api.V1.Responses;
using TeacherIdentity.AuthServer.Infrastructure.Security;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Api.V1.Controllers;

[ApiController]
[Route("api/v1/teachers")]
[Authorize(AuthorizationPolicies.GetAnIdentitySupport)]
public class TeachersController : ControllerBase
{
    private readonly TeacherIdentityServerDbContext _dbContext;

    public TeachersController(TeacherIdentityServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("{teacherId}")]
    public async Task<IActionResult> GetTeacherDetail([FromRoute] Guid teacherId)
    {
        var teacher = await _dbContext.Users
            .Where(u => u.UserType == UserType.Teacher && u.UserId == teacherId)
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
    public async Task<IActionResult> GetAllTeachers()
    {
        var teachers = await _dbContext.Users
            .Where(u => u.UserType == UserType.Teacher)
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
}
