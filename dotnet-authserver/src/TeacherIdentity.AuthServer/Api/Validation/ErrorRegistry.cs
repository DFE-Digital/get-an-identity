namespace TeacherIdentity.AuthServer.Api.Validation;

public static class ErrorRegistry
{
    private static readonly Dictionary<int, ErrorDescriptor> _all = new ErrorDescriptor[]
    {
        new ErrorDescriptor(10001, "UserType must be Teacher"),
        new ErrorDescriptor(10002, "TRN is assigned to another user"),
    }.ToDictionary(d => d.ErrorCode, d => d);

    public static Error UserMustBeTeacher() => CreateError(10001);

    public static Error TrnIsAssignedToAnotherUser() => CreateError(10002);

    private static Error CreateError(int errorCode)
    {
        var descriptor = _all[errorCode];
        return new Error(descriptor);
    }
}
