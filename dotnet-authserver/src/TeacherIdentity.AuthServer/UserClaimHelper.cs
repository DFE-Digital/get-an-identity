using System.Diagnostics;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services.DqtApi;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer;

public class UserClaimHelper
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IDqtApiClient _dqtApiClient;

    public UserClaimHelper(TeacherIdentityServerDbContext dbContext, IDqtApiClient dqtApiClient)
    {
        _dbContext = dbContext;
        _dqtApiClient = dqtApiClient;
    }

    public static IReadOnlyCollection<Claim> GetInternalClaims(AuthenticationState authenticationState)
    {
        if (!authenticationState.UserId.HasValue)
        {
            throw new InvalidOperationException("User is not signed in.");
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

    public static IReadOnlyCollection<Claim> GetInternalClaims(User user) =>
        GetInternalClaims(
            user.UserId,
            user.EmailAddress,
            user.FirstName,
            user.MiddleName,
            user.LastName,
            user.Trn,
            user.UserType,
            user.StaffRoles);

    public static string MapUserTypeToClaimValue(UserType userType) => userType.ToString();

    public async Task<IReadOnlyCollection<Claim>> GetPublicClaims(Guid userId, TrnMatchPolicy? trnMatchPolicy)
    {
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.UserId == userId);

        if (user is null)
        {
            return Array.Empty<Claim>();
        }

        var fullName = string.IsNullOrWhiteSpace(user.MiddleName)
            ? $"{user.FirstName} {user.LastName}"
            : $"{user.FirstName} {user.MiddleName} {user.LastName}";

        var claims = new List<Claim>()
        {
            new Claim(Claims.Subject, userId.ToString()!),
            new Claim(Claims.Email, user.EmailAddress),
            new Claim(Claims.EmailVerified, bool.TrueString),
            new Claim(Claims.Name, fullName),
            new Claim(Claims.GivenName, user.FirstName),
            new Claim(Claims.FamilyName, user.LastName),
        };

        AddClaimIfHaveValue(claims, CustomClaims.PreferredName, user.PreferredName);
        AddClaimIfHaveValue(claims, Claims.MiddleName, user.MiddleName);

        if (user.DateOfBirth is DateOnly dateOfBirth)
        {
            claims.Add(new Claim(Claims.Birthdate, dateOfBirth.ToString(CustomClaims.DateFormat)));
        }

        if (user.MobileNumber is not null)
        {
            claims.Add(new Claim(Claims.PhoneNumber, user.MobileNumber));
            claims.Add(new Claim(Claims.PhoneNumberVerified, bool.TrueString));
        }

        if (trnMatchPolicy is not null)
        {
            var haveSufficientTrnMatch = user.Trn is not null &&
                (trnMatchPolicy == TrnMatchPolicy.Default || user.EffectiveVerificationLevel == TrnVerificationLevel.Medium);

            if (haveSufficientTrnMatch)
            {
                Debug.Assert(user.Trn is not null);
                Debug.Assert(user.TrnLookupStatus.HasValue);
                claims.Add(new Claim(CustomClaims.Trn, user.Trn!));
                claims.Add(new Claim(CustomClaims.TrnLookupStatus, user.TrnLookupStatus!.Value.ToString()));

                if (trnMatchPolicy == TrnMatchPolicy.Strict)
                {
                    var dqtPerson = await _dqtApiClient.GetTeacherByTrn(user.Trn!) ?? throw new Exception($"Could not find teacher with TRN: '{user.Trn}'.");
                    var dqtRecordHasNino = !string.IsNullOrEmpty(dqtPerson.NationalInsuranceNumber);
                    var niNumber = User.NormalizeNationalInsuranceNumber(dqtRecordHasNino ? dqtPerson.NationalInsuranceNumber : user.NationalInsuranceNumber);
                    AddClaimIfHaveValue(claims, CustomClaims.NiNumber, niNumber);
                    claims.Add(new Claim(CustomClaims.TrnMatchNiNumber, dqtRecordHasNino.ToString()));
                }
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

        AddClaimIfHaveValue(claims, Claims.MiddleName, middleName);
        AddClaimIfHaveValue(claims, CustomClaims.Trn, trn);

        foreach (var role in staffRoles ?? Array.Empty<string>())
        {
            claims.Add(new Claim(Claims.Role, role));
        }

        return claims;
    }

    private static void AddClaimIfHaveValue(List<Claim> claims, string claimType, string? stringValue)
    {
        if (!string.IsNullOrEmpty(stringValue))
        {
            claims.Add(new Claim(claimType, stringValue));
        }
    }
}
