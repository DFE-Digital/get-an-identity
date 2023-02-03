namespace TeacherIdentity.AuthServer.Models;

public class UserImportJob
{
    public required Guid UserImportJobId { get; set; }
    public required string StoredFilename { get; set; }
    public required string OriginalFilename { get; set; }
    public required UserImportJobStatus UserImportJobStatus { get; set; }
    public required DateTime Uploaded { get; set; }
    public DateTime? Imported { get; set; }
}
