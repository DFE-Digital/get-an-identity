using System.ComponentModel.DataAnnotations;

namespace TeacherIdentity.AuthServer.Services.Establishment;

public class GiasOptions
{
    [Required]
    public required string BaseDownloadAddress { get; init; }
}
