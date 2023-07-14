namespace TeacherIdentity.AuthServer.Infrastructure.Http;

public class HttpRateLimitingException : Exception
{
    public TimeSpan? RetryAfter { get; private set; }

    public HttpRateLimitingException(TimeSpan? retryAfter)
        : this(retryAfter, DefaultMessage(retryAfter), null)
    {
    }

    public HttpRateLimitingException(TimeSpan? retryAfter, string? message)
        : this(retryAfter, message, null)
    {
    }

    public HttpRateLimitingException(TimeSpan? retryAfter, string? message, Exception? innerException)
        : base(message, innerException)
    {
        RetryAfter = retryAfter;
    }

    private static string DefaultMessage(TimeSpan? retryAfter) =>
        $"The operation has been rate-limited and should be retried after {retryAfter}";
}
