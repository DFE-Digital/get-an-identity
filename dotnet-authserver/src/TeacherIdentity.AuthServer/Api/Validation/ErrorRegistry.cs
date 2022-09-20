namespace TeacherIdentity.AuthServer.Api.Validation;

public static class ErrorRegistry
{
    private static readonly Dictionary<int, ErrorDescriptor> _all = new ErrorDescriptor[]
    {
    }.ToDictionary(d => d.ErrorCode, d => d);

    private static Error CreateError(int errorCode)
    {
        var descriptor = _all[errorCode];
        return new Error(descriptor);
    }
}
