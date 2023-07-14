using System.Text;
using Microsoft.EntityFrameworkCore;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Admin;

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
        var byteArrayContent = new ByteArrayContent(new byte[] { });
        byteArrayContent.Headers.Add("Content-Type", "application/octet-stream");

        var multipartContent = new MultipartFormDataContent("----myuserimportboundary");
        multipartContent.Add(byteArrayContent, "Upload", "test-user-import.txt");

        var request = new HttpRequestMessage(HttpMethod.Post, "/admin/user-imports/new");
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
        var csvContent = new StringBuilder();

        var request = new HttpRequestMessage(HttpMethod.Post, "/admin/user-imports/new");
        request.Content = BuildCsvUploadFormContent("test-user-import.csv", csvContent);

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Upload", "The selected file contains no records");
    }

    [Fact]
    public async Task Post_FileWithInvalidHeadersUploaded_RendersError()
    {
        // Arrange
        var csvContent = new StringBuilder().AppendLine("This is an invalid header");

        var request = new HttpRequestMessage(HttpMethod.Post, "/admin/user-imports/new");
        request.Content = BuildCsvUploadFormContent("test-user-import.csv", csvContent);

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Upload", "The selected file contains invalid headers");
    }

    [Fact]
    public async Task Post_FileWithExtraHeadersUploaded_RendersError()
    {
        // Arrange
        var csvContent = new StringBuilder().AppendLine("ID,EMAIL_ADDRESS,TRN,FIRST_NAME,MIDDLE_NAME,LAST_NAME,PREFERRED_NAME,DATE_OF_BIRTH,TRN,EXTRA_COLUMN");

        var request = new HttpRequestMessage(HttpMethod.Post, "/admin/user-imports/new");
        request.Content = BuildCsvUploadFormContent("test-user-import.csv", csvContent);

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Upload", "The selected file contains invalid headers");
    }

    [Fact]
    public async Task Post_FileWithValidHeadersButNoDataRowsUploaded_RendersError()
    {
        // Arrange
        var csvContent = new StringBuilder().AppendLine("ID,EMAIL_ADDRESS,TRN,FIRST_NAME,MIDDLE_NAME,LAST_NAME,PREFERRED_NAME,DATE_OF_BIRTH");

        var request = new HttpRequestMessage(HttpMethod.Post, "/admin/user-imports/new");
        request.Content = BuildCsvUploadFormContent("test-user-import.csv", csvContent);

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Upload", "The selected file contains no records");
    }

    [Fact]
    public async Task Post_ValidFileUploaded_StoresFileAndUpdatesDatabaseAndRedirects()
    {
        // Arrange
        var csvFilename = "test-user-import.csv";
        var csvContent = BuildCsvContent();
        var csvStream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent.ToString()));

        HostFixture.UserImportCsvStorageService
            .Setup(s => s.OpenReadStream(It.IsAny<string>()))
            .ReturnsAsync(csvStream);

        var request = new HttpRequestMessage(HttpMethod.Post, "/admin/user-imports/new");
        request.Content = BuildCsvUploadFormContent(csvFilename, csvContent);

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

    private StringBuilder BuildCsvContent()
    {
        var csvContent = new StringBuilder();
        csvContent.AppendLine("ID,EMAIL_ADDRESS,TRN,FIRST_NAME,MIDDLE_NAME,LAST_NAME,PREFERRED_NAME,DATE_OF_BIRTH");
        csvContent.AppendLine("1234567890,test@email.com,1234765,joe,orlando,bloggs,joe bloggs,05021970");
        return csvContent;
    }

    private MultipartFormDataContent BuildCsvUploadFormContent(string csvFilename, StringBuilder csvContent)
    {
        var byteArrayContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csvContent.ToString()));
        byteArrayContent.Headers.Add("Content-Type", "text/csv");

        var multipartContent = new MultipartFormDataContent("----myuserimportboundary");
        multipartContent.Add(byteArrayContent, "Upload", csvFilename);
        return multipartContent;
    }
}
