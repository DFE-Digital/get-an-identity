using System.Security.Claims;
using TeacherIdentity.AuthServer.Models;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer.Oidc;

public class UserClaimHelper
{
    public IEnumerable<Claim> GetPublicClaims(User user, Func<string, bool> hasScope)
    {
        return GetPublicClaims(
            hasScope,
            user.UserId,
            user.EmailAddress,
            user.FirstName,
            user.LastName,
            user.DateOfBirth,
            user.Trn);
    }

    public IEnumerable<Claim> GetPublicClaims(AuthenticationState authenticationState, Func<string, bool> hasScope)
    {
        if (!authenticationState.IsComplete())
        {
            throw new InvalidOperationException("Cannot retrieve claims until authentication is complete.");
        }

        return GetPublicClaims(
            hasScope,
            authenticationState.UserId!.Value,
            authenticationState.EmailAddress!,
            authenticationState.FirstName!,
            authenticationState.LastName!,
            authenticationState.DateOfBirth,
            authenticationState.Trn);
    }

    private static IEnumerable<Claim> GetPublicClaims(
        Func<string, bool> hasScope,
        Guid userId,
        string email,
        string firstName,
        string lastName,
        DateOnly? dateOfBirth,
        string? trn)
    {
        yield return new Claim(Claims.Subject, userId.ToString()!);
        yield return new Claim(Claims.Email, email);
        yield return new Claim(Claims.EmailVerified, bool.TrueString);
        yield return new Claim(Claims.Name, firstName + " " + lastName);
        yield return new Claim(Claims.GivenName, firstName);
        yield return new Claim(Claims.FamilyName, lastName);

        if (dateOfBirth.HasValue)
        {
            yield return new Claim(Claims.Birthdate, dateOfBirth!.Value.ToString(CustomClaims.DateFormat));
        }

        if (trn is not null && hasScope(CustomScopes.Trn))
        {
            yield return new Claim(CustomClaims.Trn, trn);
        }
    }
}
