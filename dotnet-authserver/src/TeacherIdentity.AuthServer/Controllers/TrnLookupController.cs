using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeacherIdentity.AuthServer.Infrastructure.Security;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Controllers;

[ApiController]
[Route("api/find-trn")]
[Authorize(AuthorizationPolicies.TrnLookupApi)]
public class TrnLookupController : ControllerBase
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IClock _clock;

    public TrnLookupController(
        TeacherIdentityServerDbContext dbContext,
        IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    [HttpPut("user/{journeyId}")]
    public async Task<IActionResult> SetJourneyTrnLookupStateUser(
        [FromRoute] Guid journeyId,
        [FromBody] SetJourneyTrnLookupStateRequest request)
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

public class SetJourneyTrnLookupStateRequest
{
    [Required]
    public string FirstName { get; set; } = null!;
    [Required]
    public string LastName { get; set; } = null!;
    [Required]
    public DateOnly DateOfBirth { get; set; }
    [StringLength(maximumLength: 7, MinimumLength = 7)]
    public string? Trn { get; set; }
}
