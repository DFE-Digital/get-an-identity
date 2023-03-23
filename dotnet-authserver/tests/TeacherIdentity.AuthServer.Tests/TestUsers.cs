using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Tests;

public static class TestUsers
{
    public static User AdminUserWithAllRoles { get; } = new User()
    {
        UserId = new Guid("60276e94-736c-4bbc-b504-94afb61789e1"),
        Created = DateTime.UtcNow,
        DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
        EmailAddress = Faker.Internet.Email(),
        FirstName = Faker.Name.First(),
        LastName = Faker.Name.Last(),
        Updated = DateTime.UtcNow,
        UserType = UserType.Staff,
        StaffRoles = StaffRoles.All
    };

    public static User AdminUserWithNoRoles { get; } = new User()
    {
        UserId = new Guid("1a049cf0-6c0f-4157-a77d-4b081145a05a"),
        Created = DateTime.UtcNow,
        DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
        EmailAddress = Faker.Internet.Email(),
        FirstName = Faker.Name.First(),
        LastName = Faker.Name.Last(),
        Updated = DateTime.UtcNow,
        UserType = UserType.Staff,
        StaffRoles = StaffRoles.None
    };

    public static User DefaultUser { get; } = new User()
    {
        UserId = new Guid("a9fa306d-c5f2-4225-b3d6-45d5dd8948c0"),
        Created = DateTime.UtcNow,
        DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
        EmailAddress = Faker.Internet.Email(),
        FirstName = Faker.Name.First(),
        LastName = Faker.Name.Last(),
        Updated = DateTime.UtcNow,
        UserType = UserType.Default,
        StaffRoles = StaffRoles.None
    };

    public static User DefaultUserWithTrn { get; } = new User()
    {
        UserId = new Guid("3828dc03-7500-402a-beeb-acd45b635fc3"),
        Created = DateTime.UtcNow,
        DateOfBirth = DateOnly.FromDateTime(Faker.Identification.DateOfBirth()),
        EmailAddress = Faker.Internet.Email(),
        FirstName = Faker.Name.First(),
        LastName = Faker.Name.Last(),
        Updated = DateTime.UtcNow,
        UserType = UserType.Default,
        StaffRoles = StaffRoles.None,
        Trn = "7654321",
        TrnLookupStatus = TrnLookupStatus.Found,
    };

    public static IReadOnlyCollection<User> All => Default.Concat(Staff).ToArray();

    public static IReadOnlyCollection<User> Default => new[] { DefaultUser, DefaultUserWithTrn };

    public static IReadOnlyCollection<User> Staff => new[] { AdminUserWithAllRoles, AdminUserWithNoRoles };

    public static async Task CreateUsers(TeacherIdentityServerDbContext dbContext)
    {
        foreach (var user in All)
        {
            dbContext.Add(user);
        }

        await dbContext.SaveChangesAsync();
    }

    public static async Task DeleteNonTestUsers(TeacherIdentityServerDbContext dbContext)
    {
        var nonTestUsers = dbContext.Users.IgnoreQueryFilters().Where(u => !All.Select(u => u.UserId).Contains(u.UserId));
        dbContext.JourneyTrnLookupStates.RemoveRange(dbContext.JourneyTrnLookupStates);
        dbContext.Users.RemoveRange(nonTestUsers);
        await dbContext.SaveChangesAsync();
    }
}
