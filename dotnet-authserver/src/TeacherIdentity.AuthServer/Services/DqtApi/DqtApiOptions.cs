using System.ComponentModel.DataAnnotations;

namespace TeacherIdentity.AuthServer.Services.DqtApi;

public class DqtApiOptions
{
    [Required]
    public required string ApiKey { get; init; }

    [Required]
    public required string BaseAddress { get; init; }
}
