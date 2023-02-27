using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Admin;

public class UserImportsTests : TestBase
{
    public UserImportsTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_UnauthenticatedUser_RedirectsToSignIn()
    {
        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Get, "/admin/user-imports");
    }

    [Fact]
    public async Task Get_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Get, "/admin/user-imports");
    }

    [Fact]
    public async Task Get_ValidRequest_RendersExpectedContent()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/admin/user-imports");
        var userImportJobId = Guid.NewGuid();
        var userImportJobRowSuccess1 = new UserImportJobRow
        {
            UserImportJobId = userImportJobId,
            RowNumber = 1,
            Id = Guid.NewGuid().ToString(),
            UserId = Guid.NewGuid(),
            UserImportRowResult = UserImportRowResult.UserAdded
        };

        var userImportJobRowFailure = new UserImportJobRow
        {
            UserImportJobId = userImportJobId,
            RowNumber = 2,
            Id = Guid.NewGuid().ToString(),
            Notes = new List<string> { "There is something wrong with this row" },
            UserImportRowResult = UserImportRowResult.Invalid
        };

        var userImportJobRowSuccess2 = new UserImportJobRow
        {
            UserImportJobId = userImportJobId,
            RowNumber = 3,
            Id = Guid.NewGuid().ToString(),
            UserId = Guid.NewGuid(),
            UserImportRowResult = UserImportRowResult.UserAdded
        };

        var userImportJob = new UserImportJob
        {
            UserImportJobId = userImportJobId,
            OriginalFilename = "my-test.csv",
            StoredFilename = "stored.csv",
            UserImportJobStatus = UserImportJobStatus.Processed,
            Uploaded = DateTime.UtcNow,
            UserImportJobRows = new List<UserImportJobRow>
            {
                userImportJobRowSuccess1,
                userImportJobRowFailure,
                userImportJobRowSuccess2,
            }
        };

        await TestData.WithDbContext(async dbContext =>
        {
            dbContext.UserImportJobs.Add(userImportJob);
            await dbContext.SaveChangesAsync();
        });

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        var tableRow = doc.GetElementByTestId($"user-import-{userImportJobId}");
        Assert.NotNull(tableRow);
        var file = tableRow.GetElementByTestId($"file-{userImportJobId}");
        Assert.NotNull(file);
        Assert.Equal(userImportJob.OriginalFilename, file.TextContent);
        var uploaded = tableRow.GetElementByTestId($"uploaded-{userImportJobId}");
        Assert.NotNull(uploaded);
        Assert.Equal(userImportJob.Uploaded.ToString("dd/MM/yyyy HH:mm"), uploaded.TextContent);
        var status = tableRow.GetElementByTestId($"status-{userImportJobId}");
        Assert.NotNull(status);
        Assert.Equal(userImportJob.UserImportJobStatus.ToString(), status.TextContent);
        var added = tableRow.GetElementByTestId($"added-{userImportJobId}");
        Assert.NotNull(added);
        Assert.Equal("2", added.TextContent);
        var updated = tableRow.GetElementByTestId($"updated-{userImportJobId}");
        Assert.NotNull(updated);
        Assert.Equal("0", updated.TextContent);
        var invalid = tableRow.GetElementByTestId($"invalid-{userImportJobId}");
        Assert.NotNull(invalid);
        Assert.Equal("1", invalid.TextContent);
        var noAction = tableRow.GetElementByTestId($"noaction-{userImportJobId}");
        Assert.NotNull(noAction);
        Assert.Equal("0", noAction.TextContent);
        var total = tableRow.GetElementByTestId($"total-{userImportJobId}");
        Assert.NotNull(total);
        Assert.Equal("3", total.TextContent);
    }
}
