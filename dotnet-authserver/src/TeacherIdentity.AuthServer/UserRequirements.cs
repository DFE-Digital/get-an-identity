using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer;

[Flags]
public enum UserRequirements
{
    None = 0,
    DefaultUserType = 1 << 0,
    StaffUserType = 1 << 1,

    TrnHolder = 1 << 2,

    GetAnIdentityAdmin = 1 << 16,
    GetAnIdentitySupport = 1 << 17,
}

public static class UserRequirementsExtensions
{
    public static UserType[] GetPermittedUserTypes(this UserRequirements requirements)
    {
        if (!ValidateUserRequirements(requirements, out var errorMessage))
        {
            throw new InvalidOperationException($"{nameof(UserRequirements)} are not valid.\n{errorMessage}");
        }

        var userTypes = new HashSet<UserType>();

        if (requirements.HasFlag(UserRequirements.StaffUserType))
        {
            return new[] { UserType.Staff };
        }
        else if (requirements.HasFlag(UserRequirements.DefaultUserType))
        {
            return new[] { UserType.Default };
        }

        return new[] { UserType.Default, UserType.Staff };
    }

    public static UserRequirements GetUserRequirementsForScopes(Func<string, bool> hasScope)
    {
        if (!TryGetUserRequirementsForScopes(hasScope, out var userRequirements, out var invalidScopeErrorMessage))
        {
            throw new InvalidOperationException($"Scope is not valid.\n{invalidScopeErrorMessage}");
        }

        return userRequirements;
    }

    public static bool TryGetUserRequirementsForScopes(
        Func<string, bool> hasScope,
        [NotNullWhen(true)] out UserRequirements userRequirements,
        [NotNullWhen(false)] out string? invalidScopeErrorMessage)
    {
        userRequirements = UserRequirements.DefaultUserType;

        if (hasScope(CustomScopes.Trn))
        {
            userRequirements = UserRequirements.DefaultUserType | UserRequirements.TrnHolder;
        }

        if (hasScope(CustomScopes.GetAnIdentitySupport))
        {
            userRequirements = UserRequirements.StaffUserType | UserRequirements.GetAnIdentitySupport;
        }

        if (hasScope(CustomScopes.UserRead))
        {
            userRequirements = UserRequirements.StaffUserType | UserRequirements.GetAnIdentitySupport;
        }

        if (hasScope(CustomScopes.UserWrite))
        {
            userRequirements = UserRequirements.StaffUserType | UserRequirements.GetAnIdentitySupport;
        }

        if (userRequirements == UserRequirements.None)
        {
            invalidScopeErrorMessage = $"Could not determine {nameof(UserRequirements)} from scopes.";
            return false;
        }

        if (!ValidateUserRequirements(userRequirements, out var errorMessage))
        {
            invalidScopeErrorMessage = $"{nameof(UserRequirements)} deduced from scopes are not valid.\n{errorMessage}";
            return false;
        }

        invalidScopeErrorMessage = default;
        return true;
    }

    public static bool VerifyStaffUserRequirements(this UserRequirements userRequirements, ClaimsPrincipal principal)
    {
        if (!ValidateUserRequirements(userRequirements, out var errorMessage))
        {
            throw new InvalidOperationException($"{nameof(UserRequirements)} are not valid.\n{errorMessage}");
        }

        if (!userRequirements.HasFlag(UserRequirements.StaffUserType))
        {
            throw new InvalidOperationException($"{nameof(UserRequirements)} does not contain {UserRequirements.StaffUserType}.");
        }

        if (principal.GetUserType(throwIfMissing: true) != UserType.Staff)
        {
            return false;
        }

        if (userRequirements.HasFlag(UserRequirements.GetAnIdentityAdmin) && !principal.IsInRole(StaffRoles.GetAnIdentityAdmin))
        {
            return false;
        }

        if (userRequirements.HasFlag(UserRequirements.GetAnIdentitySupport))
        {
            if (!(principal.IsInRole(StaffRoles.GetAnIdentitySupport) || principal.IsInRole(StaffRoles.GetAnIdentityAdmin)))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ValidateUserRequirements(
        UserRequirements requirements,
        [NotNullWhen(false)] out string? errorMessage)
    {
        if (requirements.HasFlag(UserRequirements.DefaultUserType) && requirements.HasFlag(UserRequirements.StaffUserType))
        {
            errorMessage = $"'{UserRequirements.DefaultUserType}' and '{UserRequirements.StaffUserType}' are mutually exclusive.";
            return false;
        }

        if (requirements.HasFlag(UserRequirements.TrnHolder) && !requirements.HasFlag(UserRequirements.DefaultUserType))
        {
            errorMessage = $"'{UserRequirements.TrnHolder}' requires '{UserRequirements.DefaultUserType}'.";
            return false;
        }

        if (requirements.HasFlag(UserRequirements.GetAnIdentityAdmin) && !requirements.HasFlag(UserRequirements.StaffUserType))
        {
            errorMessage = $"'{UserRequirements.GetAnIdentityAdmin}' requires '{UserRequirements.StaffUserType}'.";
            return false;
        }

        if (requirements.HasFlag(UserRequirements.GetAnIdentitySupport) && !requirements.HasFlag(UserRequirements.StaffUserType))
        {
            errorMessage = $"'{UserRequirements.GetAnIdentitySupport}' requires '{UserRequirements.StaffUserType}'.";
            return false;
        }

        errorMessage = default;
        return true;
    }
}
