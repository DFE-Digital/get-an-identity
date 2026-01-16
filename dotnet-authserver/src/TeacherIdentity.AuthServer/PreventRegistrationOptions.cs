using System.ComponentModel.DataAnnotations;

namespace TeacherIdentity.AuthServer;

public class PreventRegistrationOptions
{
    [Required]
    public Dictionary<string, string> ClientRedirects { get; set; } = new();
}
