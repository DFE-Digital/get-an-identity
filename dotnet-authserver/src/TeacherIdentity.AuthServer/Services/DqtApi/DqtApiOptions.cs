using System.ComponentModel.DataAnnotations;

namespace TeacherIdentity.AuthServer.Services.DqtApi;

public class DqtApiOptions
{
    [Required]
    public string ApiKey { get; set; } = null!;

    [Required]
    public string BaseAddress { get; set; } = null!;
}
