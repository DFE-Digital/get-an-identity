using System.ComponentModel.DataAnnotations;

namespace TeacherIdentity.AuthServer.Services.DqtApi;

public class DqtEvidenceOptions
{
    [Required]
    public required string StorageConnectionString { get; init; }

    [Required]
    public required string StorageContainerName { get; init; }
}
