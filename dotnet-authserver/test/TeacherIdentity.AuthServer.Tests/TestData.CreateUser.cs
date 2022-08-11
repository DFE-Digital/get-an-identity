using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Tests;

public partial class TestData
{
    public Task<User> CreateUser(string? email = null, string? trn = null) => WithDbContext(async dbContext =>
    {
        var user = new User()
        {
            UserId = Guid.NewGuid(),
            EmailAddress = email ?? Faker.Internet.Email(),
            FirstName = Faker.Name.First(),
            LastName = Faker.Name.Last(),
            Trn = trn
        };

        dbContext.Users.Add(user);

        await dbContext.SaveChangesAsync();

        return user;
    });
}
