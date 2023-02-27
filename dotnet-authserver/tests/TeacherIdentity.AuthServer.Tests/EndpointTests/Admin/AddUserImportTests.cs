using System.Text;
using Microsoft.EntityFrameworkCore;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Admin;

[Collection(nameof(DisableParallelization))]  // Relies on mocks
public class AddUserImportTests : TestBase
{
    public AddUserImportTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_UnauthenticatedUser_RedirectsToSignIn()
    {
        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Get, "/admin/user-imports/new");
    }

    [Fact]
    public async Task Get_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Get, "/admin/user-imports/new");
    }

    [Fact]
    public async Task Get_ValidRequest_RendersExpectedContent()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/admin/user-imports/new");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_NoFileUploaded_RendersError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/admin/user-imports/new");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Upload", "Select a file");
    }

    [Fact]
    public async Task Post_NoneCsvFileUploaded_RendersError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/admin/user-imports/new");
        var multipartContent = new MultipartFormDataContent("----myuserimportboundary");
        var byteArrayContent = new ByteArrayContent(new byte[] { });
        byteArrayContent.Headers.Add("Content-Type", "application/octet-stream");
        multipartContent.Add(byteArrayContent, "Upload", "test-user-import.txt");
        request.Content = multipartContent;

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Upload", "The selected file must be a CSV");
    }

    [Fact]
    public async Task Post_EmptyFileUploaded_RendersError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/admin/user-imports/new");
        var multipartContent = new MultipartFormDataContent("----myuserimportboundary");
        var byteArrayContent = new ByteArrayContent(new byte[] { });
        byteArrayContent.Headers.Add("Content-Type", "text/csv");
        multipartContent.Add(byteArrayContent, "Upload", "test-user-import.csv");
        request.Content = multipartContent;

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Upload", "The selected file contains no records");
    }

    [Fact]
    public async Task Post_FileWithInvalidHeadersUploaded_RendersError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/admin/user-imports/new");
        var multipartContent = new MultipartFormDataContent("----myuserimportboundary");
        var csvContent = new StringBuilder();
        csvContent.AppendLine("This is an invalid header");
        var byteArrayContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csvContent.ToString()));
        byteArrayContent.Headers.Add("Content-Type", "text/csv");
        multipartContent.Add(byteArrayContent, "Upload", "test-user-import.csv");
        request.Content = multipartContent;

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Upload", "The selected file contains invalid headers");
    }

    [Fact]
    public async Task Post_FileWithExtraHeadersUploaded_RendersError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/admin/user-imports/new");
        var multipartContent = new MultipartFormDataContent("----myuserimportboundary");
        var csvContent = new StringBuilder();
        csvContent.AppendLine("ID,EMAIL_ADDRESS,FIRST_NAME,LAST_NAME,DATE_OF_BIRTH,TRN,EXTRA_COLUMN");
        var byteArrayContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csvContent.ToString()));
        byteArrayContent.Headers.Add("Content-Type", "text/csv");
        multipartContent.Add(byteArrayContent, "Upload", "test-user-import.csv");
        request.Content = multipartContent;

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Upload", "The selected file contains invalid headers");
    }

    [Fact]
    public async Task Post_FileWithValidHeadersButNoDataRowsUploaded_RendersError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/admin/user-imports/new");
        var multipartContent = new MultipartFormDataContent("----myuserimportboundary");
        var csvContent = new StringBuilder();
        csvContent.AppendLine("ID,EMAIL_ADDRESS,FIRST_NAME,LAST_NAME,DATE_OF_BIRTH,TRN");
        var byteArrayContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csvContent.ToString()));
        byteArrayContent.Headers.Add("Content-Type", "text/csv");
        multipartContent.Add(byteArrayContent, "Upload", "test-user-import.csv");
        request.Content = multipartContent;

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Upload", "The selected file contains no records");
    }

    [Fact]
    public async Task Post_ValidFileUploaded_StoresFileAndUpdatesDatabaseAndRedirects()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/admin/user-imports/new");
        var multipartContent = new MultipartFormDataContent("----myuserimportboundary");
        var csvFilename = "test-user-import.csv";
        var csvContent = new StringBuilder();
        csvContent.AppendLine("ID,EMAIL_ADDRESS,FIRST_NAME,LAST_NAME,DATE_OF_BIRTH,TRN");
        csvContent.AppendLine("1234567890,test@email.com,joe,bloggs,05021970,1234765");
        var byteArrayContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csvContent.ToString()));
        byteArrayContent.Headers.Add("Content-Type", "text/csv");
        multipartContent.Add(byteArrayContent, "Upload", csvFilename);
        request.Content = multipartContent;

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        HostFixture.UserImportCsvStorageService.Verify(s => s.Upload(It.IsAny<Stream>(), It.IsAny<string>()));

        await TestData.WithDbContext(async dbContext =>
        {
            var userImportJob = await dbContext.UserImportJobs.SingleOrDefaultAsync(u => u.OriginalFilename == csvFilename);
            Assert.NotNull(userImportJob);
        });

        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal("/admin/user-imports", response.Headers.Location?.OriginalString);

        var redirectedResponse = await response.FollowRedirect(HttpClient);
        var redirectedDoc = await redirectedResponse.GetDocument();
        AssertEx.HtmlDocumentHasFlashSuccess(redirectedDoc, $"CSV {csvFilename} uploaded");
    }
}
