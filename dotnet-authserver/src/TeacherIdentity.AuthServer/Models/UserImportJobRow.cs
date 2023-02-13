namespace TeacherIdentity.AuthServer.Models;

public class UserImportJobRow
{
    public const int IdMaxLength = 100;

    public required Guid UserImportJobId { get; set; }
    public UserImportJob? UserImportJob { get; set; }
    public required int RowNumber { get; set; }
    public string? Id { get; set; }
    public string? RawData { get; set; }
    public Guid? UserId { get; set; }
    public List<string>? Errors { get; set; }
}
