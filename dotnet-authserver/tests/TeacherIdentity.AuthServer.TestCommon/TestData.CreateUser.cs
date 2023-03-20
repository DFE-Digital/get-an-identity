using System.Text.RegularExpressions;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.TestCommon;

public partial class TestData
{
    public Task<User> CreateUser(
            string? email = null,
            bool? hasTrn = null,
            bool? haveCompletedTrnLookup = null,
            UserType userType = UserType.Default,
            string? registeredWithClientId = null,
            Guid? mergedWithUserId = null,
            TrnLookupStatus? trnLookupStatus = null,
            string? firstName = null,
            string[]? staffRoles = null,
            bool? hasMobileNumber = true) =>
        WithDbContext(async dbContext =>
        {
            if (hasTrn == true && userType != UserType.Default)
            {
                throw new ArgumentException($"Only {UserType.Default} users should have a TRN.");
            }

            if (trnLookupStatus is not null && userType != UserType.Default)
            {
                throw new ArgumentException($"{nameof(trnLookupStatus)} can only be set for {UserType.Default} users.");
            }

            if (hasTrn == true && (trnLookupStatus ?? TrnLookupStatus.Found) != TrnLookupStatus.Found)
            {
                throw new ArgumentException($"{nameof(TrnLookupStatus)} must be {TrnLookupStatus.Found} when the user has a TRN.");
            }
            else if (hasTrn == false && trnLookupStatus == TrnLookupStatus.Found)
            {
                throw new ArgumentException($"{nameof(TrnLookupStatus)} cannot be {TrnLookupStatus.Found} when the user does not have a TRN.");
            }

            if (userType == UserType.Default && hasTrn is null && trnLookupStatus is not null)
            {
                hasTrn = trnLookupStatus == TrnLookupStatus.Found;
            }

            if (haveCompletedTrnLookup == true && userType == UserType.Default)
            {
                throw new ArgumentException($"{userType} users should not have {nameof(User.CompletedTrnLookup)} set.");
            }

            var normalisedMobileNumber =
                hasMobileNumber == true ? Regex.Replace(Faker.Phone.Number(), @"^[^\d\+]|(?<=.)[^\d]", "") : null;

            var user = new User()
            {
                UserId = Guid.NewGuid(),
                EmailAddress = email ?? Faker.Internet.Email(),
                MobileNumber = normalisedMobileNumber,
                FirstName = firstName ?? Faker.Name.First(),
                LastName = Faker.Name.Last(),
                Created = _clock.UtcNow,
                CompletedTrnLookup = userType is UserType.Default && haveCompletedTrnLookup != false ? _clock.UtcNow : null,
                UserType = userType,
                DateOfBirth = userType is UserType.Default ? DateOnly.FromDateTime(Faker.Identification.DateOfBirth()) : null,
                Trn = hasTrn == true ? GenerateTrn() : null,
                TrnAssociationSource = hasTrn == true ? (haveCompletedTrnLookup != false ? TrnAssociationSource.Lookup : TrnAssociationSource.Api) : null,
                TrnLookupStatus = trnLookupStatus ?? (userType == UserType.Default ? (hasTrn == true ? TrnLookupStatus.Found : TrnLookupStatus.None) : null),
                Updated = _clock.UtcNow,
                RegisteredWithClientId = registeredWithClientId,
                MergedWithUserId = mergedWithUserId,
                IsDeleted = mergedWithUserId != null,
                StaffRoles = userType is UserType.Staff ? staffRoles ?? StaffRoles.All : Array.Empty<string>()
            };

            dbContext.Users.Add(user);

            await dbContext.SaveChangesAsync();

            return user;
        });
}
