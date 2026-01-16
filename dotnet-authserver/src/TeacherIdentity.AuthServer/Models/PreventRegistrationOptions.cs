using System.ComponentModel.DataAnnotations;

namespace TeacherIdentity.AuthServer.Models;

public class PreventRegistrationOptions
{
    [Required]
    public Dictionary<string, string> ClientRedirects { get; set; } = new();
}
