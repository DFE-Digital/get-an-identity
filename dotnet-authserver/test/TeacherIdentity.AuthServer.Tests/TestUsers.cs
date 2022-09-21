using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Tests;

public static class TestUsers
{
    public static User AdminUser1 { get; } = new User()
    {
        UserId = new Guid("60276e94-736c-4bbc-b504-94afb61789e1"),
        Created = DateTime.UtcNow,
        DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
        EmailAddress = Faker.Internet.Email(),
        FirstName = Faker.Name.First(),
        LastName = Faker.Name.Last(),
        UserType = UserType.Staff
    };

    public static async Task CreateUsers(TeacherIdentityServerDbContext dbContext)
    {
        var allUsers = new[] { AdminUser1 };

        foreach (var user in allUsers)
        {
            dbContext.Add(user);
        }

        await dbContext.SaveChangesAsync();
    }
}
