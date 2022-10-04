namespace TeacherIdentity.AuthServer.Models;

public class EmailConfirmationPin
{
    public long EmailConfirmationPinId { get; set; }
    public required string Email { get; init; }
    public required string Pin { get; init; }
    public required DateTime Expires { get; init; }
    public required bool IsActive { get; set; }
    public DateTime? VerifiedOn { get; set; }
}
