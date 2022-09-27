using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer;

public static class ClaimsPrincipalExtensions
{
    public static DateOnly? GetDateOfBirth(this ClaimsPrincipal principal, bool throwIfMissing = false) =>
        GetClaim(principal, Claims.Birthdate, throwIfMissing, value => DateOnly.ParseExact(value, CustomClaims.DateFormat));

    public static string? GetEmailAddress(this ClaimsPrincipal principal, bool throwIfMissing = false) =>
        GetClaim(principal, Claims.Email, throwIfMissing);

    public static bool? GetEmailAddressVerified(this ClaimsPrincipal principal, bool throwIfMissing = false) =>
        GetClaim(principal, Claims.EmailVerified, throwIfMissing, value => bool.Parse(value));

    public static string? GetFirstName(this ClaimsPrincipal principal, bool throwIfMissing = false) =>
        GetClaim(principal, Claims.GivenName, throwIfMissing);

    public static bool? GetHaveCompletedTrnLookup(this ClaimsPrincipal principal, bool throwIfMissing = false) =>
        GetClaim(principal, CustomClaims.HaveCompletedTrnLookup, throwIfMissing, value => bool.Parse(value));

    public static string? GetLastName(this ClaimsPrincipal principal, bool throwIfMissing = false) =>
        GetClaim(principal, Claims.FamilyName, throwIfMissing);

    public static string[] GetStaffRoles(this ClaimsPrincipal principal) =>
        principal.Claims.Where(c => c.Type == Claims.Role).Select(c => c.Value).ToArray();

    public static string? GetTrn(this ClaimsPrincipal principal, bool throwIfMissing = false) =>
        GetClaim(principal, CustomClaims.Trn, throwIfMissing);

    public static Guid? GetUserId(this ClaimsPrincipal principal, bool throwIfMissing = false) =>
        GetClaim(principal, Claims.Subject, throwIfMissing, value => Guid.Parse(value));

    public static UserType? GetUserType(this ClaimsPrincipal principal, bool throwIfMissing = false) =>
        GetClaim(principal, CustomClaims.UserType, throwIfMissing, value => Enum.Parse<UserType>(value));

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
}
