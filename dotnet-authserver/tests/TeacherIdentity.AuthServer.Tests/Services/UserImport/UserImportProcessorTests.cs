using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.UserImport;
using TeacherIdentity.AuthServer.Services.UserSearch;
using User = TeacherIdentity.AuthServer.Models.User;

namespace TeacherIdentity.AuthServer.Tests.Services;

public class UserImportProcessorTests : IClassFixture<DbFixture>
{
    private readonly DbFixture _dbFixture;

    public UserImportProcessorTests(DbFixture dbFixture)
    {
        _dbFixture = dbFixture;
    }

    public static TheoryData<UserImportTestScenarioData> GetProcessImportData()
    {
        var clock = new TestClock();
        var uniqueSuffixTest17 = Guid.NewGuid().ToString();
        var uniqueSuffixTest18 = Guid.NewGuid().ToString();

        return new TheoryData<UserImportTestScenarioData>()
        {
            // 1. All valid row data
            new UserImportTestScenarioData
            {
                ExistingUser = null, // Existing user to pre-populate the database with before executing the test
                Id = "UserImportProcessorTests1", // ID
                EmailAddress = "UserImportProcessorTests1@email.com", // EMAIL_ADDRESS
                FirstName = Faker.Name.First(), // FIRST_NAME
                LastName = Faker.Name.Last(), // LAST_NAME
                DateOfBirth = "05021970", // DATE_OF_BIRTH
                FullRowDataOverride = string.Empty, // Full row data override
                UseMockUserSearchService = true, // Use mock search service to check for potential duplicate users 
                ExpectUserToBeInserted = true, // We expect a User record to be inserted
                ExpectUserImportedEventToBeInserted = true, // We expect a UserImportedEvent Event record to be inserted
                ExpectedErrors = null // Expected errors
            },
            // 2. Too few fields in the row data
            new UserImportTestScenarioData
            {
                ExistingUser = null,
                Id = string.Empty,
                EmailAddress = string.Empty,
                FirstName = string.Empty,
                LastName = string.Empty,
                DateOfBirth = string.Empty,
                FullRowDataOverride = "UserImportProcessorTests2,UserImportProcessorTests2@email.com,joe,bloggs",
                UseMockUserSearchService = true,
                ExpectUserToBeInserted = false,
                ExpectUserImportedEventToBeInserted = false,
                ExpectedErrors = new [] { $"Exactly {UserImportRow.ColumnCount} fields expected (this row has less)" }
            },
            new UserImportTestScenarioData
            // 3. Too many fields in the row data
            {
                ExistingUser = null,
                Id = string.Empty,
                EmailAddress = string.Empty,
                FirstName = string.Empty,
                LastName = string.Empty,
                DateOfBirth = string.Empty,
                FullRowDataOverride = "UserImportProcessorTests3,UserImportProcessorTests3@email.com,joe,bloggs,05021970,extra,data",
                UseMockUserSearchService = true,
                ExpectUserToBeInserted = false,
                ExpectUserImportedEventToBeInserted = false,
                ExpectedErrors = new [] { $"Exactly {UserImportRow.ColumnCount} fields expected (this row has more)" }
            },
            // 4. Empty ID field
            new UserImportTestScenarioData
            {
                ExistingUser = null,
                Id = string.Empty,
                EmailAddress = "UserImportProcessorTests4@email.com",
                FirstName = Faker.Name.First(),
                LastName = Faker.Name.Last(),
                DateOfBirth = "05021970",
                FullRowDataOverride = string.Empty,
                UseMockUserSearchService = true,
                ExpectUserToBeInserted = false,
                ExpectUserImportedEventToBeInserted = false,
                ExpectedErrors = new [] { $"{UserImportRow.IdHeader} field is empty" }
            },
            // 5. Oversized ID field
            new UserImportTestScenarioData
            {
                ExistingUser = null,
                Id = new string('a', UserImportJobRow.IdMaxLength + 1),
                EmailAddress = "UserImportProcessorTests5@email.com",
                FirstName = Faker.Name.First(),
                LastName = Faker.Name.Last(),
                DateOfBirth = "05021970",
                FullRowDataOverride = string.Empty,
                UseMockUserSearchService = true,
                ExpectUserToBeInserted = false,
                ExpectUserImportedEventToBeInserted = false,
                ExpectedErrors = new [] { $"{UserImportRow.IdHeader} field should have a maximum of {UserImportJobRow.IdMaxLength} characters" }
            },
            // 6. Empty EMAIL_ADDRESS field
            new UserImportTestScenarioData
            {
                ExistingUser = null,
                Id = "UserImportProcessorTests6",
                EmailAddress = string.Empty,
                FirstName = Faker.Name.First(),
                LastName = Faker.Name.Last(),
                DateOfBirth = "05021970",
                FullRowDataOverride = string.Empty,
                UseMockUserSearchService = true,
                ExpectUserToBeInserted = false,
                ExpectUserImportedEventToBeInserted = false,
                ExpectedErrors = new [] { $"{UserImportRow.EmailAddressHeader} field is empty" }
            },
            // 7. Oversized EMAIL_ADDRESS field
            new UserImportTestScenarioData
            {
                ExistingUser = null,
                Id = "UserImportProcessorTests7",
                EmailAddress = $"{new string('a', User.EmailAddressMaxLength + 1)}@email.com",
                FirstName = Faker.Name.First(),
                LastName = Faker.Name.Last(),
                DateOfBirth = "05021970",
                FullRowDataOverride = string.Empty,
                UseMockUserSearchService = true,
                ExpectUserToBeInserted = false,
                ExpectUserImportedEventToBeInserted = false,
                ExpectedErrors = new [] { $"{UserImportRow.EmailAddressHeader} field should have a maximum of {User.EmailAddressMaxLength} characters" }
            },
            // 8. Invalid format EMAIL_ADDRESS field
            new UserImportTestScenarioData
            {
                ExistingUser = null,
                Id = "UserImportProcessorTests8",
                EmailAddress = "UserImportProcessorTests8.com",
                FirstName = Faker.Name.First(),
                LastName = Faker.Name.Last(),
                DateOfBirth = "05021970",
                FullRowDataOverride = string.Empty,
                UseMockUserSearchService = true,
                ExpectUserToBeInserted = false,
                ExpectUserImportedEventToBeInserted = false,
                ExpectedErrors = new [] { $"{UserImportRow.EmailAddressHeader} field should be in a valid email address format" }
            },
            // 9. Empty FIRST_NAME field
            new UserImportTestScenarioData
            {
                ExistingUser = null,
                Id = "UserImportProcessorTests9",
                EmailAddress = "UserImportProcessorTests9@email.com",
                FirstName = string.Empty,
                LastName = Faker.Name.Last(),
                DateOfBirth = "05021970",
                FullRowDataOverride = string.Empty,
                UseMockUserSearchService = true,
                ExpectUserToBeInserted = false,
                ExpectUserImportedEventToBeInserted = false,
                ExpectedErrors = new [] { $"{UserImportRow.FirstNameHeader} field is empty" }
            },
            // 10. Oversized FIRST_NAME field
            new UserImportTestScenarioData
            {
                ExistingUser = null,
                Id = "UserImportProcessorTests10",
                EmailAddress = "UserImportProcessorTests10@email.com",
                FirstName = new string('a', User.FirstNameMaxLength + 1),
                LastName = Faker.Name.Last(),
                DateOfBirth = "05021970",
                FullRowDataOverride = string.Empty,
                UseMockUserSearchService = true,
                ExpectUserToBeInserted = false,
                ExpectUserImportedEventToBeInserted = false,
                ExpectedErrors = new [] { $"{UserImportRow.FirstNameHeader} field should have a maximum of {User.FirstNameMaxLength} characters" }
            },
            // 11. Empty LAST_NAME field
            new UserImportTestScenarioData
            {
                ExistingUser = null,
                Id = "UserImportProcessorTests11",
                EmailAddress = "UserImportProcessorTests11@email.com",
                FirstName = Faker.Name.First(),
                LastName = string.Empty,
                DateOfBirth = "05021970",
                FullRowDataOverride = string.Empty,
                UseMockUserSearchService = true,
                ExpectUserToBeInserted = false,
                ExpectUserImportedEventToBeInserted = false,
                ExpectedErrors = new [] { $"{UserImportRow.LastNameHeader} field is empty" }
            },
            // 12. Oversized LAST_NAME field
            new UserImportTestScenarioData
            {
                ExistingUser = null,
                Id = "UserImportProcessorTests12",
                EmailAddress = "UserImportProcessorTests12@email.com",
                FirstName = string.Empty,
                LastName = new string('a', User.LastNameMaxLength + 1),
                DateOfBirth = "05021970",
                FullRowDataOverride = string.Empty,
                UseMockUserSearchService = true,
                ExpectUserToBeInserted = false,
                ExpectUserImportedEventToBeInserted = false,
                ExpectedErrors = new [] { $"{UserImportRow.LastNameHeader} field should have a maximum of {User.LastNameMaxLength} characters" }
            },
            // 13. Empty DATE_OF_BIRTH field
            new UserImportTestScenarioData
            {
                ExistingUser = null,
                Id = "UserImportProcessorTests13",
                EmailAddress = "UserImportProcessorTests13@email.com",
                FirstName = Faker.Name.First(),
                LastName = Faker.Name.Last(),
                DateOfBirth = string.Empty,
                FullRowDataOverride = string.Empty,
                UseMockUserSearchService = true,
                ExpectUserToBeInserted = false,
                ExpectUserImportedEventToBeInserted = false,
                ExpectedErrors = new [] { $"{UserImportRow.DateOfBirthHeader} field is empty" }
            },
            // 14. Invalid format DATE_OF_BIRTH field
            new UserImportTestScenarioData
            {
                ExistingUser = null,
                Id = "UserImportProcessorTests14",
                EmailAddress = "UserImportProcessorTests14@email.com",
                FirstName = Faker.Name.First(),
                LastName = Faker.Name.Last(),
                DateOfBirth = "12345678",
                FullRowDataOverride = string.Empty,
                UseMockUserSearchService = true,
                ExpectUserToBeInserted = false,
                ExpectUserImportedEventToBeInserted = false,
                ExpectedErrors = new [] { $"{UserImportRow.DateOfBirthHeader} field should be a valid date in ddMMyyyy format" }
            },
            // 15. Duplicate EMAIL_ADDRESS field
            new UserImportTestScenarioData
            {
                ExistingUser = new User
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
                Id = "UserImportProcessorTests15",
                EmailAddress = "UserImportProcessorTests15@email.com",
                FirstName = Faker.Name.First(),
                LastName = Faker.Name.Last(),
                DateOfBirth = "05021970",
                FullRowDataOverride = string.Empty,
                UseMockUserSearchService = true,
                ExpectUserToBeInserted = false,
                ExpectUserImportedEventToBeInserted = false,
                ExpectedErrors = new [] { "A user already exists with the specified email address" }
            },
            // 16. Multiple invalid fields
            new UserImportTestScenarioData
            {
                ExistingUser = null,
                Id = string.Empty,
                EmailAddress = "UserImportProcessorTests16@email.com",
                FirstName = string.Empty,
                LastName = Faker.Name.Last(),
                DateOfBirth = "12345678",
                FullRowDataOverride = string.Empty,
                UseMockUserSearchService = true,
                ExpectUserToBeInserted = false,
                ExpectUserImportedEventToBeInserted = false,
                ExpectedErrors = new []
                {
                    $"{UserImportRow.IdHeader} field is empty",
                    $"{UserImportRow.FirstNameHeader} field is empty",
                    $"{UserImportRow.DateOfBirthHeader} field should be a valid date in ddMMyyyy format"
                }
            },
            // 17. Potential duplicate based on first name, last name and date of birth
            new UserImportTestScenarioData
            {
                ExistingUser = new User
                {
                    UserId = Guid.NewGuid(),
                    EmailAddress = $"UserImportProcessorTest{uniqueSuffixTest17}@email.com",
                    FirstName = "Josephine",
                    LastName = $"Smith{uniqueSuffixTest17}",
                    Created = clock.UtcNow,
                    Updated = clock.UtcNow,
                    DateOfBirth = new DateOnly(1970, 2, 5),
                    UserType = UserType.Teacher
                },
                Id = "UserImportProcessorTests17",
                EmailAddress = "UserImportProcessorTests17@email.com",
                FirstName = "Josephine",
                LastName = $"Smith{uniqueSuffixTest17}",
                DateOfBirth = "05021970",
                FullRowDataOverride = string.Empty,
                UseMockUserSearchService = false,
                ExpectUserToBeInserted = false,
                ExpectUserImportedEventToBeInserted = false,
                ExpectedErrors = new [] { "Potential duplicate user" }
            },
            // 18. Potential duplicate based on synonym of first name, last name and date of birth
            new UserImportTestScenarioData
            {
                ExistingUser = new User
                {
                    UserId = Guid.NewGuid(),
                    EmailAddress = $"UserImportProcessorTest{uniqueSuffixTest18}@email.com",
                    FirstName = "Josephine",
                    LastName = $"Smith{uniqueSuffixTest18}",
                    Created = clock.UtcNow,
                    Updated = clock.UtcNow,
                    DateOfBirth = new DateOnly(1970, 2, 5),
                    UserType = UserType.Teacher
                },
                Id = "UserImportProcessorTests18",
                EmailAddress = "UserImportProcessorTests18@email.com",
                FirstName = "Jo",
                LastName = $"Smith{uniqueSuffixTest18}",
                DateOfBirth = "05021970",
                FullRowDataOverride = string.Empty,
                UseMockUserSearchService = false,
                ExpectUserToBeInserted = false,
                ExpectUserImportedEventToBeInserted = false,
                ExpectedErrors = new [] { "Potential duplicate user" }
            }
        };
    }

    [Theory]
    [MemberData(nameof(GetProcessImportData))]
    public async Task Process_WithTheoryCsvRowData_InsertsExpectedUserAndEventAndUserImportJobRowWithExpectedErrorsIntoDatabase(UserImportTestScenarioData testScenarioData)
    {
        // Arrange
        using var dbContext = _dbFixture.GetDbContext();
        var userImportStorageService = Mock.Of<IUserImportStorageService>();
        var mocUserSearchService = Mock.Of<IUserSearchService>();
        IUserSearchService userSearchService;
        if (testScenarioData.UseMockUserSearchService)
        {
            userSearchService = Mock.Of<IUserSearchService>();
            Mock.Get(userSearchService)
                .Setup(s => s.FindUsers(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<bool>()))
                .ReturnsAsync(new User[] { });
        }
        else
        {
            userSearchService = new UserSearchService(dbContext, new NameSynonymsService());
        }

        var clock = new TestClock();
        var logger = Mock.Of<ILogger<UserImportProcessor>>();
        var userImportJobId = Guid.NewGuid();
        var storedFilename = $"{userImportJobId}.csv";
        var csvContent = new StringBuilder();
        csvContent.AppendLine("ID,EMAIL_ADDRESS,FIRST_NAME,LAST_NAME,DATE_OF_BIRTH");
        if (!string.IsNullOrEmpty(testScenarioData.FullRowDataOverride))
        {
            csvContent.AppendLine(testScenarioData.FullRowDataOverride);
        }
        else
        {
            csvContent.AppendLine($"{testScenarioData.Id},{testScenarioData.EmailAddress},{testScenarioData.FirstName},{testScenarioData.LastName},{testScenarioData.DateOfBirth}");
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

            if (testScenarioData.ExistingUser != null)
            {
                dbContext.Users.Add(testScenarioData.ExistingUser);
            }

            await dbContext.SaveChangesAsync();
        });

        Mock.Get(userImportStorageService)
            .Setup(s => s.OpenReadStream(storedFilename))
            .ReturnsAsync(csvStream);

        // Act
        var userImportProcessor = new UserImportProcessor(
            dbContext,
            userImportStorageService,
            userSearchService,
            clock,
            logger);
        await userImportProcessor.Process(userImportJobId);

        // Assert
        await _dbFixture.TestData.WithDbContext(async dbContext =>
        {
            if (testScenarioData.ExistingUser == null)
            {
                var user = await dbContext.Users.SingleOrDefaultAsync(r => r.EmailAddress == testScenarioData.EmailAddress);
                if (testScenarioData.ExpectUserToBeInserted)
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

            if (testScenarioData.ExpectUserImportedEventToBeInserted)
            {
                Assert.NotNull(userImportedEvent);
            }
            else
            {
                Assert.Null(userImportedEvent);
            }

            var userImportJobRow = await dbContext.UserImportJobRows.SingleOrDefaultAsync(r => r.UserImportJobId == userImportJobId);
            Assert.NotNull(userImportJobRow);

            if (testScenarioData.ExpectedErrors != null && testScenarioData.ExpectedErrors.Length > 0)
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

public class UserImportTestScenarioData
{
    public required User? ExistingUser { get; set; }
    public required string Id { get; set; }
    public required string EmailAddress { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string DateOfBirth { get; set; }
    public required string FullRowDataOverride { get; set; }
    public required bool UseMockUserSearchService { get; set; }
    public required bool ExpectUserToBeInserted { get; set; }
    public required bool ExpectUserImportedEventToBeInserted { get; set; }
    public required string[]? ExpectedErrors { get; set; }
}
