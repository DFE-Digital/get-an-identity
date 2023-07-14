namespace TeacherIdentity.AuthServer.Infrastructure.Http;

public class TooManyRequestsException : Exception
{
    public TimeSpan? RetryAfter { get; private set; }

    public TooManyRequestsException(TimeSpan? retryAfter)
        : this(retryAfter, DefaultMessage(retryAfter), null)
    {
    }

    public TooManyRequestsException(TimeSpan? retryAfter, string? message)
        : this(retryAfter, message, null)
    {
    }

    public TooManyRequestsException(TimeSpan? retryAfter, string? message, Exception? innerException)
        : base(message, innerException)
    {
        RetryAfter = retryAfter;
    }

    private static string DefaultMessage(TimeSpan? retryAfter) =>
        $"The operation has been rate-limited and should be retried{(retryAfter.HasValue ? " after " + retryAfter : "")}.";
}
