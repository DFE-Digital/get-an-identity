using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using TeacherIdentity.AuthServer.ApiModels;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Controllers.Api;

[ApiController]
[Route("api/find-trn")]
[Authorize("TrnLookup")]
public class FindALostTrnIntegrationController : ControllerBase
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;

    public FindALostTrnIntegrationController(
        TeacherIdentityServerDbContext dbContext,
        IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    [HttpPut("user/{journeyId}")]
    public async Task<IActionResult> SetJourneyFindALostTrnUser(
        [FromRoute] Guid journeyId,
        [FromBody] SetJourneyFindALostTrnUserRequest request)
    {
        try
        {
            _dbContext.JourneyTrnLookupStates.Add(new JourneyTrnLookupState()
            {
                Created = _clock.UtcNow,
                JourneyId = journeyId,
                DateOfBirth = request.DateOfBirth,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Trn = request.Trn
            });

            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex) when (
            ex.InnerException is PostgresException postgresException &&
            postgresException.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return BadRequest();  // TODO Better error message
        }
        return NoContent();
    }
}
