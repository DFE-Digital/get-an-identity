namespace TeacherIdentity.AuthServer.Api.Validation;

public class ErrorDescriptor
{
    public ErrorDescriptor(int errorCode, string title)
    {
        ErrorCode = errorCode;
        Title = title;
    }

    public int ErrorCode { get; }
    public string Title { get; }
    public string? Detail { get; init; }
}
