using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer;

public static class UserClaimHelper
{
    public static DateOnly? GetDateOfBirth(this ClaimsPrincipal principal, bool throwIfMissing = true) =>
        GetClaim(principal, Claims.Birthdate, throwIfMissing, value => DateOnly.ParseExact(value, CustomClaims.DateFormat));

    public static string? GetEmailAddress(this ClaimsPrincipal principal, bool throwIfMissing = true) =>
        GetClaim(principal, Claims.Email, throwIfMissing);

    public static bool? GetEmailAddressVerified(this ClaimsPrincipal principal, bool throwIfMissing = true) =>
        GetClaim(principal, Claims.EmailVerified, throwIfMissing, value => bool.Parse(value));

    public static string? GetFirstName(this ClaimsPrincipal principal, bool throwIfMissing = true) =>
        GetClaim(principal, Claims.GivenName, throwIfMissing);

    public static bool? GetHaveCompletedTrnLookup(this ClaimsPrincipal principal, bool throwIfMissing = true) =>
        GetClaim(principal, CustomClaims.HaveCompletedTrnLookup, throwIfMissing, value => bool.Parse(value));

    public static string? GetLastName(this ClaimsPrincipal principal, bool throwIfMissing = true) =>
        GetClaim(principal, Claims.FamilyName, throwIfMissing);

    public static string[] GetStaffRoles(this ClaimsPrincipal principal) =>
        principal.Claims.Where(c => c.Type == Claims.Role).Select(c => c.Value).ToArray();

    public static string? GetTrn(this ClaimsPrincipal principal, bool throwIfMissing = true) =>
        GetClaim(principal, CustomClaims.Trn, throwIfMissing);

    public static Guid? GetUserId(this ClaimsPrincipal principal, bool throwIfMissing = true) =>
        GetClaim(principal, Claims.Subject, throwIfMissing, value => Guid.Parse(value));

    public static UserType? GetUserType(this ClaimsPrincipal principal, bool throwIfMissing = true) =>
        GetClaim(principal, CustomClaims.UserType, throwIfMissing, value => Enum.Parse<UserType>(value));

    public static IEnumerable<Claim> GetInternalClaims(User user)
    {
        return GetInternalClaims(
            user.UserId,
            user.EmailAddress,
            user.FirstName,
            user.LastName,
            user.DateOfBirth,
            user.Trn,
            user.CompletedTrnLookup.HasValue,
            user.UserType,
            user.StaffRoles);
    }

    public static IEnumerable<Claim> GetInternalClaims(AuthenticationState authenticationState)
    {
        if (!authenticationState.IsComplete())
        {
            throw new InvalidOperationException("Cannot retrieve claims until authentication is complete.");
        }

        return GetInternalClaims(
            authenticationState.UserId!.Value,
            authenticationState.EmailAddress!,
            authenticationState.FirstName!,
            authenticationState.LastName!,
            authenticationState.DateOfBirth,
            authenticationState.Trn,
            authenticationState.HaveCompletedTrnLookup,
            authenticationState.UserType!.Value,
            authenticationState.StaffRoles);
    }

    public static IEnumerable<Claim> GetPublicClaims(User user, Func<string, bool> hasScope)
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

    public static IEnumerable<Claim> GetPublicClaims(AuthenticationState authenticationState, Func<string, bool> hasScope)
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

    private static string? GetClaim(ClaimsPrincipal principal, string claimType, bool throwIfMissing)
    {
        var value = principal.FindFirstValue(claimType);

        if (string.IsNullOrEmpty(value))
        {
            if (throwIfMissing)
            {
                ThrowClaimMissingException(claimType);
            }

            return null;
        }

        return value;
    }

    private static T? GetClaim<T>(ClaimsPrincipal principal, string claimType, bool throwIfMissing, Func<string, T> convertValue)
        where T : struct
    {
        var value = principal.FindFirstValue(claimType);

        if (string.IsNullOrEmpty(value))
        {
            if (throwIfMissing)
            {
                ThrowClaimMissingException(claimType);
            }

            return null;
        }

        return convertValue(value);
    }

    [DoesNotReturn]
    private static void ThrowClaimMissingException(string claimType) =>
        throw new InvalidOperationException($"No '{claimType}' claim was found.");

    private static IEnumerable<Claim> GetInternalClaims(
        Guid userId,
        string email,
        string firstName,
        string lastName,
        DateOnly? dateOfBirth,
        string? trn,
        bool haveCompletedTrnLookup,
        UserType userType,
        string[]? staffRoles)
    {
        yield return new Claim(Claims.Subject, userId.ToString()!);
        yield return new Claim(Claims.Email, email);
        yield return new Claim(Claims.EmailVerified, bool.TrueString);
        yield return new Claim(Claims.Name, firstName + " " + lastName);
        yield return new Claim(Claims.GivenName, firstName);
        yield return new Claim(Claims.FamilyName, lastName);
        yield return new Claim(CustomClaims.HaveCompletedTrnLookup, haveCompletedTrnLookup.ToString());
        yield return new Claim(CustomClaims.UserType, userType.ToString());

        if (dateOfBirth.HasValue)
        {
            yield return new Claim(Claims.Birthdate, dateOfBirth!.Value.ToString(CustomClaims.DateFormat));
        }

        if (trn is not null)
        {
            yield return new Claim(CustomClaims.Trn, trn);
        }

        foreach (var role in staffRoles ?? Array.Empty<string>())
        {
            yield return new Claim(Claims.Role, role);
        }
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

        if (hasScope(CustomScopes.Trn))
        {
            yield return new Claim(CustomClaims.Trn, trn ?? string.Empty);
        }
    }
}
