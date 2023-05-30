using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer;

public static class ClaimsPrincipalExtensions
{
    public static string GetEmailAddress(this ClaimsPrincipal principal) => GetClaim(principal, Claims.Email);

    public static string GetFirstName(this ClaimsPrincipal principal) => GetClaim(principal, Claims.GivenName);

    public static string? GetMiddleName(this ClaimsPrincipal principal) =>
        TryGetClaim(principal, Claims.MiddleName, out var middleName) ? middleName : null;

    public static string GetLastName(this ClaimsPrincipal principal) => GetClaim(principal, Claims.FamilyName);

    public static string[] GetStaffRoles(this ClaimsPrincipal principal) =>
        principal.Claims.Where(c => c.Type == Claims.Role).Select(c => c.Value).ToArray();

    public static string GetTrn(this ClaimsPrincipal principal) => GetClaim(principal, CustomClaims.Trn);

    public static Guid GetUserId(this ClaimsPrincipal principal) =>
        TryGetUserId(principal, out var userId) ? userId : throw GetClaimMissingException(Claims.Subject);

    public static UserType GetUserType(this ClaimsPrincipal principal) =>
        GetClaim(principal, CustomClaims.UserType, Enum.Parse<UserType>);

    public static bool TryGetTrn(this ClaimsPrincipal principal, [NotNullWhen(true)] out string? trn) =>
        TryGetClaim(principal, CustomClaims.Trn, out trn);

    public static bool TryGetUserId(this ClaimsPrincipal principal, out Guid userId)
    {
        // Subject here can either be a GUID (which is our User ID) or it could be the client ID
        // (in cases where client credentials grant is being used)

        var value = principal.FindFirstValue(Claims.Subject);

        if (!string.IsNullOrEmpty(value) && Guid.TryParse(value, out userId))
        {
            return true;
        }

        userId = default;
        return false;
    }

    private static string GetClaim(ClaimsPrincipal principal, string claimType) =>
        TryGetClaim(principal, claimType, out var value) ? value : throw GetClaimMissingException(claimType);

    private static T GetClaim<T>(ClaimsPrincipal principal, string claimType, Func<string, T> convertValue) where T : struct =>
        TryGetClaim<T>(principal, claimType, convertValue, out var value) ? value : throw GetClaimMissingException(claimType);

    private static bool TryGetClaim(ClaimsPrincipal principal, string claimType, [NotNullWhen(true)] out string? value)
    {
        value = principal.FindFirstValue(claimType);

        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        return true;
    }

    private static bool TryGetClaim<T>(ClaimsPrincipal principal, string claimType, Func<string, T> convertValue, out T value)
        where T : struct
    {
        var stringValue = principal.FindFirstValue(claimType);

        if (string.IsNullOrEmpty(stringValue))
        {
            value = default;
            return false;
        }

        value = convertValue(stringValue);
        return true;
    }

    private static Exception GetClaimMissingException(string claimType) =>
        new InvalidOperationException($"No '{claimType}' claim was found.");
}
