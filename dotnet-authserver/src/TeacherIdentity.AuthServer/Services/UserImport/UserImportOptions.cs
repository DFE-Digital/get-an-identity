using System.ComponentModel.DataAnnotations;

namespace TeacherIdentity.AuthServer.Services.UserImport;

public class UserImportOptions
{
    [Required]
    public required string StorageContainerName { get; init; }
}
