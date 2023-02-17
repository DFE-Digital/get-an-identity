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

    public static TheoryData<User?, string, string, string, string, string, string, bool, bool, string[]?> GetProcessImportData()
    {
        var clock = new TestClock();

        return new TheoryData<User?, string, string, string, string, string, string, bool, bool, string[]?>()
        {
            // 1. All valid row data
            {
                null, // Existing user to pre-populate the database with before executing the test
                "UserImportProcessorTests1", // ID
                "UserImportProcessorTests1@email.com", // EMAIL_ADDRESS
                Faker.Name.First(), // FIRST_NAME
                Faker.Name.Last(), // LAST_NAME
                "05021970", // DATE_OF_BIRTH
                string.Empty, // Full row data override
                true, // We expect a User record to be inserted
                true, // We expect a UserImportedEvent Event record to be inserted
                null // Expected errors
            },
            // 2. Too few fields in the row data
            {
                null,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                "UserImportProcessorTests2,UserImportProcessorTests2@email.com,joe,bloggs",
                false,
                false,
                new [] { $"Exactly {UserImportRow.ColumnCount} fields expected (this row has less)" }
            },
            // 3. Too many fields in the row data
            {
                null,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                "UserImportProcessorTests3,UserImportProcessorTests3@email.com,joe,bloggs,05021970,extra,data",
                false,
                false,
                new [] { $"Exactly {UserImportRow.ColumnCount} fields expected (this row has more)" }
            },
            // 4. Empty ID field
            {
                null,
                string.Empty,
                "UserImportProcessorTests4@email.com",
                Faker.Name.First(),
                Faker.Name.Last(),
                "05021970",
                string.Empty,
                false,
                false,
                new [] { $"{UserImportRow.IdHeader} field is empty" }
            },
            // 5. Oversized ID field
            {
                null,
                new string('a', UserImportJobRow.IdMaxLength + 1),
                "UserImportProcessorTests5@email.com",
                Faker.Name.First(),
                Faker.Name.Last(),
                "05021970",
                string.Empty,
                false,
                false,
                new [] { $"{UserImportRow.IdHeader} field should have a maximum of {UserImportJobRow.IdMaxLength} characters" }
            },
            // 6. Empty EMAIL_ADDRESS field
            {
                null,
                "UserImportProcessorTests6",
                string.Empty,
                Faker.Name.First(),
                Faker.Name.Last(),
                "05021970",
                string.Empty,
                false,
                false,
                new [] { $"{UserImportRow.EmailAddressHeader} field is empty" }
            },
            // 7. Oversized EMAIL_ADDRESS field
            {
                null,
                "UserImportProcessorTests7",
                $"{new string('a', User.EmailAddressMaxLength + 1)}@email.com",
                Faker.Name.First(),
                Faker.Name.Last(),
                "05021970",
                string.Empty,
                false,
                false,
                new [] { $"{UserImportRow.EmailAddressHeader} field should have a maximum of {User.EmailAddressMaxLength} characters" }
            },
            // 8. Invalid format EMAIL_ADDRESS field
            {
                null,
                "UserImportProcessorTests8",
                "UserImportProcessorTests8.com",
                Faker.Name.First(),
                Faker.Name.Last(),
                "05021970",
                string.Empty,
                false,
                false,
                new [] { $"{UserImportRow.EmailAddressHeader} field should be in a valid email address format" }
            },
            // 9. Empty FIRST_NAME field
            {
                null,
                "UserImportProcessorTests9",
                "UserImportProcessorTests9@email.com",
                string.Empty,
                Faker.Name.Last(),
                "05021970",
                string.Empty,
                false,
                false,
                new [] { $"{UserImportRow.FirstNameHeader} field is empty" }
            },
            // 10. Oversized FIRST_NAME field
            {
                null,
                "UserImportProcessorTests10",
                "UserImportProcessorTests10@email.com",
                new string('a', User.FirstNameMaxLength + 1),
                Faker.Name.Last(),
                "05021970",
                string.Empty,
                false,
                false,
                new [] { $"{UserImportRow.FirstNameHeader} field should have a maximum of {User.FirstNameMaxLength} characters" }
            },
            // 11. Empty LAST_NAME field
            {
                null,
                "UserImportProcessorTests11",
                "UserImportProcessorTests11@email.com",
                Faker.Name.First(),
                string.Empty,
                "05021970",
                string.Empty,
                false,
                false,
                new [] { $"{UserImportRow.LastNameHeader} field is empty" }
            },
            // 12. Oversized LAST_NAME field
            {
                null,
                "UserImportProcessorTests12",
                "UserImportProcessorTests12@email.com",
                string.Empty,
                new string('a', User.LastNameMaxLength + 1),
                "05021970",
                string.Empty,
                false,
                false,
                new [] { $"{UserImportRow.LastNameHeader} field should have a maximum of {User.LastNameMaxLength} characters" }
            },
            // 13. Empty DATE_OF_BIRTH field
            {
                null,
                "UserImportProcessorTests13",
                "UserImportProcessorTests13@email.com",
                Faker.Name.First(),
                Faker.Name.Last(),
                string.Empty,
                string.Empty,
                false,
                false,
                new [] { $"{UserImportRow.DateOfBirthHeader} field is empty" }
            },
            // 14. Invalid format DATE_OF_BIRTH field
            {
                null,
                "UserImportProcessorTests14",
                "UserImportProcessorTests14@email.com",
                Faker.Name.First(),
                Faker.Name.Last(),
                "12345678",
                string.Empty,
                false,
                false,
                new [] { $"{UserImportRow.DateOfBirthHeader} field should be a valid date in ddMMyyyy format" }
            },
            // 15. Duplicate EMAIL_ADDRESS field
            {
                new User
                {
                    UserId = Guid.NewGuid(),
                    EmailAddress = "UserImportProcessorTests15@email.com",
                    FirstName = "Josephine",
                    LastName = "Smith",
                    Created = clock.UtcNow,
                    Updated = clock.UtcNow,
                    DateOfBirth = new DateOnly(1969, 12, 1),
                    UserType = UserType.Teacher
                },
                "UserImportProcessorTests15",
                "UserImportProcessorTests15@email.com",
                Faker.Name.First(),
                Faker.Name.Last(),
                "05021970",
                string.Empty,
                false,
                false,
                new [] { "A user already exists with the specified email address" }
            },
            // 16. Multiple invalid fields
            {
                null,
                string.Empty,
                "UserImportProcessorTests16@email.com",
                string.Empty,
                Faker.Name.Last(),
                "12345678",
                string.Empty,
                false,
                false,
                new []
                {
                    $"{UserImportRow.IdHeader} field is empty",
                    $"{UserImportRow.FirstNameHeader} field is empty",
                    $"{UserImportRow.DateOfBirthHeader} field should be a valid date in ddMMyyyy format"
                }
            }
        };
    }

    [Theory]
    [MemberData(nameof(GetProcessImportData))]
    public async Task Process_WithTheoryCsvRowData_InsertsExpectedUserAndEventAndUserImportJobRowWithExpectedErrorsIntoDatabase(
        User? existingUser,
        string id,
        string emailAddress,
        string firstName,
        string lastName,
        string dateOfBirth,
        string fullRowDataOverride,
        bool expectUserToBeInserted,
        bool expectUserImportedEventToBeInserted,
        string[]? expectedErrors
        )
    {
        // Arrange
        using var dbContext = _dbFixture.GetDbContext();
        var userImportStorageService = new Mock<IUserImportStorageService>();
        var userSearchService = new Mock<IUserSearchService>();
        var clock = new TestClock();
        var logger = new Mock<ILogger<UserImportProcessor>>();
        var userImportJobId = Guid.NewGuid();
        var storedFilename = $"{userImportJobId}.csv";
        var csvContent = new StringBuilder();
        csvContent.AppendLine("ID,EMAIL_ADDRESS,FIRST_NAME,LAST_NAME,DATE_OF_BIRTH");
        if (!string.IsNullOrEmpty(fullRowDataOverride))
        {
            csvContent.AppendLine(fullRowDataOverride);
        }
        else
        {
            csvContent.AppendLine($"{id},{emailAddress},{firstName},{lastName},{dateOfBirth}");
        }
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

            if (existingUser != null)
            {
                dbContext.Users.Add(existingUser);
            }

            await dbContext.SaveChangesAsync();
        });

        userImportStorageService.Setup(s => s.OpenReadStream(storedFilename))
            .ReturnsAsync(csvStream);
        userSearchService.Setup(s => s.FindUsers(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<bool>()))
            .ReturnsAsync(new User[] { });

        // Act
        var userImportProcessor = new UserImportProcessor(
            dbContext,
            userImportStorageService.Object,
            userSearchService.Object,
            clock,
            logger.Object);
        await userImportProcessor.Process(userImportJobId);

        // Assert
        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            if (existingUser == null)
            {
                var user = await dbContext.Users.SingleOrDefaultAsync(r => r.EmailAddress == emailAddress);
                if (expectUserToBeInserted)
                {
                    Assert.NotNull(user);
                }
                else
                {
                    Assert.Null(user);
                }
            }

            var events = await dbContext.Events
                                .Where(e => e.EventName == "UserImportedEvent").ToListAsync();
            var userImportedEvent = events
                .Select(e => JsonSerializer.Deserialize<UserImportedEvent>(e.Payload))
                .Where(i => i!.UserImportJobId == userImportJobId)
                .SingleOrDefault();

            if (expectUserImportedEventToBeInserted)
            {
                Assert.NotNull(userImportedEvent);
            }
            else
            {
                Assert.Null(userImportedEvent);
            }

            var userImportJobRow = await dbContext.UserImportJobRows.SingleOrDefaultAsync(r => r.UserImportJobId == userImportJobId);
            Assert.NotNull(userImportJobRow);

            if (expectedErrors != null && expectedErrors.Length > 0)
            {
                Assert.NotNull(userImportJobRow.Errors);
                var elementInspectors = new List<Action<string>>();
                foreach (var error in userImportJobRow!.Errors)
                {
                    elementInspectors.Add(e => Assert.Equal(error, e));
                }

                Assert.Collection(userImportJobRow!.Errors, elementInspectors.ToArray());
            }
        });
    }
}
