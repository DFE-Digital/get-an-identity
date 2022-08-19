namespace TeacherIdentity.AuthServer.Models;

public class EmailConfirmationPin
{
    public long EmailConfirmationPinId { get; set; }
    public string Email { get; set; } = null!;
    public string Pin { get; set; } = null!;
    public DateTime Expires { get; set; }
    public bool IsActive { get; set; }
}
