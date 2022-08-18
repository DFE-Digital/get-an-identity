using System.ComponentModel.DataAnnotations;

namespace TeacherIdentity.AuthServer.ApiModels;

public class SetJourneyFindALostTrnUserRequest
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
