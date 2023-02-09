using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.UserImport;
using User = TeacherIdentity.AuthServer.Models.User;

namespace TeacherIdentity.AuthServer.Tests.Services;

public class UserImportProcessorTests : IClassFixture<DbFixture>
{
    private readonly DbFixture _dbFixture;

    public UserImportProcessorTests(DbFixture dbFixture)
    {
        _dbFixture = dbFixture;
    }

    [Fact]
    public async Task Process_WithValidCsvData_InsertsUserAndEventAndUserImportJobRowIntoDatabase()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var userImportStorageService = new Mock<IUserImportStorageService>();
        var clock = new TestClock();
        var logger = new Mock<ILogger<UserImportProcessor>>();
        var userImportJobId = Guid.NewGuid();
        var storedFilename = $"{userImportJobId}.csv";
        var csvContent = new StringBuilder();
        csvContent.AppendLine("ID,EMAIL_ADDRESS,FIRST_NAME,LAST_NAME,DATE_OF_BIRTH");
        csvContent.AppendLine("UserImportProcessorTests1,UserImportProcessorTests1@email.com,joe,bloggs,05021970");
        using var csvStream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent.ToString()));

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            dbContext.UserImportJobs.Add(new UserImportJob()
            {
                UserImportJobId = userImportJobId,
                StoredFilename = storedFilename,
                OriginalFilename = "my-user-import.csv",
                UserImportJobStatus = UserImportJobStatus.New,
                Uploaded = clock.UtcNow
            });

            await dbContext.SaveChangesAsync();
        });

        userImportStorageService.Setup(s => s.OpenReadStream(storedFilename))
            .ReturnsAsync(csvStream)
            .Verifiable();

        // Act
        var userImportProcessor = new UserImportProcessor(
            dbContext,
            userImportStorageService.Object,
            clock,
            logger.Object);
        await userImportProcessor.Process(userImportJobId);

        // Assert
        userImportStorageService
            .Verify();

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            var user = await dbContext.Users.SingleOrDefaultAsync(r => r.EmailAddress == "UserImportProcessorTests1@email.com");
            Assert.NotNull(user);

            var events = await dbContext.Events.Where(e => e.EventName == "UserImportedEvent").ToListAsync();
            var userImportedEventCount = events
                .Select(e => JsonSerializer.Deserialize<UserImportedEvent>(e.Payload))
                .Count(i => i!.UserImportJobId == userImportJobId && i.User.UserId == user.UserId);
            Assert.Equal(1, userImportedEventCount);

            var userImportJobRowCount = await dbContext.UserImportJobRows.CountAsync(r => r.UserImportJobId == userImportJobId);
            Assert.Equal(1, userImportJobRowCount);
        });
    }

    [Fact]
    public async Task Process_WithCsvRowWithLessFieldsThanExpected_InsertsUserImportJobRowWithErrorIntoDatabase()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var userImportStorageService = new Mock<IUserImportStorageService>();
        var clock = new TestClock();
        var logger = new Mock<ILogger<UserImportProcessor>>();
        var userImportJobId = Guid.NewGuid();
        var storedFilename = $"{userImportJobId}.csv";
        var csvContent = new StringBuilder();
        csvContent.AppendLine("ID,EMAIL_ADDRESS,FIRST_NAME,LAST_NAME,DATE_OF_BIRTH");
        csvContent.AppendLine("UserImportProcessorTests2,UserImportProcessorTests2@email.com,joe,bloggs");
        using var csvStream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent.ToString()));

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            dbContext.UserImportJobs.Add(new UserImportJob()
            {
                UserImportJobId = userImportJobId,
                StoredFilename = storedFilename,
                OriginalFilename = "my-user-import.csv",
                UserImportJobStatus = UserImportJobStatus.New,
                Uploaded = clock.UtcNow
            });

            await dbContext.SaveChangesAsync();
        });

        userImportStorageService.Setup(s => s.OpenReadStream(storedFilename))
            .ReturnsAsync(csvStream)
            .Verifiable();

        // Act
        var userImportProcessor = new UserImportProcessor(
            dbContext,
            userImportStorageService.Object,
            clock,
            logger.Object);
        await userImportProcessor.Process(userImportJobId);

        // Assert
        userImportStorageService
            .Verify();

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            var user = await dbContext.Users.SingleOrDefaultAsync(r => r.EmailAddress == "UserImportProcessorTests2@email.com");
            Assert.Null(user);

            var events = await dbContext.Events.Where(e => e.EventName == "UserImportedEvent").ToListAsync();
            var userImportedEventCount = events
                .Select(e => JsonSerializer.Deserialize<UserImportedEvent>(e.Payload))
                .Count(i => i!.UserImportJobId == userImportJobId);
            Assert.Equal(0, userImportedEventCount);

            var userImportJobRow = await dbContext.UserImportJobRows.SingleOrDefaultAsync(r => r.UserImportJobId == userImportJobId);
            Assert.NotNull(userImportJobRow);
            Assert.NotNull(userImportJobRow.Errors);
            Assert.Single(userImportJobRow.Errors);
            Assert.Equal($"Exactly {UserImportRow.ColumnCount} fields expected (this row has less)", userImportJobRow.Errors[0]);
        });
    }

    [Fact]
    public async Task Process_WithCsvRowWithMoreFieldsThanExpected_InsertsUserImportJobRowWithErrorIntoDatabase()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var userImportStorageService = new Mock<IUserImportStorageService>();
        var clock = new TestClock();
        var logger = new Mock<ILogger<UserImportProcessor>>();
        var userImportJobId = Guid.NewGuid();
        var storedFilename = $"{userImportJobId}.csv";
        var csvContent = new StringBuilder();
        csvContent.AppendLine("ID,EMAIL_ADDRESS,FIRST_NAME,LAST_NAME,DATE_OF_BIRTH");
        csvContent.AppendLine("UserImportProcessorTests3,UserImportProcessorTests3@email.com,joe,bloggs,05021970,extra,data");
        using var csvStream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent.ToString()));

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            dbContext.UserImportJobs.Add(new UserImportJob()
            {
                UserImportJobId = userImportJobId,
                StoredFilename = storedFilename,
                OriginalFilename = "my-user-import.csv",
                UserImportJobStatus = UserImportJobStatus.New,
                Uploaded = clock.UtcNow
            });

            await dbContext.SaveChangesAsync();
        });

        userImportStorageService.Setup(s => s.OpenReadStream(storedFilename))
            .ReturnsAsync(csvStream)
            .Verifiable();

        // Act
        var userImportProcessor = new UserImportProcessor(
            dbContext,
            userImportStorageService.Object,
            clock,
            logger.Object);
        await userImportProcessor.Process(userImportJobId);

        // Assert
        userImportStorageService
            .Verify();

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            var user = await dbContext.Users.SingleOrDefaultAsync(r => r.EmailAddress == "UserImportProcessorTests3@email.com");
            Assert.Null(user);

            var events = await dbContext.Events.Where(e => e.EventName == "UserImportedEvent").ToListAsync();
            var userImportedEventCount = events
                .Select(e => JsonSerializer.Deserialize<UserImportedEvent>(e.Payload))
                .Count(i => i!.UserImportJobId == userImportJobId);
            Assert.Equal(0, userImportedEventCount);

            var userImportJobRow = await dbContext.UserImportJobRows.SingleOrDefaultAsync(r => r.UserImportJobId == userImportJobId);
            Assert.NotNull(userImportJobRow);
            Assert.NotNull(userImportJobRow.Errors);
            Assert.Single(userImportJobRow.Errors);
            Assert.Equal($"Exactly {UserImportRow.ColumnCount} fields expected (this row has more)", userImportJobRow.Errors[0]);
        });
    }

    [Fact]
    public async Task Process_WithCsvRowWithEmptyIdField_InsertsUserImportJobRowWithErrorIntoDatabase()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var userImportStorageService = new Mock<IUserImportStorageService>();
        var clock = new TestClock();
        var logger = new Mock<ILogger<UserImportProcessor>>();
        var userImportJobId = Guid.NewGuid();
        var storedFilename = $"{userImportJobId}.csv";
        var csvContent = new StringBuilder();
        csvContent.AppendLine("ID,EMAIL_ADDRESS,FIRST_NAME,LAST_NAME,DATE_OF_BIRTH");
        csvContent.AppendLine(",UserImportProcessorTests4@email.com,joe,bloggs,05021970");
        using var csvStream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent.ToString()));

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            dbContext.UserImportJobs.Add(new UserImportJob()
            {
                UserImportJobId = userImportJobId,
                StoredFilename = storedFilename,
                OriginalFilename = "my-user-import.csv",
                UserImportJobStatus = UserImportJobStatus.New,
                Uploaded = clock.UtcNow
            });

            await dbContext.SaveChangesAsync();
        });

        userImportStorageService.Setup(s => s.OpenReadStream(storedFilename))
            .ReturnsAsync(csvStream)
            .Verifiable();

        // Act
        var userImportProcessor = new UserImportProcessor(
            dbContext,
            userImportStorageService.Object,
            clock,
            logger.Object);
        await userImportProcessor.Process(userImportJobId);

        // Assert
        userImportStorageService
            .Verify();

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            var user = await dbContext.Users.SingleOrDefaultAsync(r => r.EmailAddress == "UserImportProcessorTests4@email.com");
            Assert.Null(user);

            var events = await dbContext.Events.Where(e => e.EventName == "UserImportedEvent").ToListAsync();
            var userImportedEventCount = events
                .Select(e => JsonSerializer.Deserialize<UserImportedEvent>(e.Payload))
                .Count(i => i!.UserImportJobId == userImportJobId);
            Assert.Equal(0, userImportedEventCount);

            var userImportJobRow = await dbContext.UserImportJobRows.SingleOrDefaultAsync(r => r.UserImportJobId == userImportJobId);
            Assert.NotNull(userImportJobRow);
            Assert.NotNull(userImportJobRow.Errors);
            Assert.Single(userImportJobRow.Errors);
            Assert.Equal($"{UserImportRow.IdHeader} field is empty", userImportJobRow.Errors[0]);
        });
    }

    [Fact]
    public async Task Process_WithCsvRowWithOversizedIdField_InsertsUserImportJobRowWithErrorIntoDatabase()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var userImportStorageService = new Mock<IUserImportStorageService>();
        var clock = new TestClock();
        var logger = new Mock<ILogger<UserImportProcessor>>();
        var userImportJobId = Guid.NewGuid();
        var storedFilename = $"{userImportJobId}.csv";
        var oversizedId = new string('a', UserImportJobRow.IdMaxLength + 1);
        var csvContent = new StringBuilder();
        csvContent.AppendLine("ID,EMAIL_ADDRESS,FIRST_NAME,LAST_NAME,DATE_OF_BIRTH");
        csvContent.AppendLine($"{oversizedId},UserImportProcessorTests5@email.com,joe,bloggs,05021970");
        using var csvStream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent.ToString()));

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            dbContext.UserImportJobs.Add(new UserImportJob()
            {
                UserImportJobId = userImportJobId,
                StoredFilename = storedFilename,
                OriginalFilename = "my-user-import.csv",
                UserImportJobStatus = UserImportJobStatus.New,
                Uploaded = clock.UtcNow
            });

            await dbContext.SaveChangesAsync();
        });

        userImportStorageService.Setup(s => s.OpenReadStream(storedFilename))
            .ReturnsAsync(csvStream)
            .Verifiable();

        // Act
        var userImportProcessor = new UserImportProcessor(
            dbContext,
            userImportStorageService.Object,
            clock,
            logger.Object);
        await userImportProcessor.Process(userImportJobId);

        // Assert
        userImportStorageService
            .Verify();

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            var user = await dbContext.Users.SingleOrDefaultAsync(r => r.EmailAddress == "UserImportProcessorTests5@email.com");
            Assert.Null(user);

            var events = await dbContext.Events.Where(e => e.EventName == "UserImportedEvent").ToListAsync();
            var userImportedEventCount = events
                .Select(e => JsonSerializer.Deserialize<UserImportedEvent>(e.Payload))
                .Count(i => i!.UserImportJobId == userImportJobId);
            Assert.Equal(0, userImportedEventCount);

            var userImportJobRow = await dbContext.UserImportJobRows.SingleOrDefaultAsync(r => r.UserImportJobId == userImportJobId);
            Assert.NotNull(userImportJobRow);
            Assert.NotNull(userImportJobRow.Errors);
            Assert.Single(userImportJobRow.Errors);
            Assert.Equal($"{UserImportRow.IdHeader} field should have a maximum of {UserImportJobRow.IdMaxLength} characters", userImportJobRow.Errors[0]);
        });
    }

    [Fact]
    public async Task Process_WithCsvRowWithEmptyEmailAddressField_InsertsUserImportJobRowWithErrorIntoDatabase()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var userImportStorageService = new Mock<IUserImportStorageService>();
        var clock = new TestClock();
        var logger = new Mock<ILogger<UserImportProcessor>>();
        var userImportJobId = Guid.NewGuid();
        var storedFilename = $"{userImportJobId}.csv";
        var csvContent = new StringBuilder();
        csvContent.AppendLine("ID,EMAIL_ADDRESS,FIRST_NAME,LAST_NAME,DATE_OF_BIRTH");
        csvContent.AppendLine("UserImportProcessorTests6,,joe,bloggs,05021970");
        using var csvStream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent.ToString()));

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            dbContext.UserImportJobs.Add(new UserImportJob()
            {
                UserImportJobId = userImportJobId,
                StoredFilename = storedFilename,
                OriginalFilename = "my-user-import.csv",
                UserImportJobStatus = UserImportJobStatus.New,
                Uploaded = clock.UtcNow
            });

            await dbContext.SaveChangesAsync();
        });

        userImportStorageService.Setup(s => s.OpenReadStream(storedFilename))
            .ReturnsAsync(csvStream)
            .Verifiable();

        // Act
        var userImportProcessor = new UserImportProcessor(
            dbContext,
            userImportStorageService.Object,
            clock,
            logger.Object);
        await userImportProcessor.Process(userImportJobId);

        // Assert
        userImportStorageService
            .Verify();

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            var user = await dbContext.Users.SingleOrDefaultAsync(r => r.EmailAddress == "UserImportProcessorTests6@email.com");
            Assert.Null(user);

            var events = await dbContext.Events.Where(e => e.EventName == "UserImportedEvent").ToListAsync();
            var userImportedEventCount = events
                .Select(e => JsonSerializer.Deserialize<UserImportedEvent>(e.Payload))
                .Count(i => i!.UserImportJobId == userImportJobId);
            Assert.Equal(0, userImportedEventCount);

            var userImportJobRow = await dbContext.UserImportJobRows.SingleOrDefaultAsync(r => r.UserImportJobId == userImportJobId);
            Assert.NotNull(userImportJobRow);
            Assert.NotNull(userImportJobRow.Errors);
            Assert.Single(userImportJobRow.Errors);
            Assert.Equal($"{UserImportRow.EmailAddressHeader} field is empty", userImportJobRow.Errors[0]);
        });
    }

    [Fact]
    public async Task Process_WithCsvRowWithOversizedEmailAddressField_InsertsUserImportJobRowWithErrorIntoDatabase()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var userImportStorageService = new Mock<IUserImportStorageService>();
        var clock = new TestClock();
        var logger = new Mock<ILogger<UserImportProcessor>>();
        var userImportJobId = Guid.NewGuid();
        var storedFilename = $"{userImportJobId}.csv";
        var oversizedEmailAddress = $"{new string('a', User.EmailAddressMaxLength + 1)}@email.com";
        var csvContent = new StringBuilder();
        csvContent.AppendLine("ID,EMAIL_ADDRESS,FIRST_NAME,LAST_NAME,DATE_OF_BIRTH");
        csvContent.AppendLine($"UserImportProcessorTests7,{oversizedEmailAddress},joe,bloggs,05021970");
        using var csvStream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent.ToString()));

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            dbContext.UserImportJobs.Add(new UserImportJob()
            {
                UserImportJobId = userImportJobId,
                StoredFilename = storedFilename,
                OriginalFilename = "my-user-import.csv",
                UserImportJobStatus = UserImportJobStatus.New,
                Uploaded = clock.UtcNow
            });

            await dbContext.SaveChangesAsync();
        });

        userImportStorageService.Setup(s => s.OpenReadStream(storedFilename))
            .ReturnsAsync(csvStream)
            .Verifiable();

        // Act
        var userImportProcessor = new UserImportProcessor(
            dbContext,
            userImportStorageService.Object,
            clock,
            logger.Object);
        await userImportProcessor.Process(userImportJobId);

        // Assert
        userImportStorageService
            .Verify();

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            var user = await dbContext.Users.SingleOrDefaultAsync(r => r.EmailAddress == oversizedEmailAddress);
            Assert.Null(user);

            var events = await dbContext.Events.Where(e => e.EventName == "UserImportedEvent").ToListAsync();
            var userImportedEventCount = events
                .Select(e => JsonSerializer.Deserialize<UserImportedEvent>(e.Payload))
                .Count(i => i!.UserImportJobId == userImportJobId);
            Assert.Equal(0, userImportedEventCount);

            var userImportJobRow = await dbContext.UserImportJobRows.SingleOrDefaultAsync(r => r.UserImportJobId == userImportJobId);
            Assert.NotNull(userImportJobRow);
            Assert.NotNull(userImportJobRow.Errors);
            Assert.Single(userImportJobRow.Errors);
            Assert.Equal($"{UserImportRow.EmailAddressHeader} field should have a maximum of {User.EmailAddressMaxLength} characters", userImportJobRow.Errors[0]);
        });
    }

    [Fact]
    public async Task Process_WithCsvRowWithInvalidFormatEmailAddressField_InsertsUserImportJobRowWithErrorIntoDatabase()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var userImportStorageService = new Mock<IUserImportStorageService>();
        var clock = new TestClock();
        var logger = new Mock<ILogger<UserImportProcessor>>();
        var userImportJobId = Guid.NewGuid();
        var storedFilename = $"{userImportJobId}.csv";
        var csvContent = new StringBuilder();
        csvContent.AppendLine("ID,EMAIL_ADDRESS,FIRST_NAME,LAST_NAME,DATE_OF_BIRTH");
        csvContent.AppendLine("UserImportProcessorTests8,UserImportProcessorTests8.com,joe,bloggs,05021970");
        using var csvStream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent.ToString()));

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            dbContext.UserImportJobs.Add(new UserImportJob()
            {
                UserImportJobId = userImportJobId,
                StoredFilename = storedFilename,
                OriginalFilename = "my-user-import.csv",
                UserImportJobStatus = UserImportJobStatus.New,
                Uploaded = clock.UtcNow
            });

            await dbContext.SaveChangesAsync();
        });

        userImportStorageService.Setup(s => s.OpenReadStream(storedFilename))
            .ReturnsAsync(csvStream)
            .Verifiable();

        // Act
        var userImportProcessor = new UserImportProcessor(
            dbContext,
            userImportStorageService.Object,
            clock,
            logger.Object);
        await userImportProcessor.Process(userImportJobId);

        // Assert
        userImportStorageService
            .Verify();

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            var user = await dbContext.Users.SingleOrDefaultAsync(r => r.EmailAddress == "UserImportProcessorTests8.com");
            Assert.Null(user);

            var events = await dbContext.Events.Where(e => e.EventName == "UserImportedEvent").ToListAsync();
            var userImportedEventCount = events
                .Select(e => JsonSerializer.Deserialize<UserImportedEvent>(e.Payload))
                .Count(i => i!.UserImportJobId == userImportJobId);
            Assert.Equal(0, userImportedEventCount);

            var userImportJobRow = await dbContext.UserImportJobRows.SingleOrDefaultAsync(r => r.UserImportJobId == userImportJobId);
            Assert.NotNull(userImportJobRow);
            Assert.NotNull(userImportJobRow.Errors);
            Assert.Single(userImportJobRow.Errors);
            Assert.Equal($"{UserImportRow.EmailAddressHeader} field should be in a valid email address format", userImportJobRow.Errors[0]);
        });
    }

    [Fact]
    public async Task Process_WithCsvRowWithEmptyFirstNameField_InsertsUserImportJobRowWithErrorIntoDatabase()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var userImportStorageService = new Mock<IUserImportStorageService>();
        var clock = new TestClock();
        var logger = new Mock<ILogger<UserImportProcessor>>();
        var userImportJobId = Guid.NewGuid();
        var storedFilename = $"{userImportJobId}.csv";
        var csvContent = new StringBuilder();
        csvContent.AppendLine("ID,EMAIL_ADDRESS,FIRST_NAME,LAST_NAME,DATE_OF_BIRTH");
        csvContent.AppendLine("UserImportProcessorTests9,UserImportProcessorTests9@email.com,,bloggs,05021970");
        using var csvStream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent.ToString()));

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            dbContext.UserImportJobs.Add(new UserImportJob()
            {
                UserImportJobId = userImportJobId,
                StoredFilename = storedFilename,
                OriginalFilename = "my-user-import.csv",
                UserImportJobStatus = UserImportJobStatus.New,
                Uploaded = clock.UtcNow
            });

            await dbContext.SaveChangesAsync();
        });

        userImportStorageService.Setup(s => s.OpenReadStream(storedFilename))
            .ReturnsAsync(csvStream)
            .Verifiable();

        // Act
        var userImportProcessor = new UserImportProcessor(
            dbContext,
            userImportStorageService.Object,
            clock,
            logger.Object);
        await userImportProcessor.Process(userImportJobId);

        // Assert
        userImportStorageService
            .Verify();

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            var user = await dbContext.Users.SingleOrDefaultAsync(r => r.EmailAddress == "UserImportProcessorTests9@email.com");
            Assert.Null(user);

            var events = await dbContext.Events.Where(e => e.EventName == "UserImportedEvent").ToListAsync();
            var userImportedEventCount = events
                .Select(e => JsonSerializer.Deserialize<UserImportedEvent>(e.Payload))
                .Count(i => i!.UserImportJobId == userImportJobId);
            Assert.Equal(0, userImportedEventCount);

            var userImportJobRow = await dbContext.UserImportJobRows.SingleOrDefaultAsync(r => r.UserImportJobId == userImportJobId);
            Assert.NotNull(userImportJobRow);
            Assert.NotNull(userImportJobRow.Errors);
            Assert.Single(userImportJobRow.Errors);
            Assert.Equal($"{UserImportRow.FirstNameHeader} field is empty", userImportJobRow.Errors[0]);
        });
    }

    [Fact]
    public async Task Process_WithCsvRowWithOversizedFirstNameField_InsertsUserImportJobRowWithErrorIntoDatabase()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var userImportStorageService = new Mock<IUserImportStorageService>();
        var clock = new TestClock();
        var logger = new Mock<ILogger<UserImportProcessor>>();
        var userImportJobId = Guid.NewGuid();
        var storedFilename = $"{userImportJobId}.csv";
        var oversizedFirstName = new string('a', User.FirstNameMaxLength + 1);
        var csvContent = new StringBuilder();
        csvContent.AppendLine("ID,EMAIL_ADDRESS,FIRST_NAME,LAST_NAME,DATE_OF_BIRTH");
        csvContent.AppendLine($"UserImportProcessorTests10,UserImportProcessorTests10@email.com,{oversizedFirstName},bloggs,05021970");
        using var csvStream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent.ToString()));

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            dbContext.UserImportJobs.Add(new UserImportJob()
            {
                UserImportJobId = userImportJobId,
                StoredFilename = storedFilename,
                OriginalFilename = "my-user-import.csv",
                UserImportJobStatus = UserImportJobStatus.New,
                Uploaded = clock.UtcNow
            });

            await dbContext.SaveChangesAsync();
        });

        userImportStorageService.Setup(s => s.OpenReadStream(storedFilename))
            .ReturnsAsync(csvStream)
            .Verifiable();

        // Act
        var userImportProcessor = new UserImportProcessor(
            dbContext,
            userImportStorageService.Object,
            clock,
            logger.Object);
        await userImportProcessor.Process(userImportJobId);

        // Assert
        userImportStorageService
            .Verify();

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            var user = await dbContext.Users.SingleOrDefaultAsync(r => r.EmailAddress == "UserImportProcessorTests10@email.com");
            Assert.Null(user);

            var events = await dbContext.Events.Where(e => e.EventName == "UserImportedEvent").ToListAsync();
            var userImportedEventCount = events
                .Select(e => JsonSerializer.Deserialize<UserImportedEvent>(e.Payload))
                .Count(i => i!.UserImportJobId == userImportJobId);
            Assert.Equal(0, userImportedEventCount);

            var userImportJobRow = await dbContext.UserImportJobRows.SingleOrDefaultAsync(r => r.UserImportJobId == userImportJobId);
            Assert.NotNull(userImportJobRow);
            Assert.NotNull(userImportJobRow.Errors);
            Assert.Single(userImportJobRow.Errors);
            Assert.Equal($"{UserImportRow.FirstNameHeader} field should have a maximum of {User.FirstNameMaxLength} characters", userImportJobRow.Errors[0]);
        });
    }

    [Fact]
    public async Task Process_WithCsvRowWithEmptyLastNameField_InsertsUserImportJobRowWithErrorIntoDatabase()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var userImportStorageService = new Mock<IUserImportStorageService>();
        var clock = new TestClock();
        var logger = new Mock<ILogger<UserImportProcessor>>();
        var userImportJobId = Guid.NewGuid();
        var storedFilename = $"{userImportJobId}.csv";
        var csvContent = new StringBuilder();
        csvContent.AppendLine("ID,EMAIL_ADDRESS,FIRST_NAME,LAST_NAME,DATE_OF_BIRTH");
        csvContent.AppendLine("UserImportProcessorTests11,UserImportProcessorTests11@email.com,joe,,05021970");
        using var csvStream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent.ToString()));

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            dbContext.UserImportJobs.Add(new UserImportJob()
            {
                UserImportJobId = userImportJobId,
                StoredFilename = storedFilename,
                OriginalFilename = "my-user-import.csv",
                UserImportJobStatus = UserImportJobStatus.New,
                Uploaded = clock.UtcNow
            });

            await dbContext.SaveChangesAsync();
        });

        userImportStorageService.Setup(s => s.OpenReadStream(storedFilename))
            .ReturnsAsync(csvStream)
            .Verifiable();

        // Act
        var userImportProcessor = new UserImportProcessor(
            dbContext,
            userImportStorageService.Object,
            clock,
            logger.Object);
        await userImportProcessor.Process(userImportJobId);

        // Assert
        userImportStorageService
            .Verify();

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            var user = await dbContext.Users.SingleOrDefaultAsync(r => r.EmailAddress == "UserImportProcessorTests11@email.com");
            Assert.Null(user);

            var events = await dbContext.Events.Where(e => e.EventName == "UserImportedEvent").ToListAsync();
            var userImportedEventCount = events
                .Select(e => JsonSerializer.Deserialize<UserImportedEvent>(e.Payload))
                .Count(i => i!.UserImportJobId == userImportJobId);
            Assert.Equal(0, userImportedEventCount);

            var userImportJobRow = await dbContext.UserImportJobRows.SingleOrDefaultAsync(r => r.UserImportJobId == userImportJobId);
            Assert.NotNull(userImportJobRow);
            Assert.NotNull(userImportJobRow.Errors);
            Assert.Single(userImportJobRow.Errors);
            Assert.Equal($"{UserImportRow.LastNameHeader} field is empty", userImportJobRow.Errors[0]);
        });
    }

    [Fact]
    public async Task Process_WithCsvRowWithOversizedLastNameField_InsertsUserImportJobRowWithErrorIntoDatabase()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var userImportStorageService = new Mock<IUserImportStorageService>();
        var clock = new TestClock();
        var logger = new Mock<ILogger<UserImportProcessor>>();
        var userImportJobId = Guid.NewGuid();
        var storedFilename = $"{userImportJobId}.csv";
        var oversizedLastName = new string('a', User.LastNameMaxLength + 1);
        var csvContent = new StringBuilder();
        csvContent.AppendLine("ID,EMAIL_ADDRESS,FIRST_NAME,LAST_NAME,DATE_OF_BIRTH");
        csvContent.AppendLine($"UserImportProcessorTests12,UserImportProcessorTests12@email.com,joe,{oversizedLastName},05021970");
        using var csvStream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent.ToString()));

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            dbContext.UserImportJobs.Add(new UserImportJob()
            {
                UserImportJobId = userImportJobId,
                StoredFilename = storedFilename,
                OriginalFilename = "my-user-import.csv",
                UserImportJobStatus = UserImportJobStatus.New,
                Uploaded = clock.UtcNow
            });

            await dbContext.SaveChangesAsync();
        });

        userImportStorageService.Setup(s => s.OpenReadStream(storedFilename))
            .ReturnsAsync(csvStream)
            .Verifiable();

        // Act
        var userImportProcessor = new UserImportProcessor(
            dbContext,
            userImportStorageService.Object,
            clock,
            logger.Object);
        await userImportProcessor.Process(userImportJobId);

        // Assert
        userImportStorageService
            .Verify();

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            var user = await dbContext.Users.SingleOrDefaultAsync(r => r.EmailAddress == "UserImportProcessorTests12@email.com");
            Assert.Null(user);

            var events = await dbContext.Events.Where(e => e.EventName == "UserImportedEvent").ToListAsync();
            var userImportedEventCount = events
                .Select(e => JsonSerializer.Deserialize<UserImportedEvent>(e.Payload))
                .Count(i => i!.UserImportJobId == userImportJobId);
            Assert.Equal(0, userImportedEventCount);

            var userImportJobRow = await dbContext.UserImportJobRows.SingleOrDefaultAsync(r => r.UserImportJobId == userImportJobId);
            Assert.NotNull(userImportJobRow);
            Assert.NotNull(userImportJobRow.Errors);
            Assert.Single(userImportJobRow.Errors);
            Assert.Equal($"{UserImportRow.LastNameHeader} field should have a maximum of {User.LastNameMaxLength} characters", userImportJobRow.Errors[0]);
        });
    }

    [Fact]
    public async Task Process_WithCsvRowWithEmptyDateOfBirthField_InsertsUserImportJobRowWithErrorIntoDatabase()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var userImportStorageService = new Mock<IUserImportStorageService>();
        var clock = new TestClock();
        var logger = new Mock<ILogger<UserImportProcessor>>();
        var userImportJobId = Guid.NewGuid();
        var storedFilename = $"{userImportJobId}.csv";
        var csvContent = new StringBuilder();
        csvContent.AppendLine("ID,EMAIL_ADDRESS,FIRST_NAME,LAST_NAME,DATE_OF_BIRTH");
        csvContent.AppendLine("UserImportProcessorTests13,UserImportProcessorTests13@email.com,joe,bloggs,");
        using var csvStream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent.ToString()));

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            dbContext.UserImportJobs.Add(new UserImportJob()
            {
                UserImportJobId = userImportJobId,
                StoredFilename = storedFilename,
                OriginalFilename = "my-user-import.csv",
                UserImportJobStatus = UserImportJobStatus.New,
                Uploaded = clock.UtcNow
            });

            await dbContext.SaveChangesAsync();
        });

        userImportStorageService.Setup(s => s.OpenReadStream(storedFilename))
            .ReturnsAsync(csvStream)
            .Verifiable();

        // Act
        var userImportProcessor = new UserImportProcessor(
            dbContext,
            userImportStorageService.Object,
            clock,
            logger.Object);
        await userImportProcessor.Process(userImportJobId);

        // Assert
        userImportStorageService
            .Verify();

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            var user = await dbContext.Users.SingleOrDefaultAsync(r => r.EmailAddress == "UserImportProcessorTests13@email.com");
            Assert.Null(user);

            var events = await dbContext.Events.Where(e => e.EventName == "UserImportedEvent").ToListAsync();
            var userImportedEventCount = events
                .Select(e => JsonSerializer.Deserialize<UserImportedEvent>(e.Payload))
                .Count(i => i!.UserImportJobId == userImportJobId);
            Assert.Equal(0, userImportedEventCount);

            var userImportJobRow = await dbContext.UserImportJobRows.SingleOrDefaultAsync(r => r.UserImportJobId == userImportJobId);
            Assert.NotNull(userImportJobRow);
            Assert.NotNull(userImportJobRow.Errors);
            Assert.Single(userImportJobRow.Errors);
            Assert.Equal($"{UserImportRow.DateOfBirthHeader} field is empty", userImportJobRow.Errors[0]);
        });
    }

    [Fact]
    public async Task Process_WithCsvRowWithInvalidFormatDateOfBirthField_InsertsUserImportJobRowWithErrorIntoDatabase()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var userImportStorageService = new Mock<IUserImportStorageService>();
        var clock = new TestClock();
        var logger = new Mock<ILogger<UserImportProcessor>>();
        var userImportJobId = Guid.NewGuid();
        var storedFilename = $"{userImportJobId}.csv";
        var csvContent = new StringBuilder();
        csvContent.AppendLine("ID,EMAIL_ADDRESS,FIRST_NAME,LAST_NAME,DATE_OF_BIRTH");
        csvContent.AppendLine("UserImportProcessorTests14,UserImportProcessorTests14@email.com,joe,bloggs,12345678");
        using var csvStream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent.ToString()));

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            dbContext.UserImportJobs.Add(new UserImportJob()
            {
                UserImportJobId = userImportJobId,
                StoredFilename = storedFilename,
                OriginalFilename = "my-user-import.csv",
                UserImportJobStatus = UserImportJobStatus.New,
                Uploaded = clock.UtcNow
            });

            await dbContext.SaveChangesAsync();
        });

        userImportStorageService.Setup(s => s.OpenReadStream(storedFilename))
            .ReturnsAsync(csvStream)
            .Verifiable();

        // Act
        var userImportProcessor = new UserImportProcessor(
            dbContext,
            userImportStorageService.Object,
            clock,
            logger.Object);
        await userImportProcessor.Process(userImportJobId);

        // Assert
        userImportStorageService
            .Verify();

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            var user = await dbContext.Users.SingleOrDefaultAsync(r => r.EmailAddress == "UserImportProcessorTests14@email.com");
            Assert.Null(user);

            var events = await dbContext.Events.Where(e => e.EventName == "UserImportedEvent").ToListAsync();
            var userImportedEventCount = events
                .Select(e => JsonSerializer.Deserialize<UserImportedEvent>(e.Payload))
                .Count(i => i!.UserImportJobId == userImportJobId);
            Assert.Equal(0, userImportedEventCount);

            var userImportJobRow = await dbContext.UserImportJobRows.SingleOrDefaultAsync(r => r.UserImportJobId == userImportJobId);
            Assert.NotNull(userImportJobRow);
            Assert.NotNull(userImportJobRow.Errors);
            Assert.Single(userImportJobRow.Errors);
            Assert.Equal($"{UserImportRow.DateOfBirthHeader} field should be a valid date in ddMMyyyy format", userImportJobRow.Errors[0]);
        });
    }

    [Fact]
    public async Task Process_WithCsvRowWithDuplicateEmailAddressField_InsertsUserAndEventAndUserImportJobRowIntoDatabase()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var userImportStorageService = new Mock<IUserImportStorageService>();
        var clock = new TestClock();
        var logger = new Mock<ILogger<UserImportProcessor>>();
        var userImportJobId = Guid.NewGuid();
        var storedFilename = $"{userImportJobId}.csv";
        var emailAddressToDuplicate = "UserImportProcessorTests15@email.com";
        var existingUser = new User
        {
            UserId = Guid.NewGuid(),
            EmailAddress = emailAddressToDuplicate,
            FirstName = "Josephine",
            LastName = "Smith",
            Created = clock.UtcNow,
            Updated = clock.UtcNow,
            DateOfBirth = new DateOnly(1969, 12, 1),
            UserType = UserType.Teacher
        };
        var csvContent = new StringBuilder();
        csvContent.AppendLine("ID,EMAIL_ADDRESS,FIRST_NAME,LAST_NAME,DATE_OF_BIRTH");
        csvContent.AppendLine($"UserImportProcessorTests15,{emailAddressToDuplicate},joe,bloggs,05021970");
        using var csvStream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent.ToString()));

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            dbContext.UserImportJobs.Add(new UserImportJob()
            {
                UserImportJobId = userImportJobId,
                StoredFilename = storedFilename,
                OriginalFilename = "my-user-import.csv",
                UserImportJobStatus = UserImportJobStatus.New,
                Uploaded = clock.UtcNow
            });

            dbContext.Users.Add(existingUser);

            await dbContext.SaveChangesAsync();
        });

        userImportStorageService.Setup(s => s.OpenReadStream(storedFilename))
            .ReturnsAsync(csvStream)
            .Verifiable();

        // Act
        var userImportProcessor = new UserImportProcessor(
            dbContext,
            userImportStorageService.Object,
            clock,
            logger.Object);
        await userImportProcessor.Process(userImportJobId);

        // Assert
        userImportStorageService
            .Verify();

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            var events = await dbContext.Events.Where(e => e.EventName == "UserImportedEvent").ToListAsync();
            var userImportedEventCount = events
                .Select(e => JsonSerializer.Deserialize<UserImportedEvent>(e.Payload))
                .Count(i => i!.UserImportJobId == userImportJobId && i.User.UserId == existingUser.UserId);
            Assert.Equal(0, userImportedEventCount);

            var userImportJobRow = await dbContext.UserImportJobRows.SingleOrDefaultAsync(r => r.UserImportJobId == userImportJobId);
            Assert.NotNull(userImportJobRow);
            Assert.NotNull(userImportJobRow.Errors);
            Assert.Single(userImportJobRow.Errors);
            Assert.Equal("A user already exists with the specified email address", userImportJobRow.Errors[0]);
        });
    }

    [Fact]
    public async Task Process_WithCsvRowWithMultipleInvalidFields_InsertsUserImportJobRowWithMultipleErrorsIntoDatabase()
    {
        // Arrange
        var dbContext = _dbFixture.GetDbContext();
        var userImportStorageService = new Mock<IUserImportStorageService>();
        var clock = new TestClock();
        var logger = new Mock<ILogger<UserImportProcessor>>();
        var userImportJobId = Guid.NewGuid();
        var storedFilename = $"{userImportJobId}.csv";
        var csvContent = new StringBuilder();
        csvContent.AppendLine("ID,EMAIL_ADDRESS,FIRST_NAME,LAST_NAME,DATE_OF_BIRTH");
        csvContent.AppendLine(",UserImportProcessorTests16@email.com,,bloggs,12345678");
        using var csvStream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent.ToString()));

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            dbContext.UserImportJobs.Add(new UserImportJob()
            {
                UserImportJobId = userImportJobId,
                StoredFilename = storedFilename,
                OriginalFilename = "my-user-import.csv",
                UserImportJobStatus = UserImportJobStatus.New,
                Uploaded = clock.UtcNow
            });

            await dbContext.SaveChangesAsync();
        });

        userImportStorageService.Setup(s => s.OpenReadStream(storedFilename))
            .ReturnsAsync(csvStream)
            .Verifiable();

        // Act
        var userImportProcessor = new UserImportProcessor(
            dbContext,
            userImportStorageService.Object,
            clock,
            logger.Object);
        await userImportProcessor.Process(userImportJobId);

        // Assert
        userImportStorageService
            .Verify();

        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            var user = await dbContext.Users.SingleOrDefaultAsync(r => r.EmailAddress == "UserImportProcessorTests16@email.com");
            Assert.Null(user);

            var events = await dbContext.Events.Where(e => e.EventName == "UserImportedEvent").ToListAsync();
            var userImportedEventCount = events
                .Select(e => JsonSerializer.Deserialize<UserImportedEvent>(e.Payload))
                .Count(i => i!.UserImportJobId == userImportJobId);
            Assert.Equal(0, userImportedEventCount);

            var userImportJobRow = await dbContext.UserImportJobRows.SingleOrDefaultAsync(r => r.UserImportJobId == userImportJobId);
            Assert.NotNull(userImportJobRow);
            Assert.NotNull(userImportJobRow.Errors);
            Assert.Equal(3, userImportJobRow.Errors.Count);
            Assert.Contains($"{UserImportRow.IdHeader} field is empty", userImportJobRow.Errors);
            Assert.Contains($"{UserImportRow.FirstNameHeader} field is empty", userImportJobRow.Errors);
            Assert.Contains($"{UserImportRow.DateOfBirthHeader} field should be a valid date in ddMMyyyy format", userImportJobRow.Errors);
        });
    }
}
