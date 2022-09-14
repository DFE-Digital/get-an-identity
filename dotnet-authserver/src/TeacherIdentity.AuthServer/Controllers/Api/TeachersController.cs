using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.ApiModels;
using TeacherIdentity.AuthServer.Infrastructure.Security;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Controllers.Api;

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

    [HttpGet("")]
    public async Task<IActionResult> GetTeachers()
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
                DateOfBirth = u.DateOfBirth
            })
            .ToArrayAsync();

        return Ok(new GetTeachersResponse() { Teachers = teachers });
    }
}
