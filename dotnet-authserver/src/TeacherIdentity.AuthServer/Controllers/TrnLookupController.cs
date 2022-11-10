using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
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
        [FromBody] SetJourneyTrnLookupStateRequestBody request)
    {
        var existingState = await _dbContext.JourneyTrnLookupStates.FindAsync(journeyId);

        if (existingState?.Locked is not null)
        {
            return BadRequest("Journey is locked.");
        }

        var normalizedNino = (request.NationalInsuranceNumber ?? string.Empty)
            .ToUpper()
            .Replace(" ", "");

        if (existingState is null)
        {
            _dbContext.JourneyTrnLookupStates.Add(new JourneyTrnLookupState()
            {
                Created = _clock.UtcNow,
                JourneyId = journeyId,
                DateOfBirth = request.DateOfBirth,
                OfficialFirstName = request.OfficialFirstName!,
                OfficialLastName = request.OfficialLastName!,
                Trn = request.Trn,
                NationalInsuranceNumber = normalizedNino,
                PreferredFirstName = request.PreferredFirstName!,
                PreferredLastName = request.PreferredLastName!,
            });
        }
        else
        {
            existingState.DateOfBirth = request.DateOfBirth;
            existingState.OfficialFirstName = request.OfficialFirstName!;
            existingState.OfficialLastName = request.OfficialLastName!;
            existingState.Trn = request.Trn;
            existingState.NationalInsuranceNumber = normalizedNino;
            existingState.PreferredFirstName = request.PreferredFirstName!;
            existingState.PreferredLastName = request.PreferredLastName!;
        }

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}

public record SetJourneyTrnLookupStateRequestBody : IValidatableObject
{
    public string? FirstName
    {
        get => OfficialFirstName;
        set => OfficialFirstName = value;
    }

    public string? LastName
    {
        get => OfficialLastName;
        set => OfficialLastName = value;
    }

    [Required]
    public string? OfficialFirstName { get; set; }
    [Required]
    public string? OfficialLastName { get; set; }
    [Required]
    public DateOnly DateOfBirth { get; set; }
    [StringLength(maximumLength: 7, MinimumLength = 7)]
    public string? Trn { get; set; }
    public string? NationalInsuranceNumber { get; set; }
    public string? PreferredFirstName { get; set; }
    public string? PreferredLastName { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if ((string.IsNullOrEmpty(PreferredFirstName) && !string.IsNullOrEmpty(PreferredLastName)) ||
            (!string.IsNullOrEmpty(PreferredFirstName) && string.IsNullOrEmpty(PreferredLastName)))
        {
            yield return new ValidationResult("Preferred name cannot be partially specified.");
        }
    }
}
