namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Account.OfficialName;

public class PreferredNameTests : TestBase
{
    public PreferredNameTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_ValidRequestWithPreferredNameInQueryParam_PopulatesFieldFromQueryParam()
    {
        // Arrange
        var previouslyStatedPreferredName = Faker.Name.FullName();

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            AppendQueryParameterSignature(
                $"/account/official-name/preferred-name" +
                $"?preferredName={Uri.EscapeDataString(previouslyStatedPreferredName)}"));

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        var doc = await response.GetDocument();
        Assert.Equal(previouslyStatedPreferredName, doc.GetElementById("PreferredName")?.GetAttribute("value"));
    }

    [Fact]
    public async Task Post_WhenPreferredNameChoiceHasNoSelection_ReturnsError()
    {
        // Arrange
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            AppendQueryParameterSignature($"/account/official-name/preferred-name"))
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "PreferredNameChoice", "Select which name to use");
    }

    [Fact]
    public async Task Post_WhenPreferredNameChoiceIsPreferredNameAndPreferredNameIsEmpty_ReturnsError()
    {
        // Arrange
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            AppendQueryParameterSignature($"/account/official-name/preferred-name"))
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "PreferredNameChoice", "PreferredName" },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "PreferredName", "Enter your preferred name");
    }

    [Fact]
    public async Task Post_WhenPreferredNameChoiceIsPreferredNameAndPreferredNameIsTooLong_ReturnsError()
    {
        // Arrange
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            AppendQueryParameterSignature($"/account/official-name/preferred-name"))
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "PreferredNameChoice", "PreferredName" },
                { "PreferredName", new string('a', 201) }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "PreferredName", "Preferred name must be 200 characters or less");
    }

    [Fact]
    public async Task Post_WhenPreferredNameChoiceIsPreferredNameAndPreferredNameIsValid_RedirectsToConfirmPage()
    {
        // Arrange
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            AppendQueryParameterSignature($"/account/official-name/preferred-name"))
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "PreferredNameChoice", "PreferredName" },
                { "PreferredName", Faker.Name.FullName() }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/account/official-name/confirm", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_WhenPreferredNameChoiceIsExistingName_RedirectsToConfirmPage()
    {
        // Arrange
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            AppendQueryParameterSignature($"/account/official-name/preferred-name"))
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "PreferredNameChoice", "ExistingName" }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/account/official-name/confirm", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_WhenPreferredNameChoiceIsExistingFullName_RedirectsToConfirmPage()
    {
        // Arrange
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            AppendQueryParameterSignature($"/account/official-name/preferred-name"))
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "PreferredNameChoice", "ExistingFullName" }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/account/official-name/confirm", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_WhenPreferredNameChoiceIsExistingPreferredName_RedirectsToConfirmPage()
    {
        // Arrange
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            AppendQueryParameterSignature($"/account/official-name/preferred-name"))
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "PreferredNameChoice", "ExistingPreferredName" }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/account/official-name/confirm", response.Headers.Location?.OriginalString);
    }
}
