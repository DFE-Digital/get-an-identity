using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Tests;

public partial class TestData
{
    public Task<User> CreateUser(string? email = null, bool haveCompletedTrnLookup = true) => WithDbContext(async dbContext =>
    {
        var user = new User()
        {
            UserId = Guid.NewGuid(),
            EmailAddress = email ?? Faker.Internet.Email(),
            FirstName = Faker.Name.First(),
            LastName = Faker.Name.Last(),
            Created = _clock.UtcNow,
            CompletedTrnLookup = haveCompletedTrnLookup ? _clock.UtcNow : null
        };

        dbContext.Users.Add(user);

        await dbContext.SaveChangesAsync();

        return user;
    });
}
