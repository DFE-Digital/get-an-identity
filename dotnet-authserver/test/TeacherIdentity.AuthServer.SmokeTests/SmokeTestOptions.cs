using System.ComponentModel.DataAnnotations;

namespace TeacherIdentity.AuthServer.SmokeTests;

public class SmokeTestOptions
{
    [Required]
    public string BaseUrl { get; set; } = default!;
}
