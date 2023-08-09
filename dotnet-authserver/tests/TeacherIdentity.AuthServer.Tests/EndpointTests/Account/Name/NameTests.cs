namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Account.Name;

public class NameTests : TestBase
{
    public NameTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_NameChangeDisabled_ReturnsBadRequest()
    {
        // Arrange
        HostFixture.Configuration["DqtSynchronizationEnabled"] = "true";
        HostFixture.SetUserId(TestUsers.DefaultUserWithTrn.UserId);

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            AppendQueryParameterSignature($"/account/name"));

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);

        // Reset config
        HostFixture.Configuration["DqtSynchronizationEnabled"] = "false";
    }

    [Fact]
    public async Task Post_EmptyFirstName_ReturnsError()
    {
        // Arrange
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            AppendQueryParameterSignature($"/account/name"))
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
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            AppendQueryParameterSignature($"/account/name"))
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
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            AppendQueryParameterSignature($"/account/name"))
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
    public async Task Post_TooLongMiddleName_ReturnsError()
    {
        // Arrange
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            AppendQueryParameterSignature($"/account/name"))
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "FirstName", Faker.Name.First() },
                { "MiddleName", new string('a', 201) },
                { "LastName", Faker.Name.Last() },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "MiddleName", "Middle name must be 200 characters or less");
    }

    [Fact]
    public async Task Post_TooLongLastName_ReturnsError()
    {
        // Arrange
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            AppendQueryParameterSignature($"/account/name"))
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
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            AppendQueryParameterSignature($"/account/name"))
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "FirstName", Faker.Name.First() },
                { "MiddleName", Faker.Name.Middle() },
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

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            AppendQueryParameterSignature($"/account/name?{clientRedirectInfo.ToQueryParam()}"))
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

    [Fact]
    public async Task Get_ValidRequestWithNamesInQueryParam_PopulatesFieldsFromQueryParams()
    {
        // Arrange
        var previouslyStatedFirstName = Faker.Name.First();
        var previouslyStatedMiddleName = Faker.Name.Middle();
        var previouslyStatedLastName = Faker.Name.Last();
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            AppendQueryParameterSignature(
                $"/account/name" +
                $"?firstName={Uri.EscapeDataString(previouslyStatedFirstName)}" +
                $"&middleName={Uri.EscapeDataString(previouslyStatedMiddleName)}" +
                $"&lastName={Uri.EscapeDataString(previouslyStatedLastName)}"));

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        var doc = await response.GetDocument();
        Assert.Equal(previouslyStatedFirstName, doc.GetElementById("FirstName")?.GetAttribute("value"));
        Assert.Equal(previouslyStatedMiddleName, doc.GetElementById("MiddleName")?.GetAttribute("value"));
        Assert.Equal(previouslyStatedLastName, doc.GetElementById("LastName")?.GetAttribute("value"));
    }

    [Fact]
    public async Task Get_ValidRequestWithoutNamesInQueryParam_PopulatesFieldsFromDatabase()
    {
        // Arrange
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            AppendQueryParameterSignature($"/account/name"));

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        var doc = await response.GetDocument();

        Assert.Equal(TestUsers.DefaultUser.FirstName, doc.GetElementById("FirstName")?.GetAttribute("value"));
        Assert.Equal(TestUsers.DefaultUser.MiddleName, doc.GetElementById("MiddleName")?.GetAttribute("value"));
        Assert.Equal(TestUsers.DefaultUser.LastName, doc.GetElementById("LastName")?.GetAttribute("value"));
    }
}
