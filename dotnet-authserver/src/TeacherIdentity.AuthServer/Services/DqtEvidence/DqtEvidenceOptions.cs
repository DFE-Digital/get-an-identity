using System.ComponentModel.DataAnnotations;

namespace TeacherIdentity.AuthServer.Services.DqtEvidence;

public class DqtEvidenceOptions
{
    [Required]
    public required string StorageContainerName { get; init; }
}
