using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        var existingState = await _dbContext.JourneyTrnLookupStates.FindAsync(journeyId);

        if (existingState?.Locked is not null)
        {
            return BadRequest();
        }

        if (existingState is null)
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
        }
        else
        {
            existingState.DateOfBirth = request.DateOfBirth;
            existingState.FirstName = request.FirstName;
            existingState.LastName = request.LastName;
            existingState.Trn = request.Trn;
        }

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
