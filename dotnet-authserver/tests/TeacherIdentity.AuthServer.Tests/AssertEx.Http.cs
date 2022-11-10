using System.Text.Json;
using System.Text.Json.Serialization;

namespace TeacherIdentity.AuthServer.Tests;

public static partial class AssertEx
{
    public static async Task<T> JsonResponse<T>(HttpResponseMessage response, int expectedStatusCode = StatusCodes.Status200OK)
    {
        if (response is null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        Assert.Equal(expectedStatusCode, (int)response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var result = await response.Content.ReadFromJsonAsync<T>(SerializerOptions);
        Assert.NotNull(result);
        return result!;
    }

    public static async Task JsonResponseIsError(HttpResponseMessage response, int expectedErrorCode, int expectedStatusCode)
    {
        var problemDetails = await ResponseIsProblemDetails(response, expectedStatusCode);

        Assert.NotNull(problemDetails.Extensions);
        Assert.Contains(problemDetails.Extensions, kvp => kvp.Key == "errorCode");
        Assert.Equal(expectedErrorCode, problemDetails.Extensions?["errorCode"].GetInt32());
    }

    public static async Task JsonResponseHasValidationErrorForProperty(
        HttpResponseMessage response,
        string propertyName,
        string expectedError,
        int expectedStatusCode = 400)
    {
        var problemDetails = await ResponseIsProblemDetails(response, expectedStatusCode);

        Assert.NotNull(problemDetails.Extensions);
        Assert.Contains(problemDetails.Extensions, kvp => kvp.Key == "errorCode");
        Assert.Equal(10004, problemDetails.Extensions?["errorCode"].GetInt32());
        Assert.Equal(expectedError, problemDetails.Errors?[propertyName].Single());
    }

    private static async Task<ProblemDetails> ResponseIsProblemDetails(HttpResponseMessage response, int expectedStatusCode)
    {
        if (response is null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        Assert.Equal(expectedStatusCode, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Equal(expectedStatusCode, problemDetails!.Status);

        return problemDetails;
    }

    private class ProblemDetails
    {
        public string? Title { get; set; }
        public int Status { get; set; }
        public IDictionary<string, string[]>? Errors { get; set; }
        [JsonExtensionData]
        public IDictionary<string, JsonElement>? Extensions { get; set; }
    }
}
