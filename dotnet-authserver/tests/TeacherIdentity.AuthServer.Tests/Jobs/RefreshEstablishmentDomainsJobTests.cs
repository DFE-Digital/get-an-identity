using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Jobs;
using TeacherIdentity.AuthServer.Services.Establishment;

namespace TeacherIdentity.AuthServer.Tests.Jobs;

public class RefreshEstablishmentDomainsJobTests : IClassFixture<DbFixture>
{
    private readonly DbFixture _dbFixture;

    public RefreshEstablishmentDomainsJobTests(DbFixture dbFixture)
    {
        _dbFixture = dbFixture;
    }

    [Fact]
    public async Task Execute_WithValidEstablishmentWebsites_ExtractsDomainsAsExpectedAndInsertsIntoDatabase()
    {
        // Arrange
        using var dbContext = _dbFixture.GetDbContext();
        var establishmentMasterDataService = Mock.Of<IEstablishmentMasterDataService>();
        var logger = Mock.Of<ILogger<RefreshEstablishmentDomainsJob>>();
        var validWebsites = new[]
        {
            "http://www.myschool1.sch.uk",
            "https://www.myschool2.sch.uk",
            "http://myschool3.sch.uk",
            "https://myschool4.sch.uk",
            "myschool5.sch.uk",
            "http://www.myschool6.sch.uk/with/extra/path",
            "https://www.myschool7.sch.uk/with/extra/path",
            "http://myschool8.sch.uk/with/extra/path",
            "https://myschool9.sch.uk/with/extra/path",
            "myschool10.sch.uk/with/extra/path",
        };

        var expectedExtractedDomains = new[]
        {
            "myschool1.sch.uk",
            "myschool2.sch.uk",
            "myschool3.sch.uk",
            "myschool4.sch.uk",
            "myschool5.sch.uk",
            "myschool6.sch.uk",
            "myschool7.sch.uk",
            "myschool8.sch.uk",
            "myschool9.sch.uk",
            "myschool10.sch.uk",
        };

        Mock.Get(establishmentMasterDataService)
            .Setup(e => e.GetEstablishmentWebsites())
            .Returns(validWebsites.ToAsyncEnumerable());

        // Act
        var job = new RefreshEstablishmentDomainsJob(
            dbContext,
            establishmentMasterDataService,
            logger);
        await job.Execute(CancellationToken.None);

        // Assert
        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            var establishmentDomainNames = await dbContext.EstablishmentDomains.Select(e => e.DomainName).ToListAsync();
            foreach (var expectedExtractedDomain in expectedExtractedDomains)
            {
                Assert.Contains(expectedExtractedDomain, establishmentDomainNames);
            }
        });
    }

    [Fact]
    public async Task Execute_WithWebsiteWithInvalidUri_LogsWarning()
    {
        // Arrange
        using var dbContext = _dbFixture.GetDbContext();
        var establishmentMasterDataService = Mock.Of<IEstablishmentMasterDataService>();
        var logger = Mock.Of<ILogger<RefreshEstablishmentDomainsJob>>();
        var invalidWebsites = new[]
        {
            "http:www.myschool1.sch.uk"
        };

        Mock.Get(establishmentMasterDataService)
            .Setup(e => e.GetEstablishmentWebsites())
            .Returns(invalidWebsites.ToAsyncEnumerable());

        // Act
        var job = new RefreshEstablishmentDomainsJob(
            dbContext,
            establishmentMasterDataService,
            logger);
        await job.Execute(CancellationToken.None);

        // Assert
        Mock.Get(logger)
            .Verify(l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v.ToString() == "School/establishment website http:www.myschool1.sch.uk is not a valid URI"), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
    }

    [Fact]
    public async Task Execute_WithWebsiteWithInvalidDomain_LogsWarning()
    {
        // Arrange
        using var dbContext = _dbFixture.GetDbContext();
        var establishmentMasterDataService = Mock.Of<IEstablishmentMasterDataService>();
        var logger = Mock.Of<ILogger<RefreshEstablishmentDomainsJob>>();
        var invalidWebsites = new[]
        {
            "?myschool1.sch.uk"
        };

        Mock.Get(establishmentMasterDataService)
            .Setup(e => e.GetEstablishmentWebsites())
            .Returns(invalidWebsites.ToAsyncEnumerable());

        // Act
        var job = new RefreshEstablishmentDomainsJob(
            dbContext,
            establishmentMasterDataService,
            logger);
        await job.Execute(CancellationToken.None);

        // Assert
        Mock.Get(logger)
            .Verify(l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v.ToString() == "?myschool1.sch.uk is not a valid domain name"), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
    }
}
