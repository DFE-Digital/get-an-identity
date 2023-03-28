using System.Text;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TeacherIdentity.AuthServer.Api.V1.Requests;
using TeacherIdentity.AuthServer.Api.V1.Responses;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Api.V1.TrnTokens;

public class PostTrnTokenTests : TestBase
{
    public PostTrnTokenTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Post_EmptyTrn_ReturnsBadRequest()
    {
        // Arrange
        var content = new StringContent("{\"Email\":\"testuser@example.com\"}", Encoding.UTF8, "application/json");

        // Act
        var response = await ApiKeyHttpClient.PostAsync("/api/v1/trn-tokens", content);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_InvalidTrn_ReturnsBadRequest()
    {
        // Arrange
        var trnTokenRequest = new PostTrnTokensRequest()
        {
            Trn = "1",
            Email = Faker.Internet.Email(),
        };

        var content = new StringContent(JsonConvert.SerializeObject(trnTokenRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await ApiKeyHttpClient.PostAsync("/api/v1/trn-tokens", content);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_EmptyEmail_ReturnsBadRequest()
    {
        // Arrange
        var content = new StringContent($"{{\"Trn\":\"{TestData.GenerateTrn()}\"}}", Encoding.UTF8, "application/json");

        // Act
        var response = await ApiKeyHttpClient.PostAsync("/api/v1/trn-tokens", content);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_InvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var trnTokenRequest = new PostTrnTokensRequest()
        {
            Trn = TestData.GenerateTrn(),
            Email = "invalid",
        };

        var content = new StringContent(JsonConvert.SerializeObject(trnTokenRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await ApiKeyHttpClient.PostAsync("/api/v1/trn-tokens", content);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_ValidRequest_GeneratesTrnToken()
    {
        // Arrange
        var trnTokenRequest = new PostTrnTokensRequest()
        {
            Trn = TestData.GenerateTrn(),
            Email = Faker.Internet.Email(),
        };

        var content = new StringContent(JsonConvert.SerializeObject(trnTokenRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await ApiKeyHttpClient.PostAsync("/api/v1/trn-tokens", content);

        // Assert
        await TestData.WithDbContext(async dbContext =>
        {
            var trnToken = await dbContext.TrnTokens.Where(t => t.Email == trnTokenRequest.Email).SingleOrDefaultAsync();
            Assert.NotNull(trnToken);
        });
    }

    [Fact]
    public async Task Post_ValidRequest_ReturnsCorrectResponse()
    {
        // Arrange
        var trnTokenRequest = new PostTrnTokensRequest()
        {
            Trn = TestData.GenerateTrn(),
            Email = Faker.Internet.Email(),
        };

        var content = new StringContent(JsonConvert.SerializeObject(trnTokenRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await ApiKeyHttpClient.PostAsync("/api/v1/trn-tokens", content);

        // Assert
        var trnTokenResponse = JsonConvert.DeserializeObject<PostTrnTokenResponse>(await response.Content.ReadAsStringAsync());

        Assert.NotNull(trnTokenResponse);
        Assert.Equal(trnTokenResponse.Trn, trnTokenRequest.Trn);
        Assert.Equal(trnTokenResponse.Email, trnTokenRequest.Email);
        Assert.NotNull(trnTokenResponse.TrnToken);
    }
}
