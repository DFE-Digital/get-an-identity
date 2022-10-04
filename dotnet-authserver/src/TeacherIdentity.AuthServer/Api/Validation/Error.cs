namespace TeacherIdentity.AuthServer.Api.Validation;

public sealed class Error
{
    private readonly ErrorDescriptor _descriptor;

    public Error(ErrorDescriptor descriptor)
    {
        _descriptor = descriptor;
    }

    public int ErrorCode => _descriptor.ErrorCode;

    public string Title => _descriptor.Title;

    public string? Detail { get; init; }
}
