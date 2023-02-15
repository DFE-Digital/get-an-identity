using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Models;

public class SmsConfirmationPin : IConfirmationPin
{
    public long SmsConfirmationPinId { get; set; }
    public required string MobileNumber { get; init; }
    public required string Pin { get; init; }
    public required DateTime Expires { get; init; }
    public required bool IsActive { get; set; }
    public DateTime? VerifiedOn { get; set; }
}
