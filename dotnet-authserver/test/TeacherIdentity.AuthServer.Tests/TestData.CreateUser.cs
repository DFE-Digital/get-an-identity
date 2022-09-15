using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Tests;

public partial class TestData
{
    public Task<User> CreateUser(string? email = null, bool? hasTrn = null, bool? haveCompletedTrnLookup = null, UserType userType = UserType.Teacher) =>
        WithDbContext(async dbContext =>
        {
            if (hasTrn is null && userType == UserType.Teacher)
            {
                hasTrn = true;
            }
            else if (hasTrn == true && userType != UserType.Teacher)
            {
                throw new ArgumentException($"Only {UserType.Teacher} users should have a TRN.");
            }

            if (haveCompletedTrnLookup == true && userType == UserType.Teacher)
            {
                throw new ArgumentException($"{userType} users should not have {nameof(User.CompletedTrnLookup)} set.");
            }
            else if (haveCompletedTrnLookup == false && hasTrn == true)
            {
                throw new ArgumentException($"Users with a TRN should have {nameof(User.CompletedTrnLookup)} set.");
            }

            var user = new User()
            {
                UserId = Guid.NewGuid(),
                EmailAddress = email ?? Faker.Internet.Email(),
                FirstName = Faker.Name.First(),
                LastName = Faker.Name.Last(),
                Created = _clock.UtcNow,
                CompletedTrnLookup = userType is UserType.Teacher && haveCompletedTrnLookup != false ? _clock.UtcNow : null,
                UserType = userType,
                DateOfBirth = userType is UserType.Teacher ? DateOnly.FromDateTime(Faker.Identification.DateOfBirth()) : null,
                Trn = hasTrn == true ? GenerateTrn() : null
            };

            dbContext.Users.Add(user);

            await dbContext.SaveChangesAsync();

            return user;
        });
}
