using System.Diagnostics;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer;

public class UserClaimHelper
{
    private readonly TeacherIdentityServerDbContext _dbContext;

    public UserClaimHelper(TeacherIdentityServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public static IReadOnlyCollection<Claim> GetInternalClaims(AuthenticationState authenticationState)
    {
        if (!authenticationState.IsComplete)
        {
            throw new InvalidOperationException("Cannot retrieve claims until authentication is complete.");
        }

        return GetInternalClaims(
            authenticationState.UserId!.Value,
            authenticationState.EmailAddress!,
            authenticationState.FirstName!,
            authenticationState.MiddleName,
            authenticationState.LastName!,
            authenticationState.Trn,
            authenticationState.UserType!.Value,
            authenticationState.StaffRoles);
    }

    public static IReadOnlyCollection<Claim> GetInternalClaims(User user)
    {
        return GetInternalClaims(
            user.UserId,
            user.EmailAddress,
            user.FirstName,
            user.MiddleName,
            user.LastName,
            user.Trn,
            user.UserType,
            user.StaffRoles);
    }

    public static string MapUserTypeToClaimValue(UserType userType) => userType.ToString();

    public async Task<IReadOnlyCollection<Claim>> GetPublicClaims(Guid userId, Func<string, bool> hasScope)
    {
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.UserId == userId);

        if (user is null)
        {
            return Array.Empty<Claim>();
        }

        var fullName = user.MiddleName is null
            ? $"{user.FirstName} {user.LastName}"
            : $"{user.FirstName} {user.MiddleName} {user.LastName}";

        var claims = new List<Claim>()
        {
            new Claim(Claims.Subject, userId.ToString()!),
            new Claim(Claims.Email, user.EmailAddress),
            new Claim(Claims.EmailVerified, bool.TrueString),
            new Claim(Claims.Name, fullName),
            new Claim(CustomClaims.PreferredName, $"{user.FirstName} {user.LastName}"),
            new Claim(Claims.GivenName, user.FirstName),
            new Claim(Claims.FamilyName, user.LastName),
        };

        if (user.MiddleName is not null)
        {
            claims.Add(new Claim(Claims.MiddleName, user.MiddleName));
        }

        if (user.DateOfBirth is DateOnly dateOfBirth)
        {
            claims.Add(new Claim(Claims.Birthdate, dateOfBirth.ToString(CustomClaims.DateFormat)));
        }

        if (user.MobileNumber is not null)
        {
            claims.Add(new Claim(Claims.PhoneNumber, user.MobileNumber));
            claims.Add(new Claim(Claims.PhoneNumberVerified, bool.TrueString));
        }

        if (UserRequirementsExtensions.GetUserRequirementsForScopes(hasScope).RequiresTrnLookup())
        {
            Debug.Assert(user.TrnLookupStatus.HasValue);
            claims.Add(new Claim(CustomClaims.TrnLookupStatus, user.TrnLookupStatus!.Value.ToString()));

            if (user.Trn is not null)
            {
                claims.Add(new Claim(CustomClaims.Trn, user.Trn));
            }
        }

        await _dbContext.Users.IgnoreQueryFilters()
            .Where(u => u.MergedWithUserId == userId)
            .ForEachAsync(u => claims.Add(new Claim(CustomClaims.PreviousUserId, u.UserId.ToString())));

        return claims;
    }

    private static IReadOnlyCollection<Claim> GetInternalClaims(
        Guid userId,
        string email,
        string firstName,
        string? middleName,
        string lastName,
        string? trn,
        UserType userType,
        string[]? staffRoles)
    {
        var fullName = middleName is null
            ? $"{firstName} {lastName}"
            : $"{firstName} {middleName} {lastName}";

        var claims = new List<Claim>()
        {
            new Claim(Claims.Subject, userId.ToString()!),
            new Claim(Claims.Email, email),
            new Claim(Claims.Name, fullName),
            new Claim(Claims.GivenName, firstName),
            new Claim(Claims.FamilyName, lastName),
            new Claim(CustomClaims.UserType, MapUserTypeToClaimValue(userType))
        };

        if (middleName is not null)
        {
            claims.Add(new Claim(Claims.MiddleName, middleName));
        }

        if (trn is not null)
        {
            claims.Add(new Claim(CustomClaims.Trn, trn));
        }

        foreach (var role in staffRoles ?? Array.Empty<string>())
        {
            claims.Add(new Claim(Claims.Role, role));
        }

        return claims;
    }
}
