namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Account.Name;

public class NameTests : TestBase
{
    public NameTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Post_EmptyFirstName_ReturnsError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/name")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "LastName", Faker.Name.Last() },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "FirstName", "Enter your first name");
    }

    [Fact]
    public async Task Post_EmptyLastName_ReturnsError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/name")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "FirstName", Faker.Name.First() },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "LastName", "Enter your last name");
    }

    [Fact]
    public async Task Post_TooLongFirstName_ReturnsError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/name")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "FirstName", new string('a', 201) },
                { "LastName", Faker.Name.Last() },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "FirstName", "First name must be 200 characters or less");
    }

    [Fact]
    public async Task Post_TooLongLastName_ReturnsError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/name")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "FirstName", Faker.Name.First() },
                { "LastName", new string('a', 201) },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "LastName", "Last name must be 200 characters or less");
    }

    [Fact]
    public async Task Post_ValidName_RedirectsToConfirmPage()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/name")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "FirstName", Faker.Name.First() },
                { "LastName", Faker.Name.Last() },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/account/name/confirm", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_ValidName_RedirectsToConfirmPageWithClientRedirectInfo()
    {
        // Arrange
        var clientRedirectInfo = CreateClientRedirectInfo();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/name?{clientRedirectInfo.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "FirstName", Faker.Name.First() },
                { "LastName", Faker.Name.Last() },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Contains(clientRedirectInfo.ToQueryParam(), response.Headers.Location?.OriginalString);
    }
}