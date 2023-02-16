using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Admin;

public class UserImportTests : TestBase
{
    public UserImportTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_UnauthenticatedUser_RedirectsToSignIn()
    {
        var userImportJobId = Guid.NewGuid();
        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Get, $"/admin/user-imports/{userImportJobId}");
    }

    [Fact]
    public async Task Get_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        var userImportJobId = Guid.NewGuid();
        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Get, $"/admin/user-imports/{userImportJobId}");
    }

    [Fact]
    public async Task Get_UserImportJobDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userImportJobId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/user-imports/{userImportJobId}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidRequest_RendersExpectedContent()
    {
        // Arrange
        var userImportJobId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/user-imports/{userImportJobId}");
        var userImportJobRow1Success = new UserImportJobRow
        {
            UserImportJobId = userImportJobId,
            RowNumber = 1,
            Id = Guid.NewGuid().ToString(),
            UserId = Guid.NewGuid()
        };

        var userImportJobRow2Failure = new UserImportJobRow
        {
            UserImportJobId = userImportJobId,
            RowNumber = 2,
            Id = Guid.NewGuid().ToString(),
            Errors = new List<string> { "There is something wrong with this row", "There is something else wrong with this row" }
        };

        var userImportJobRow3Success = new UserImportJobRow
        {
            UserImportJobId = userImportJobId,
            RowNumber = 3,
            Id = Guid.NewGuid().ToString(),
            UserId = Guid.NewGuid()
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
                userImportJobRow1Success,
                userImportJobRow2Failure,
                userImportJobRow3Success,
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

        var summary = doc.GetElementByTestId($"summary-{userImportJobId}");
        Assert.NotNull(summary);
        var file = summary.GetElementByTestId($"file-{userImportJobId}");
        Assert.NotNull(file);
        Assert.Equal(userImportJob.OriginalFilename, file.TextContent);
        var uploaded = summary.GetElementByTestId($"uploaded-{userImportJobId}");
        Assert.NotNull(uploaded);
        Assert.Equal(userImportJob.Uploaded.ToString("dd/MM/yyyy HH:mm"), uploaded.TextContent);
        var status = summary.GetElementByTestId($"status-{userImportJobId}");
        Assert.NotNull(status);
        Assert.Equal(userImportJob.UserImportJobStatus.ToString(), status.TextContent);
        var successSummary = summary.GetElementByTestId($"successsummary-{userImportJobId}");
        Assert.NotNull(successSummary);
        Assert.Equal("2 / 3", successSummary.TextContent);

        var details = doc.GetElementByTestId($"details-{userImportJobId}");
        Assert.NotNull(details);

        var tableRow1 = details.GetElementByTestId("user-import-row-1");
        Assert.NotNull(tableRow1);
        var rowNumber1 = tableRow1.GetElementByTestId("rownumber-1");
        Assert.NotNull(rowNumber1);
        Assert.Equal(userImportJobRow1Success.RowNumber.ToString(), rowNumber1.TextContent);
        var id1 = tableRow1.GetElementByTestId("id-1");
        Assert.NotNull(id1);
        Assert.Equal(userImportJobRow1Success.Id.ToString(), id1.TextContent);
        var userId1 = tableRow1.GetElementByTestId("userid-1");
        Assert.NotNull(userId1);
        Assert.Equal(userImportJobRow1Success.UserId.ToString(), userId1.TextContent);
        var errorcount1 = tableRow1.GetElementByTestId("errorcount-1");
        Assert.NotNull(errorcount1);
        Assert.Equal("0", errorcount1.TextContent);
        var tableRow1Errors = details.GetElementByTestId("user-import-row-errors-1");
        Assert.Null(tableRow1Errors);

        var tableRow2 = details.GetElementByTestId("user-import-row-2");
        Assert.NotNull(tableRow2);
        var rowNumber2 = tableRow2.GetElementByTestId("rownumber-2");
        Assert.NotNull(rowNumber2);
        Assert.Equal(userImportJobRow2Failure.RowNumber.ToString(), rowNumber2.TextContent);
        var id2 = tableRow2.GetElementByTestId("id-2");
        Assert.NotNull(id2);
        Assert.Equal(userImportJobRow2Failure.Id.ToString(), id2.TextContent);
        var userId2 = tableRow2.GetElementByTestId("userid-2");
        Assert.NotNull(userId2);
        Assert.Equal("{null}", userId2.TextContent);
        var errorcount2 = tableRow2.GetElementByTestId("errorcount-2");
        Assert.NotNull(errorcount2);
        Assert.Equal("2", errorcount2.TextContent);
        var tableRow2Errors = details.GetElementByTestId("user-import-row-errors-2");
        Assert.NotNull(tableRow2Errors);

        var tableRow3 = details.GetElementByTestId("user-import-row-3");
        Assert.NotNull(tableRow3);
        var rowNumber3 = tableRow3.GetElementByTestId("rownumber-3");
        Assert.NotNull(rowNumber3);
        Assert.Equal(userImportJobRow3Success.RowNumber.ToString(), rowNumber3.TextContent);
        var id3 = tableRow3.GetElementByTestId("id-3");
        Assert.NotNull(id3);
        Assert.Equal(userImportJobRow3Success.Id.ToString(), id3.TextContent);
        var userId3 = tableRow3.GetElementByTestId("userid-3");
        Assert.NotNull(userId3);
        Assert.Equal(userImportJobRow3Success.UserId.ToString(), userId3.TextContent);
        var errorcount3 = tableRow3.GetElementByTestId("errorcount-3");
        Assert.NotNull(errorcount3);
        Assert.Equal("0", errorcount3.TextContent);
        var tableRow3Errors = details.GetElementByTestId("user-import-row-errors-3");
        Assert.Null(tableRow3Errors);
    }

    [Fact]
    public async Task Get_DownloadFile_ReturnsCsvFileContainingExpectedContent()
    {
        // Arrange
        var userImportJobId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/user-imports/{userImportJobId}?handler=DownloadFile");
        var userImportJobRow1Success = new UserImportJobRow
        {
            UserImportJobId = userImportJobId,
            RowNumber = 1,
            Id = Guid.NewGuid().ToString(),
            UserId = Guid.NewGuid(),
            RawData = "This,was,the,raw,data,1"
        };

        var userImportJobRow2Failure = new UserImportJobRow
        {
            UserImportJobId = userImportJobId,
            RowNumber = 2,
            Id = Guid.NewGuid().ToString(),
            RawData = "This,was,the,raw,data,2",
            Errors = new List<string> { "There is something wrong with this row", "There is something else wrong with this row" }
        };

        var userImportJobRow3Success = new UserImportJobRow
        {
            UserImportJobId = userImportJobId,
            RowNumber = 3,
            Id = Guid.NewGuid().ToString(),
            UserId = Guid.NewGuid(),
            RawData = "This,was,the,raw,data,3"
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
                userImportJobRow1Success,
                userImportJobRow2Failure,
                userImportJobRow3Success,
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

        var downloadedFilename = response!.Content?.Headers?.ContentDisposition?.FileNameStar;
        Assert.Contains("userimportdetails", downloadedFilename);

        using var stream = await response!.Content!.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true });
        var downloadRecords = csv.GetRecords<CsvRowInfo>();
        Assert.NotNull(downloadRecords);
        var rows = downloadRecords.ToList();
        Assert.Equal(3, rows.Count);
        Assert.Equal("1", rows[0].RowNumber);
        Assert.Equal(userImportJobRow1Success.Id.ToString(), rows[0].Id);
        Assert.Equal(userImportJobRow1Success.UserId.ToString(), rows[0].UserId);
        Assert.Equal(string.Empty, rows[0].Errors);
        Assert.Equal(userImportJobRow1Success.RawData, rows[0].RawData);
        Assert.Equal("2", rows[1].RowNumber);
        Assert.Equal(userImportJobRow2Failure.Id.ToString(), rows[1].Id);
        Assert.Equal(string.Empty, rows[1].UserId);
        Assert.Contains(userImportJobRow2Failure.Errors[0], rows[1].Errors);
        Assert.Contains(userImportJobRow2Failure.Errors[1], rows[1].Errors);
        Assert.Equal(userImportJobRow2Failure.RawData, rows[1].RawData);
        Assert.Equal("3", rows[2].RowNumber);
        Assert.Equal(userImportJobRow3Success.Id.ToString(), rows[2].Id);
        Assert.Equal(userImportJobRow3Success.UserId.ToString(), rows[2].UserId);
        Assert.Equal(string.Empty, rows[2].Errors);
        Assert.Equal(userImportJobRow3Success.RawData, rows[2].RawData);
    }
}

public class CsvRowInfo
{
    public required string RowNumber { get; init; }
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public required string Errors { get; init; }
    public required string RawData { get; init; }
}
