using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Api.V1.TrnTokens;

public class CreateTrnTokenTests : TestBase
{
    public CreateTrnTokenTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Post_EmptyTrn_ReturnsBadRequest()
    {
        // Arrange
        var httpClient = await CreateHttpClientWithToken(withUser: false, CustomScopes.TrnTokenWrite);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/trn-tokens")
        {
            Content = JsonContent.Create(new
            {
                email = Faker.Internet.Email()
            })
        };

        // Act
        var response = await httpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_InvalidTrn_ReturnsBadRequest()
    {
        // Arrange
        var httpClient = await CreateHttpClientWithToken(withUser: false, CustomScopes.TrnTokenWrite);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/trn-tokens")
        {
            Content = JsonContent.Create(new
            {
                email = Faker.Internet.Email(),
                trn = "1"
            })
        };

        // Act
        var response = await httpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_EmptyEmail_ReturnsBadRequest()
    {
        // Arrange
        var httpClient = await CreateHttpClientWithToken(withUser: false, CustomScopes.TrnTokenWrite);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/trn-tokens")
        {
            Content = JsonContent.Create(new
            {
                email = "",
                trn = TestData.GenerateTrn()
            })
        };

        // Act
        var response = await httpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_InvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var httpClient = await CreateHttpClientWithToken(withUser: false, CustomScopes.TrnTokenWrite);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/trn-tokens")
        {
            Content = JsonContent.Create(new
            {
                email = "invalid",
                trn = TestData.GenerateTrn()
            })
        };

        // Act
        var response = await httpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_ValidRequest_GeneratesTrnToken()
    {
        // Arrange
        var httpClient = await CreateHttpClientWithToken(withUser: false, CustomScopes.TrnTokenWrite);

        var email = Faker.Internet.Email();
        var trn = TestData.GenerateTrn();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/trn-tokens")
        {
            Content = JsonContent.Create(new
            {
                email,
                trn
            })
        };

        // Act
        var response = await httpClient.SendAsync(request);

        // Assert
        await TestData.WithDbContext(async dbContext =>
        {
            var trnToken = await dbContext.TrnTokens.SingleOrDefaultAsync(t => t.Email == email);
            Assert.NotNull(trnToken);
        });
    }

    [Fact]
    public async Task Post_ValidRequest_ReturnsCorrectResponse()
    {
        // Arrange
        var httpClient = await CreateHttpClientWithToken(withUser: false, CustomScopes.TrnTokenWrite);

        var email = Faker.Internet.Email();
        var trn = TestData.GenerateTrn();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/trn-tokens")
        {
            Content = JsonContent.Create(new
            {
                email,
                trn
            })
        };

        // Act
        var response = await httpClient.SendAsync(request);

        // Assert
        var jsonResponse = await AssertEx.JsonResponse(response);
        Assert.Equal(trn, jsonResponse.RootElement.GetProperty("trn").GetString());
        Assert.Equal(email, jsonResponse.RootElement.GetProperty("email").GetString());
        Assert.NotNull(jsonResponse.RootElement.GetProperty("trnToken").GetString());
    }
}
