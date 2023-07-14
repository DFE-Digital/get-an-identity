namespace TeacherIdentity.AuthServer.Infrastructure.Http;

public static class HttpResponseMessageExtensions
{
    public static HttpResponseMessage EnsureSuccessStatusCodeWithRateLimiting(this HttpResponseMessage httpResponseMessage)
    {
        if (httpResponseMessage.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            var retryAfterHeaderValue = httpResponseMessage.Headers.RetryAfter;
            TimeSpan? retryAfter = retryAfterHeaderValue is null ? null : TimeSpan.FromSeconds(int.Parse(retryAfterHeaderValue.ToString()));
            throw new HttpRateLimitingException(retryAfter);
        }

        return httpResponseMessage.EnsureSuccessStatusCode();
    }
}
