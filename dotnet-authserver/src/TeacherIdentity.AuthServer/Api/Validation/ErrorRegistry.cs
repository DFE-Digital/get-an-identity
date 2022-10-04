namespace TeacherIdentity.AuthServer.Api.Validation;

public static class ErrorRegistry
{
    private static readonly Dictionary<int, ErrorDescriptor> _all = new ErrorDescriptor[]
    {
        new ErrorDescriptor(10001, "UserType must be Teacher"),
        new ErrorDescriptor(10002, "TRN is assigned to another user"),
        new ErrorDescriptor(10004, "Request is not valid"),
    }.ToDictionary(d => d.ErrorCode, d => d);

    public static Error UserMustBeTeacher() => CreateError(10001);

    public static Error TrnIsAssignedToAnotherUser() => CreateError(10002);

    public static Error RequestIsNotValid() => CreateError(10004);

    private static Error CreateError(int errorCode, string? detail = null)
    {
        var descriptor = _all[errorCode];

        return new Error(descriptor)
        {
            Detail = detail
        };
    }
}
