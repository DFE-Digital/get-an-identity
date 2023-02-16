using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer;

public static class ClaimsPrincipalExtensions
{
    public static string? GetEmailAddress(this ClaimsPrincipal principal, bool throwIfMissing = true) =>
        GetClaim(principal, Claims.Email, throwIfMissing);

    public static string? GetFirstName(this ClaimsPrincipal principal, bool throwIfMissing = true) =>
        GetClaim(principal, Claims.GivenName, throwIfMissing);

    public static string? GetLastName(this ClaimsPrincipal principal, bool throwIfMissing = true) =>
        GetClaim(principal, Claims.FamilyName, throwIfMissing);

    public static string[] GetStaffRoles(this ClaimsPrincipal principal) =>
        principal.Claims.Where(c => c.Type == Claims.Role).Select(c => c.Value).ToArray();

    public static string? GetTrn(this ClaimsPrincipal principal, bool throwIfMissing = true) =>
        GetClaim(principal, CustomClaims.Trn, throwIfMissing);

    public static Guid? GetUserId(this ClaimsPrincipal principal, bool throwIfMissing = true)
    {
        // Subject here can either be a GUID (which is our User ID) or it could be the client ID
        // (in cases where client credentials grant is being used)

        var value = principal.FindFirstValue(Claims.Subject);

        if (string.IsNullOrEmpty(value))
        {
            if (throwIfMissing)
            {
                ThrowClaimMissingException(Claims.Subject);
            }

            return null;
        }

        if (!Guid.TryParse(value, out var userId))
        {
            if (throwIfMissing)
            {
                throw new InvalidOperationException($"The '{Claims.Subject}' claim does not contain a user ID.");
            }

            return null;
        }

        return userId;
    }

    public static UserType? GetUserType(this ClaimsPrincipal principal, bool throwIfMissing = true) =>
        GetClaim(principal, CustomClaims.UserType, throwIfMissing, Enum.Parse<UserType>);

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
