using System.ComponentModel.DataAnnotations;

namespace TeacherIdentity.AuthServer.Services.UserVerification;

public class FixedPinUserVerificationOptions
{
    [Required]
    [StringLength(maximumLength: 5, MinimumLength = 5)]
    public required string Pin { get; set; }
}
