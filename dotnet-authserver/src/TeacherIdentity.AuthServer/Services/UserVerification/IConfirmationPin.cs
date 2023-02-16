namespace TeacherIdentity.AuthServer.Services.UserVerification;

public interface IConfirmationPin
{
    public string Pin { get; init; }
    public DateTime Expires { get; init; }
    public bool IsActive { get; set; }
    public DateTime? VerifiedOn { get; set; }
}
