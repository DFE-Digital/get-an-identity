using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace TeacherIdentity.AuthServer.Api.Endpoints;

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
        if (string.IsNullOrEmpty(PreferredFirstName) && !string.IsNullOrEmpty(PreferredLastName) ||
            !string.IsNullOrEmpty(PreferredFirstName) && string.IsNullOrEmpty(PreferredLastName))
        {
            yield return new ValidationResult("Preferred name cannot be partially specified.");
        }
    }
}
